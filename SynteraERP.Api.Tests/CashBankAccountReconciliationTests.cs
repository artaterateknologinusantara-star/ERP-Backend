using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using System.Collections.Generic;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.DTOs.SalesOrderPayment;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Prasyarat Rekonsiliasi Bank: verifikasi CashBankAccountId (migration
// 20260904133359_AddCashBankAccountIdToPayments) di scratch DB SQL Server, bukan
// SQLite - supaya FK/tipe kolom asli ikut teruji, bukan cuma logic C#.
public class CashBankAccountReconciliationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CashBankAccountReconciliationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (IServiceProvider services, Microsoft.Net.Http.Headers.MediaTypeHeaderValue? _) CreateScratchServices()
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

        // Force server creation so DI container exists without needing an HttpClient/auth handler
        // (this test never hits HTTP endpoints - it drives the services directly, same as
        // CustomerPoIntegrationTests does for its CustomerPO number-update assertions).
        var services = factory.Services;
        return (services, null);
    }

    [Fact]
    public async Task CashBankAccountId_migration_and_posting_end_to_end()
    {
        var (services, _) = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var all = db.Database.GetMigrations().ToList();
        const string target = "20260904133359_AddCashBankAccountIdToPayments";
        var idx = all.IndexOf(target);
        idx.Should().BeGreaterThan(0, "migration under test must exist in the migration history");
        var before = all[idx - 1];

        var migrator = db.GetService<IMigrator>();
        migrator.Migrate(before);

        await CustomerSeeder.SeedAsync(db);
        await SupplierSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var seededCustomerId = new Guid("C0000000-0000-0000-0000-000000000001");
        var seededSupplierId = new Guid("60000000-0000-0000-0000-000000000001");
        var seededAdminId = new Guid("20000000-0000-0000-0000-000000000001");

        // ── Fixture pra-migration: PO + 2 POPayment lama (skema lama, belum ada
        // CashBankAccountId) - mensimulasikan 7 baris POPayments yang sudah ada di
        // database development, untuk membuktikan backfill migration benar-benar jalan.
        var legacyPo = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            No = "TST-PO-LEGACY",
            SupplierId = seededSupplierId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = PurchaseOrderStatus.Ordered,
            Total = 5_000_000,
        };
        db.PurchaseOrders.Add(legacyPo);
        await db.SaveChangesAsync();

        var legacyPaymentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        foreach (var pid in legacyPaymentIds)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO POPayments (Id, PurchaseOrderId, PaymentDate, Amount, Method, Reference, Notes, CreatedAt)
                VALUES ({pid}, {legacyPo.Id}, {DateOnly.FromDateTime(DateTime.UtcNow)}, {1_000_000m}, {"Transfer"}, {(string?)null}, {(string?)null}, {DateTimeOffset.UtcNow})");
        }

        // ── Snapshot NumberingConfig SEBELUM migration (aturan wajib CLAUDE.md #2) ──
        var beforeNC = await db.NumberingConfigs
            .Select(n => new { n.DocType, n.Prefix, n.LastNumber })
            .OrderBy(n => n.DocType)
            .ToListAsync();

        migrator.Migrate(target);

        var afterNC = await db.NumberingConfigs
            .Select(n => new { n.DocType, n.Prefix, n.LastNumber })
            .OrderBy(n => n.DocType)
            .ToListAsync();

        afterNC.Should().BeEquivalentTo(beforeNC, options => options.WithStrictOrdering(),
            "migration ini tidak dimaksudkan menyentuh NumberingConfig sama sekali");

        // ── Verifikasi backfill POPayments lama → 1-1001 Kas ──
        var kasId = await db.Accounts.Where(a => a.Code == "1-1001").Select(a => a.Id).FirstAsync();
        var bcaId = await db.Accounts.Where(a => a.Code == "1-1002").Select(a => a.Id).FirstAsync();
        var mandiriId = await db.Accounts.Where(a => a.Code == "1-1003").Select(a => a.Id).FirstAsync();
        var bniId = await db.Accounts.Where(a => a.Code == "1-1004").Select(a => a.Id).FirstAsync();

        var legacyBackfilled = await db.POPayments
            .Where(p => legacyPaymentIds.Contains(p.Id))
            .Select(p => p.CashBankAccountId)
            .ToListAsync();
        legacyBackfilled.Should().AllSatisfy(id => id.Should().Be(kasId),
            "POPayments lama harus ter-backfill ke akun Kas (1-1001), bukan NULL");

        // ── Skenario bisnis: pakai service via DI, sama seperti yang dipanggil controller ──
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var poService = scope.ServiceProvider.GetRequiredService<IPurchaseOrderService>();
        var dpService = scope.ServiceProvider.GetRequiredService<ISalesOrderPaymentService>();
        var siService = scope.ServiceProvider.GetRequiredService<ISupplierInvoiceService>();

        // 2. REGRESI: Invoice payment TANPA CashBankAccountId → tetap ke 1-1001, tidak error.
        var inv1 = new Invoice
        {
            Id = Guid.NewGuid(), No = "TST-INV-1", CustomerId = seededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 1_000_000, Status = InvoiceStatus.Sent,
        };
        db.Invoices.Add(inv1);
        await db.SaveChangesAsync();

        await invoiceService.RecordPaymentAsync(inv1.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow), Amount = 400_000, Method = "Transfer",
        });
        var pay1 = await db.Payments.Where(p => p.InvoiceId == inv1.Id).FirstAsync();
        pay1.CashBankAccountId.Should().Be(kasId, "tanpa CashBankAccountId di request, harus default ke 1-1001 persis seperti sebelum field ini ada");

        // 3. Invoice payment DENGAN CashBankAccountId = Bank BCA → jurnal debit ke 1-1002.
        var inv2 = new Invoice
        {
            Id = Guid.NewGuid(), No = "TST-INV-2", CustomerId = seededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 2_000_000, Status = InvoiceStatus.Sent,
        };
        db.Invoices.Add(inv2);
        await db.SaveChangesAsync();

        await invoiceService.RecordPaymentAsync(inv2.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow), Amount = 750_000, Method = "Transfer", CashBankAccountId = bcaId,
        });
        var pay2 = await db.Payments.Where(p => p.InvoiceId == inv2.Id).FirstAsync();
        pay2.CashBankAccountId.Should().Be(bcaId);

        var pay2Line = await db.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.SourceId == pay2.Id && l.Debit > 0)
            .FirstAsync();
        pay2Line.Account.Code.Should().Be("1-1002", "payment dengan CashBankAccountId terisi harus posting ke akun yang dipilih, bukan 1-1001");

        // Validasi: CashBankAccountId yang tidak ada di COA harus ditolak, bukan diam-diam lolos.
        await FluentActions.Invoking(() => invoiceService.RecordPaymentAsync(inv2.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow), Amount = 10_000, Method = "Transfer", CashBankAccountId = Guid.NewGuid(),
        })).Should().ThrowAsync<InvalidOperationException>();

        // 4a. PO payment TANPA CashBankAccountId → default 1-1001.
        var po1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(), No = "TST-PO-1", SupplierId = seededSupplierId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow), Status = PurchaseOrderStatus.Ordered, Total = 3_000_000,
        };
        db.PurchaseOrders.Add(po1);
        await db.SaveChangesAsync();

        await poService.RecordPaymentAsync(po1.Id, new RecordPOPaymentRequest
        {
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow), Amount = 1_000_000, Method = "Transfer",
        });
        var poPay1 = await db.POPayments.Where(p => p.PurchaseOrderId == po1.Id).FirstAsync();
        poPay1.CashBankAccountId.Should().Be(kasId);

        // 4b. PO payment DENGAN CashBankAccountId = Bank Mandiri → jurnal kredit ke 1-1003.
        var po2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(), No = "TST-PO-2", SupplierId = seededSupplierId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow), Status = PurchaseOrderStatus.Ordered, Total = 3_000_000,
        };
        db.PurchaseOrders.Add(po2);
        await db.SaveChangesAsync();

        await poService.RecordPaymentAsync(po2.Id, new RecordPOPaymentRequest
        {
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow), Amount = 1_200_000, Method = "Transfer", CashBankAccountId = mandiriId,
        });
        var poPay2 = await db.POPayments.Where(p => p.PurchaseOrderId == po2.Id).FirstAsync();
        poPay2.CashBankAccountId.Should().Be(mandiriId);
        var poPay2Line = await db.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.SourceId == poPay2.Id && l.Credit > 0)
            .FirstAsync();
        poPay2Line.Account.Code.Should().Be("1-1003");

        // 5. SupplierInvoice bridge: payment lewat SupplierInvoice harus ikut ke akun yang dipilih.
        var poForSi = new PurchaseOrder
        {
            Id = Guid.NewGuid(), No = "TST-PO-SI", SupplierId = seededSupplierId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow), Status = PurchaseOrderStatus.Ordered, Total = 2_000_000,
        };
        db.PurchaseOrders.Add(poForSi);
        var si = new SupplierInvoice
        {
            Id = Guid.NewGuid(), No = "TST-SI-1", InvoiceNumber = "SUP-INV-1",
            PurchaseOrderId = poForSi.Id, SupplierId = seededSupplierId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Subtotal = 2_000_000, PPNMasukan = 0, Total = 2_000_000, Status = SupplierInvoiceStatus.Approved,
        };
        db.SupplierInvoices.Add(si);
        await db.SaveChangesAsync();

        await siService.RecordPaymentAsync(si.Id, new RecordPOPaymentRequest
        {
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow), Amount = 800_000, Method = "Transfer", CashBankAccountId = bcaId,
        });
        var siPoPayment = await db.POPayments.Where(p => p.PurchaseOrderId == poForSi.Id).FirstAsync();
        siPoPayment.CashBankAccountId.Should().Be(bcaId, "CashBankAccountId harus lolos lewat bridge SupplierInvoice -> PurchaseOrderService, tidak hilang di tengah jalan");
        var siLine = await db.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.SourceId == siPoPayment.Id && l.Credit > 0)
            .FirstAsync();
        siLine.Account.Code.Should().Be("1-1002");

        // 6. SalesOrder DP TANPA lalu DENGAN CashBankAccountId (Bank BNI).
        var so1 = new SalesOrder
        {
            Id = Guid.NewGuid(), No = "TST-SO-1", CustomerId = seededCustomerId, ProjectName = "Test Project",
            Date = DateOnly.FromDateTime(DateTime.UtcNow), SalesId = seededAdminId, Status = SalesOrderStatus.Open, Total = 5_000_000,
        };
        db.SalesOrders.Add(so1);
        await db.SaveChangesAsync();

        await dpService.RecordDownPaymentAsync(so1.Id, new RecordDownPaymentRequest { Amount = 500_000, Method = "Transfer" });
        var dp1 = await db.SalesOrderPayments.Where(p => p.SalesOrderId == so1.Id).FirstAsync();
        dp1.CashBankAccountId.Should().Be(kasId);

        await dpService.RecordDownPaymentAsync(so1.Id, new RecordDownPaymentRequest { Amount = 700_000, Method = "Transfer", CashBankAccountId = bniId });
        var dp2 = await db.SalesOrderPayments.Where(p => p.SalesOrderId == so1.Id && p.Id != dp1.Id).FirstAsync();
        dp2.CashBankAccountId.Should().Be(bniId);
        var dp2Line = await db.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.SourceId == dp2.Id && l.Debit > 0)
            .FirstAsync();
        dp2Line.Account.Code.Should().Be("1-1004");

        // 7. Saldo 1-1002/1-1003/1-1004 sekarang HARUS ada isinya (bukan lagi 0 selamanya).
        var bcaBalance = await db.JournalEntryLines.Where(l => l.Account.Code == "1-1002").SumAsync(l => l.Debit - l.Credit);
        var mandiriBalance = await db.JournalEntryLines.Where(l => l.Account.Code == "1-1003").SumAsync(l => l.Debit - l.Credit);
        var bniBalance = await db.JournalEntryLines.Where(l => l.Account.Code == "1-1004").SumAsync(l => l.Debit - l.Credit);
        bcaBalance.Should().NotBe(0);
        mandiriBalance.Should().NotBe(0);
        bniBalance.Should().NotBe(0);

        // 6 (lanjutan). Trial balance tetap balance (Rp0) di semua skenario di atas.
        var totalDebit = await db.JournalEntryLines.SumAsync(l => l.Debit);
        var totalCredit = await db.JournalEntryLines.SumAsync(l => l.Credit);
        totalDebit.Should().Be(totalCredit, "total Debit dan Credit di seluruh GL harus selalu sama (double-entry)");
    }
}
