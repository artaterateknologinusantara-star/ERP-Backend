using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Regresi untuk bug: JournalPostingService.ReverseAsync menulis reversal entry (SaveChangesAsync #1)
// lalu menandai entry asal Reversed (SaveChangesAsync #2) tanpa transaction pembungkus. Kalau save
// ke-2 gagal, reversal entry sudah kadung ter-commit sebagai jurnal yatim (Posted, tidak pernah
// ter-link balik ke entry asal). Sama persis dengan bug FinalSubconCost di QuotationService.UpdateAsync
// yang ditemukan sebelumnya -- fix-nya juga sama: bungkus dua SaveChangesAsync itu dalam satu
// Database.BeginTransactionAsync()/CommitAsync().
public class JournalPostingReverseTransactionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public JournalPostingReverseTransactionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private IServiceProvider CreateScratchServices(SaveChangesInterceptor? interceptor = null)
    {
        var connectionString = "Server=localhost,1433;Database=SynteraERP_Scratch;User Id=sa;Password=DevgvImMkAaOBHs4CP5kWRsLLyM!9q;TrustServerCertificate=True;Encrypt=False;";
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-not-used-anywhere-else-32chars");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(opt =>
                {
                    opt.UseSqlServer(connectionString);
                    if (interceptor is not null) opt.AddInterceptors(interceptor);
                });
            });
        });

        return factory.Services;
    }

    /// <summary>Throws only on the SECOND SaveChangesAsync inside ReverseAsync — the one that marks
    /// the original entry Reversed (a tracked JournalEntry in Modified state). The first save (Added
    /// reversal entry + lines) and the entry-number save (Modified NumberingConfig only) both pass
    /// through untouched, so the failure lands exactly where the real bug's second write was.</summary>
    private sealed class ThrowOnJournalEntryUpdateInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var ctx = eventData.Context;
            if (ctx is not null && ctx.ChangeTracker.Entries<JournalEntry>().Any(e => e.State == EntityState.Modified))
                throw new InvalidOperationException("Simulated failure: second write in ReverseAsync (test-only).");

            return new ValueTask<InterceptionResult<int>>(result);
        }
    }

    private static async Task<JournalEntry> SeedPostedEntryAsync(AppDbContext db, string entryNumber)
    {
        var accounts = await db.Accounts.OrderBy(a => a.Code).Take(2).ToListAsync();
        accounts.Should().HaveCountGreaterOrEqualTo(2, "chart of accounts is seeded via migration HasData");

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            Date        = DateTimeOffset.UtcNow,
            Description = "Original entry for reversal transaction test",
            SourceType  = JournalSourceType.ManualAdjustment,
            Status      = JournalEntryStatus.Posted,
            PostedAt    = DateTimeOffset.UtcNow,
            PostedByUserId = SeededAdminId,
            CreatedBy   = SeededAdminId,
            Lines =
            [
                new JournalEntryLine { AccountId = accounts[0].Id, Debit = 1_000_000, Credit = 0, Memo = "Debit line" },
                new JournalEntryLine { AccountId = accounts[1].Id, Debit = 0, Credit = 1_000_000, Memo = "Credit line" },
            ],
        };

        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    [Fact]
    public async Task Reversal_normal_case_still_succeeds_and_links_both_entries()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var original = await SeedPostedEntryAsync(db, "JE-REVTEST-OK-" + Guid.NewGuid().ToString("N")[..8]);

        var journalSvc = scope.ServiceProvider.GetRequiredService<IJournalPostingService>();
        var result = await journalSvc.ReverseAsync(original.Id, SeededAdminId);

        result.Should().NotBeNull();
        result.SourceType.Should().Be(JournalSourceType.Reversal.ToString());
        result.Status.Should().Be(JournalEntryStatus.Posted.ToString());

        // Re-read from DB with a fresh, untracked query -- not the in-memory tracked instance.
        var reloadedOriginal = await db.JournalEntries.AsNoTracking().FirstAsync(x => x.Id == original.Id);
        reloadedOriginal.Status.Should().Be(JournalEntryStatus.Reversed);
        reloadedOriginal.ReversedByEntryId.Should().Be(result.Id);

        var reloadedReversal = await db.JournalEntries.AsNoTracking().Include(x => x.Lines).FirstAsync(x => x.Id == result.Id);
        reloadedReversal.SourceType.Should().Be(JournalSourceType.Reversal);
        reloadedReversal.SourceId.Should().Be(original.Id);
        reloadedReversal.Status.Should().Be(JournalEntryStatus.Posted);
        reloadedReversal.Lines.Should().HaveCount(2);
        reloadedReversal.Lines.Sum(l => l.Debit).Should().Be(1_000_000);
        reloadedReversal.Lines.Sum(l => l.Credit).Should().Be(1_000_000);
    }

    [Fact]
    public async Task Reversal_rolls_back_the_already_written_reversal_entry_when_second_write_fails()
    {
        var interceptor = new ThrowOnJournalEntryUpdateInterceptor();
        var services = CreateScratchServices(interceptor);
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var original = await SeedPostedEntryAsync(db, "JE-REVTEST-FAIL-" + Guid.NewGuid().ToString("N")[..8]);

        var journalSvc = scope.ServiceProvider.GetRequiredService<IJournalPostingService>();

        var act = async () => await journalSvc.ReverseAsync(original.Id, SeededAdminId);
        await act.Should().ThrowAsync<InvalidOperationException>("the interceptor simulates a failure on the second write");

        // Verify with a completely separate scope/context -- the interceptor only lives on the
        // service's DbContext, and we want an authoritative read of what actually persisted.
        using var verifyScope = services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reloadedOriginal = await verifyDb.JournalEntries.AsNoTracking().FirstAsync(x => x.Id == original.Id);
        reloadedOriginal.Status.Should().Be(JournalEntryStatus.Posted,
            "the whole operation must roll back -- the original must NOT end up half-reversed");
        reloadedOriginal.ReversedByEntryId.Should().BeNull();

        var orphanReversal = await verifyDb.JournalEntries.AsNoTracking()
            .Where(x => x.SourceType == JournalSourceType.Reversal && x.SourceId == original.Id)
            .ToListAsync();
        orphanReversal.Should().BeEmpty("the reversal entry inserted before the failing second write must be rolled back too -- no orphan journal entry should remain");
    }
}
