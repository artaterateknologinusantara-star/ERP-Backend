using System;
using System.Linq;
using System.Linq.Expressions;
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

// Backend duplicate-logic audit (26 Sep 2026): NextNumberAsync-style resync logic (bump
// NumberingConfig.LastNumber up to the actual highest number issued this year, before generating
// the next one) used to exist ONLY in QuotationService — extracted to NumberingResyncHelper and
// migrated onto all 9 DocTypes. Only Quotation had ANY test coverage for the base
// GenerateNext()/NumberingConfig-not-found path before this — none of the 9 had a test for the
// resync/drift-correction behavior itself. Each test here reproduces the exact drift shape: bump
// a decoy row's number well ahead of the stored LastNumber, then confirm the helper notices and
// resyncs instead of regenerating a number that collides with (or falls behind) the decoy.
public class NumberingResyncHelperTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededSupplierId = new("60000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededAccountKasId = new("50000000-0000-0000-0000-000000000002");
    private static readonly Guid SeededExpenseCategoryRentId = new("70000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public NumberingResyncHelperTests(WebApplicationFactory<Program> factory)
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

    // Shared assertion for all 9 DocTypes: read the current counter, plant a decoy row whose
    // number is 50 ahead of it (simulating drift from any cause -- not just the closed July-2026
    // HasData incident), call the helper, and confirm it resyncs to decoyNumber+1 rather than
    // blindly incrementing the stale stored counter.
    private static async Task AssertResyncSkipsPastDrift<T>(
        AppDbContext db, string docType, IQueryable<T> table,
        Expression<Func<T, string>> numberSelector, Func<string, T> buildDecoy)
        where T : class
    {
        var configBefore = await db.NumberingConfigs.AsNoTracking().FirstAsync(n => n.DocType == docType);
        var year = DateTime.UtcNow.ToString("yy");
        var yearPrefix = $"{configBefore.Prefix}-{year}.";
        var driftedNumber = configBefore.LastNumber + 50;
        var decoyNo = $"{yearPrefix}{driftedNumber:D4}";

        var decoy = buildDecoy(decoyNo);
        db.Set<T>().Add(decoy);
        await db.SaveChangesAsync();

        try
        {
            var result = await NumberingResyncHelper.NextNumberAsync(db, table, numberSelector, docType);

            result.Should().Be($"{yearPrefix}{(driftedNumber + 1):D4}",
                "the helper must resync past the drifted decoy, not just increment the stale stored counter");

            var configAfter = await db.NumberingConfigs.AsNoTracking().FirstAsync(n => n.DocType == docType);
            configAfter.LastNumber.Should().Be(driftedNumber + 1);
        }
        finally
        {
            db.Set<T>().Remove(decoy);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Quotation_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);

        await AssertResyncSkipsPastDrift(db, "QUOTATION", db.Quotations, q => q.No, no => new Quotation
        {
            No = no,
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Resync test " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
        });
    }

    [Fact]
    public async Task Invoice_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);

        await AssertResyncSkipsPastDrift(db, "INVOICE", db.Invoices, x => x.No, no => new Invoice
        {
            No = no,
            CustomerId = SeededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 1_000_000,
        });
    }

    [Fact]
    public async Task SalesOrder_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);

        await AssertResyncSkipsPastDrift(db, "SALES_ORDER", db.SalesOrders, x => x.No, no => new SalesOrder
        {
            No = no,
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Resync test " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        });
    }

    [Fact]
    public async Task PurchaseOrder_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SupplierSeeder.SeedAsync(db);

        await AssertResyncSkipsPastDrift(db, "PURCHASE_ORDER", db.PurchaseOrders, x => x.No, no => new PurchaseOrder
        {
            No = no,
            SupplierId = SeededSupplierId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        });
    }

    [Fact]
    public async Task PurchaseRequest_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await AssertResyncSkipsPastDrift(db, "PURCHASE_REQUEST", db.PurchaseRequests, x => x.No, no => new PurchaseRequest
        {
            No = no,
            RequestedBy = SeededAdminId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        });
    }

    [Fact]
    public async Task SupplierInvoice_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SupplierSeeder.SeedAsync(db);

        // SupplierInvoice.PurchaseOrderId is non-nullable -- needs a real PO row to reference,
        // unrelated to the numbering behavior under test.
        var prerequisitePo = new PurchaseOrder
        {
            No = "PO-PREREQ-" + Guid.NewGuid().ToString("N")[..8],
            SupplierId = SeededSupplierId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.PurchaseOrders.Add(prerequisitePo);
        await db.SaveChangesAsync();

        try
        {
            await AssertResyncSkipsPastDrift(db, "SUPPLIER_INVOICE", db.SupplierInvoices, x => x.No, no => new SupplierInvoice
            {
                No = no,
                InvoiceNumber = "EXT-" + Guid.NewGuid().ToString("N")[..8],
                PurchaseOrderId = prerequisitePo.Id,
                SupplierId = SeededSupplierId,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            });
        }
        finally
        {
            db.PurchaseOrders.Remove(prerequisitePo);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Expense_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await AssertResyncSkipsPastDrift(db, "EXPENSE", db.Expenses, x => x.ExpenseNo, no => new Expense
        {
            ExpenseNo = no,
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpenseCategoryId = SeededExpenseCategoryRentId,
            Description = "Resync test " + Guid.NewGuid().ToString("N")[..6],
            Method = "Cash",
            CashBankAccountId = SeededAccountKasId,
        });
    }

    [Fact]
    public async Task DeliveryOrder_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await AssertResyncSkipsPastDrift(db, "DELIVERY_ORDER", db.DeliveryOrders, x => x.No, no => new DeliveryOrder
        {
            No = no,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedByUserId = SeededAdminId,
        });
    }

    [Fact]
    public async Task JournalEntry_resync_skips_past_drift()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await AssertResyncSkipsPastDrift(db, "JOURNAL_ENTRY", db.JournalEntries, x => x.EntryNumber, no => new JournalEntry
        {
            EntryNumber = no,
            Date = DateTimeOffset.UtcNow,
            Description = "Resync test " + Guid.NewGuid().ToString("N")[..6],
            SourceType = JournalSourceType.ManualAdjustment,
        });
    }
}
