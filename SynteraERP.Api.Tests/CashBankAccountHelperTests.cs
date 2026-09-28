using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SynteraERP.Api.Data;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Tests;

// Backend duplicate-logic audit (26 Sep 2026): SalesOrderPaymentService, InvoiceService and
// PurchaseOrderService each carried a byte-identical private ResolveCashBankAccountAsync, plus a
// 4th inline variant in ExpenseService.CreateAsync with a slightly different "not found" message
// and no upfront Code fetch. All 4 now delegate to CashBankAccountHelper.ResolveAsync - this
// covers the helper directly (explicit account, default-to-Kas fallback, and both error paths)
// rather than re-testing it indirectly through every caller.
public class CashBankAccountHelperTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Seeded via OnModelCreating HasData - see Migrations/AppDbContextModelSnapshot.cs.
    private static readonly Guid SeededKasAccountId = new("50000000-0000-0000-0000-000000000002");

    private readonly WebApplicationFactory<Program> _factory;

    public CashBankAccountHelperTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private IServiceProvider CreateScratchServices()
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
                services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));
            });
        });

        return factory.Services;
    }

    [Fact]
    public async Task ResolveAsync_with_explicit_id_returns_that_accounts_Id_and_Code()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var account = new Account
        {
            Code = "1-9" + Guid.NewGuid().ToString("N")[..4],
            Name = "Bank Test " + Guid.NewGuid().ToString("N")[..6],
            Type = AccountType.Asset,
            NormalBalance = NormalBalanceType.Debit,
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        try
        {
            var (id, code) = await CashBankAccountHelper.ResolveAsync(db, account.Id);

            id.Should().Be(account.Id);
            code.Should().Be(account.Code);
        }
        finally
        {
            db.Accounts.Remove(account);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ResolveAsync_with_null_id_falls_back_to_default_Kas_1_1001()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var (id, code) = await CashBankAccountHelper.ResolveAsync(db, null);

        id.Should().Be(SeededKasAccountId);
        code.Should().Be("1-1001");
    }

    [Fact]
    public async Task ResolveAsync_with_nonexistent_id_throws()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var act = () => CashBankAccountHelper.ResolveAsync(db, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Akun Kas/Bank yang dipilih tidak ditemukan.");
    }

    [Fact]
    public async Task ResolveAsync_ignores_soft_deleted_account_even_when_id_matches()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var deletedAccount = new Account
        {
            Code = "1-9" + Guid.NewGuid().ToString("N")[..4],
            Name = "Deleted Bank " + Guid.NewGuid().ToString("N")[..6],
            Type = AccountType.Asset,
            NormalBalance = NormalBalanceType.Debit,
            IsDeleted = true,
        };
        db.Accounts.Add(deletedAccount);
        await db.SaveChangesAsync();

        try
        {
            var act = () => CashBankAccountHelper.ResolveAsync(db, deletedAccount.Id);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Akun Kas/Bank yang dipilih tidak ditemukan.");
        }
        finally
        {
            db.Accounts.Remove(deletedAccount);
            await db.SaveChangesAsync();
        }
    }
}
