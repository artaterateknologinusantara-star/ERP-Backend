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
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Task #43: CreateFromQuotationAsync now asserts SO.Total (computed from soItems) against
// Quotation.GrandTotal (computed independently by RecalcTotals) before the SO is saved — a
// future content source added to one and forgotten in the other (exactly #41's bug shape) throws
// instead of silently converting with a wrong total. Tolerance scales with line count (each
// line's own rounding can contribute up to Rp1 of divergence between "round-per-line-then-sum"
// (SO) and "sum-then-round-per-category" (Quotation)).
//
// Also covers the Discount bug found en route while implementing this: Quotation.Discount was
// never applied in CreateFromQuotationAsync, so SO.Total silently ignored it entirely.
public class SalesOrderQuotationTotalInvariantTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public SalesOrderQuotationTotalInvariantTests(WebApplicationFactory<Program> factory)
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

    private static SaveQuotationRequest BaseRequest(decimal discount, params SaveQuotationItemRequest[] items) => new()
    {
        CustomerId = SeededCustomerId,
        SalesId = SeededAdminId,
        ProjectName = "Test Total Invariant " + Guid.NewGuid().ToString("N")[..6],
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
        Discount = discount,
        TaxRate = 11,
        Tabs =
        [
            new SaveQuotationTabRequest
            {
                Label = "Tab 1", SortOrder = 0,
                Groups = [ new SaveQuotationGroupRequest { Name = "Group 1", SortOrder = 0, Items = [.. items] } ],
            },
        ],
    };

    private static async Task CleanupAsync(AppDbContext db, Guid quotationId)
    {
        var so = await db.SalesOrders.FirstOrDefaultAsync(x => x.QuotationId == quotationId);
        if (so is not null)
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.SalesOrderId == so.Id);
            if (project is not null) db.Projects.Remove(project);
            db.SalesOrders.Remove(so);
        }
        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotationId);
        if (quotation is not null) db.Quotations.Remove(quotation);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateFromQuotationAsync_succeeds_when_totals_match_exactly()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(0,
            new SaveQuotationItemRequest { ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit", MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0 }));

        try
        {
            await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");
            var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);

            so.GrandTotal.Should().Be(quotation.GrandTotal);
        }
        finally
        {
            await CleanupAsync(db, quotation.Id);
        }
    }

    // Real divergence source, not hypothetical: 3 lines at Qty=1 x MaterialPrice=100.4 each.
    // Quotation (RecalcTotals): sums first (301.2) then rounds once => 301.
    // SO (soItems): rounds each line first (100.4 => 100) then sums => 300.
    // Difference = 1, well within the 3-line tolerance (soItems.Count = 3) — must NOT throw.
    [Fact]
    public async Task CreateFromQuotationAsync_allows_small_rounding_divergence_within_line_count_tolerance()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(0,
            new SaveQuotationItemRequest { ItemNo = "1", Equipment = "Line A", Qty = 1, Unit = "unit", MaterialPrice = 100.4m, ServicePrice = 0, SortOrder = 0 },
            new SaveQuotationItemRequest { ItemNo = "2", Equipment = "Line B", Qty = 1, Unit = "unit", MaterialPrice = 100.4m, ServicePrice = 0, SortOrder = 1 },
            new SaveQuotationItemRequest { ItemNo = "3", Equipment = "Line C", Qty = 1, Unit = "unit", MaterialPrice = 100.4m, ServicePrice = 0, SortOrder = 2 }));

        try
        {
            quotation.TotalMaterial.Should().Be(301); // sum-then-round: 301.2 -> 301

            await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");
            var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId); // must NOT throw

            so.Should().NotBeNull();
        }
        finally
        {
            await CleanupAsync(db, quotation.Id);
        }
    }

    [Fact]
    public async Task CreateFromQuotationAsync_throws_when_GrandTotal_diverges_beyond_tolerance()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(0,
            new SaveQuotationItemRequest { ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit", MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0 }));

        try
        {
            await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");

            // Simulate a future #41-shaped bug: directly corrupt the persisted GrandTotal so it
            // diverges from what soItems would independently compute — without touching any
            // production code path (CreateFromQuotationAsync itself is never modified here).
            var dbQuotation = await db.Quotations.FirstAsync(q => q.Id == quotation.Id);
            dbQuotation.GrandTotal += 1_000_000;
            await db.SaveChangesAsync();

            var act = () => salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*SalesOrder tidak bisa dibuat*");
        }
        finally
        {
            await CleanupAsync(db, quotation.Id);
        }
    }

    // The Discount bug found en route: Quotation.Discount was never applied in
    // CreateFromQuotationAsync, so SO.Total silently ignored it. Fixed alongside the assertion
    // above (the assertion itself would have caught the old behavior for any Discount>0).
    [Fact]
    public async Task CreateFromQuotationAsync_applies_Quotation_Discount_to_SO_Total()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(20,
            new SaveQuotationItemRequest { ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit", MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0 }));

        try
        {
            // Subtotal 1.000.000, Discount 20% = 200.000, TotalBeforeTax 800.000, PPN 11% =
            // 88.000, GrandTotal 888.000 — NOT 1.110.000 (what the undiscounted bug would produce).
            quotation.GrandTotal.Should().Be(888_000);

            await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");
            var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);

            so.GrandTotal.Should().Be(888_000);
            so.GrandTotal.Should().Be(quotation.GrandTotal);
        }
        finally
        {
            await CleanupAsync(db, quotation.Id);
        }
    }
}
