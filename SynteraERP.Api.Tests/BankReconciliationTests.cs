using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.DTOs.BankReconciliation;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Rekonsiliasi Bank Fase 1 (backend) - verifikasi di scratch DB SQL Server (bukan SQLite),
// meliputi migration (2 tabel baru saja), import CSV valid/invalid, saran matching ±3 hari,
// validasi match (akun sama, Posted-only, tidak dobel-pakai), dan endpoint balances ringan.
public class BankReconciliationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BankReconciliationTests(WebApplicationFactory<Program> factory)
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

    private static IFormFile MakeCsvFile(string content, string fileName = "mutasi.csv")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName) { Headers = new HeaderDictionary(), ContentType = "text/csv" };
    }

    [Fact]
    public async Task BankReconciliation_migration_import_matching_and_balances_end_to_end()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var all = db.Database.GetMigrations().ToList();
        const string target = "20260904185141_AddBankReconciliationTables";
        var idx = all.IndexOf(target);
        idx.Should().BeGreaterThan(0, "migration under test must exist in the migration history");
        var before = all[idx - 1];

        var migrator = db.GetService<IMigrator>();
        migrator.Migrate(before);

        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        // ── §1: snapshot Accounts + NumberingConfig SEBELUM migration ──
        var beforeAccounts = await db.Accounts
            .Select(a => new { a.Id, a.Code, a.Name, a.Type, a.ParentAccountId, a.NormalBalance, a.IsControlAccount, a.IsDeleted })
            .OrderBy(a => a.Code).ToListAsync();
        var beforeNC = await db.NumberingConfigs
            .Select(n => new { n.DocType, n.Prefix, n.LastNumber })
            .OrderBy(n => n.DocType).ToListAsync();

        migrator.Migrate(target);

        var afterAccounts = await db.Accounts
            .Select(a => new { a.Id, a.Code, a.Name, a.Type, a.ParentAccountId, a.NormalBalance, a.IsControlAccount, a.IsDeleted })
            .OrderBy(a => a.Code).ToListAsync();
        var afterNC = await db.NumberingConfigs
            .Select(n => new { n.DocType, n.Prefix, n.LastNumber })
            .OrderBy(n => n.DocType).ToListAsync();

        afterAccounts.Should().BeEquivalentTo(beforeAccounts, o => o.WithStrictOrdering(), "migration ini tidak boleh menyentuh tabel Accounts sama sekali");
        afterNC.Should().BeEquivalentTo(beforeNC, o => o.WithStrictOrdering(), "migration ini tidak boleh menyentuh NumberingConfig sama sekali");

        var bankSvc = scope.ServiceProvider.GetRequiredService<IBankReconciliationService>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var journalPostingService = scope.ServiceProvider.GetRequiredService<IJournalPostingService>();

        var seededCustomerId = new Guid("C0000000-0000-0000-0000-000000000001");
        var bcaId = await db.Accounts.Where(a => a.Code == "1-1002").Select(a => a.Id).FirstAsync();

        // ── §2: import CSV valid, 5 baris campuran debit/kredit ──
        const string validCsv =
            "Tanggal,Keterangan,Debit,Kredit\n" +
            "2026-09-01,Setoran tunai,500000,\n" +
            "2026-09-02,Transfer masuk dari customer,750000,\n" +
            "2026-09-03,Biaya admin bank,,15000\n" +
            "2026-09-04,Bunga bank,25000,\n" +
            "2026-09-05,Transfer keluar,,300000\n";

        var importResult = await bankSvc.ImportAsync(
            new ImportBankStatementRequest { AccountId = bcaId, PeriodStart = new DateOnly(2026, 9, 1), PeriodEnd = new DateOnly(2026, 9, 5) },
            MakeCsvFile(validCsv));

        importResult.Success.Should().BeTrue();
        importResult.Summary!.LineCount.Should().Be(5);

        var savedLines = await db.BankStatementLines
            .Where(l => l.BankStatementImportId == importResult.Summary.Id)
            .OrderBy(l => l.TransactionDate)
            .ToListAsync();
        savedLines.Should().HaveCount(5);
        savedLines[0].Amount.Should().Be(500_000m);   // Debit -> +
        savedLines[1].Amount.Should().Be(750_000m);   // Debit -> +
        savedLines[2].Amount.Should().Be(-15_000m);   // Kredit -> -
        savedLines[3].Amount.Should().Be(25_000m);    // Debit -> +
        savedLines[4].Amount.Should().Be(-300_000m);  // Kredit -> -
        savedLines.Should().OnlyContain(l => l.MatchStatus == BankStatementLineMatchStatus.Unmatched);

        // ── §3: import CSV dengan 1 baris format tanggal salah -> seluruh import ditolak ──
        const string invalidCsv =
            "Tanggal,Keterangan,Debit,Kredit\n" +
            "2026-09-01,Baris OK 1,100000,\n" +
            "2026-13-40,Baris tanggal rusak,200000,\n" +
            "2026-09-03,Baris OK 2,,50000\n" +
            "2026-09-04,Baris OK 3,300000,\n";

        var invalidResult = await bankSvc.ImportAsync(
            new ImportBankStatementRequest { AccountId = bcaId, PeriodStart = new DateOnly(2026, 9, 1), PeriodEnd = new DateOnly(2026, 9, 4) },
            MakeCsvFile(invalidCsv, "invalid.csv"));

        invalidResult.Success.Should().BeFalse();
        invalidResult.RowErrors.Should().HaveCount(1);
        invalidResult.RowErrors![0].RowNumber.Should().Be(3, "baris data ke-2 (setelah header di baris 1) ada di baris fisik ke-3");
        invalidResult.RowErrors[0].Reason.Should().Contain("tanggal");

        var importsAfterRejectedFile = await db.BankStatementImports.Where(i => i.AccountId == bcaId).CountAsync();
        importsAfterRejectedFile.Should().Be(1, "import yang gagal validasi tidak boleh membuat baris apa pun di DB (bukan 4 baris masuk 1 gagal)");

        // ── §4: transaksi nyata yang cocok dengan salah satu baris CSV di atas ──
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), No = "TST-BANKREC-INV-1", CustomerId = seededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 2_000_000, Status = InvoiceStatus.Sent,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        // Baris CSV row[1]: 2026-09-02, +750000. Bayar Rp750.000 tgl 2026-09-01 (dalam window ±3 hari) ke BCA.
        await invoiceService.RecordPaymentAsync(invoice.Id, new RecordPaymentRequest
        {
            Date = new DateOnly(2026, 9, 1), Amount = 750_000, Method = "Transfer", CashBankAccountId = bcaId,
        });
        var matchingPayment = await db.Payments.Where(p => p.InvoiceId == invoice.Id).FirstAsync();
        var matchingJel = await db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.SourceId == matchingPayment.Id && l.Debit > 0)
            .FirstAsync();

        var detail = await bankSvc.GetImportDetailAsync(importResult.Summary.Id);
        detail.Should().NotBeNull();
        var targetLine = detail!.Lines.First(l => l.Amount == 750_000m);
        targetLine.SuggestedMatches.Should().ContainSingle(c => c.JournalEntryLineId == matchingJel.Id,
            "baris CSV +750.000 tgl 2026-09-02 harus dapat saran matching ke pembayaran Rp750.000 tgl 2026-09-01 (dalam window ±3 hari)");

        // ── §5: konfirmasi match ──
        var matched = await bankSvc.MatchAsync(targetLine.Id, matchingJel.Id);
        matched.MatchStatus.Should().Be("Matched");
        matched.MatchedJournalEntryLineId.Should().Be(matchingJel.Id);

        // ── §6: match BankStatementLine LAIN ke JournalEntryLine yang SUDAH dipakai -> ditolak ──
        var otherLine = detail.Lines.First(l => l.Amount == 500_000m);
        await FluentActions.Invoking(() => bankSvc.MatchAsync(otherLine.Id, matchingJel.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dipakai*");

        // ── §7: match ke JournalEntryLine dari entry yang sudah di-Reverse -> ditolak ──
        var invoice2 = new Invoice
        {
            Id = Guid.NewGuid(), No = "TST-BANKREC-INV-2", CustomerId = seededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 1_000_000, Status = InvoiceStatus.Sent,
        };
        db.Invoices.Add(invoice2);
        await db.SaveChangesAsync();

        await invoiceService.RecordPaymentAsync(invoice2.Id, new RecordPaymentRequest
        {
            Date = new DateOnly(2026, 9, 4), Amount = 500_000, Method = "Transfer", CashBankAccountId = bcaId,
        });
        var toBeReversedPayment = await db.Payments.Where(p => p.InvoiceId == invoice2.Id).FirstAsync();
        var toBeReversedJel = await db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.SourceId == toBeReversedPayment.Id && l.Debit > 0)
            .FirstAsync();

        var seededAdminId = new Guid("20000000-0000-0000-0000-000000000001");
        await journalPostingService.ReverseAsync(toBeReversedJel.JournalEntryId, seededAdminId);

        await FluentActions.Invoking(() => bankSvc.MatchAsync(otherLine.Id, toBeReversedJel.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Posted*");

        // ── §8: endpoint balances - SUM benar untuk keempat akun, termasuk yang belum pernah ada transaksi ──
        var balances = await bankSvc.GetBalancesAsync(new DateOnly(2026, 9, 30));
        balances.Should().HaveCount(4);
        var bcaBalance = balances.First(b => b.AccountCode == "1-1002").Balance;
        // BCA: +750.000 (matched invoice payment) + 500.000 (invoice2, sebelum di-reverse) - 500.000 (reversal-nya) = +750.000
        bcaBalance.Should().Be(750_000m);
        var kasBalance = balances.First(b => b.AccountCode == "1-1001").Balance;
        kasBalance.Should().Be(0m, "belum pernah ada transaksi ke akun Kas di test ini - harus 0, bukan hilang dari response");
        balances.Select(b => b.AccountCode).Should().BeEquivalentTo(["1-1001", "1-1002", "1-1003", "1-1004"]);

        // ── §9: unmatch -> balik ke Unmatched bersih ──
        var unmatched = await bankSvc.UnmatchAsync(targetLine.Id);
        unmatched.MatchStatus.Should().Be("Unmatched");
        unmatched.MatchedJournalEntryLineId.Should().BeNull();
    }
}
