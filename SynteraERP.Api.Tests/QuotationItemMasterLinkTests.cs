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
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// QuotationItem.ItemMasterId (link eksplisit — diisi hanya kalau user memilih dari autocomplete
// katalog, TIDAK PERNAH ditebak otomatis) harus ikut terbawa di setiap jalur yang menyalin baris
// Quotation: konversi ke SalesOrder, Duplicate, dan CreateRevision. Duplicate/CreateRevision
// pernah punya kelas bug yang sama untuk field lain (WorkItems lupa di-copy) — suite ini
// mengunci field ini secara spesifik, bukan cuma smoke test umum.
public class QuotationItemMasterLinkTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public QuotationItemMasterLinkTests(WebApplicationFactory<Program> factory)
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

    private static SaveQuotationRequest BaseRequest(string projectName, Guid itemMasterId, string suffix) => new()
    {
        CustomerId = SeededCustomerId,
        SalesId = SeededAdminId,
        ProjectName = projectName,
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
        Discount = 0,
        TaxRate = 11,
        IsCivilMeMode = false,
        Tabs =
        [
            new SaveQuotationTabRequest
            {
                Label = "Tab 1",
                SortOrder = 0,
                Groups =
                [
                    new SaveQuotationGroupRequest
                    {
                        Name = "Kategori",
                        SortOrder = 0,
                        Items =
                        [
                            new SaveQuotationItemRequest
                            {
                                ItemNo = "1.1",
                                Equipment = $"Managed Switch 24 Port {suffix}",
                                Qty = 1,
                                Unit = "Unit",
                                ServicePrice = 0,
                                MaterialPrice = 1_000_000,
                                SortOrder = 0,
                                ItemMasterId = itemMasterId,
                            },
                        ],
                    },
                ],
            },
        ],
    };

    [Fact]
    public async Task CreateFromQuotationAsync_copies_ItemMasterId_onto_the_SalesOrderItem()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var itemMaster = new ItemMaster
        {
            Id = Guid.NewGuid(), Code = $"SW-{suffix}", Name = $"Managed Switch 24 Port {suffix}",
            Uom = "Unit", Stock = 10, MinStock = 0, SellingPrice = 1_000_000, IsActive = true,
        };
        db.ItemMasters.Add(itemMaster);
        await db.SaveChangesAsync();

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest("Test ItemMasterId link " + suffix, itemMaster.Id, suffix));
        quotation.Tabs[0].Groups[0].Items[0].ItemMasterId.Should().Be(itemMaster.Id, "ToDto must round-trip the link");

        await quotationSvc.SendAsync(quotation.Id, SeededAdminId);
        await quotationSvc.ApproveAsync(quotation.Id, SeededAdminId);

        var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);

        var soItem = await db.SalesOrderItems.AsNoTracking().FirstAsync(x => x.SalesOrderId == so.Id);
        soItem.ItemMasterId.Should().Be(itemMaster.Id, "SO line converted from a linked Quotation line must carry the link — no more guessing needed downstream in DO creation");

        await CleanupAsync(db, quotation.Id, so.Id, itemMaster.Id);
    }

    [Fact]
    public async Task DuplicateAsync_copies_ItemMasterId_onto_the_new_QuotationItem()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var itemMaster = new ItemMaster
        {
            Id = Guid.NewGuid(), Code = $"SW-{suffix}", Name = $"Managed Switch 24 Port {suffix}",
            Uom = "Unit", Stock = 10, MinStock = 0, SellingPrice = 1_000_000, IsActive = true,
        };
        db.ItemMasters.Add(itemMaster);
        await db.SaveChangesAsync();

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var quotation = await quotationSvc.CreateAsync(BaseRequest("Test Duplicate link " + suffix, itemMaster.Id, suffix));

        var duplicate = await quotationSvc.DuplicateAsync(quotation.Id);

        duplicate.Tabs[0].Groups[0].Items[0].ItemMasterId.Should().Be(itemMaster.Id, "same bug class as the WorkItems-not-copied incident — must not regress silently");

        // duplicate.ParentId points at the original quotation (NoAction FK) — must delete the
        // child (duplicate) before the parent, or the cleanup itself throws an FK violation.
        await CleanupQuotationOnly(db, duplicate.Id);
        await CleanupAsync(db, quotation.Id, null, itemMaster.Id);
    }

    [Fact]
    public async Task CreateRevisionAsync_copies_ItemMasterId_onto_the_new_revision_QuotationItem()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var itemMaster = new ItemMaster
        {
            Id = Guid.NewGuid(), Code = $"SW-{suffix}", Name = $"Managed Switch 24 Port {suffix}",
            Uom = "Unit", Stock = 10, MinStock = 0, SellingPrice = 1_000_000, IsActive = true,
        };
        db.ItemMasters.Add(itemMaster);
        await db.SaveChangesAsync();

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var quotation = await quotationSvc.CreateAsync(BaseRequest("Test Revision link " + suffix, itemMaster.Id, suffix));
        await quotationSvc.SendAsync(quotation.Id, SeededAdminId);

        var revision = await quotationSvc.CreateRevisionAsync(quotation.Id);

        revision!.Tabs[0].Groups[0].Items[0].ItemMasterId.Should().Be(itemMaster.Id, "same bug class as the WorkItems-not-copied incident — must not regress silently");

        // revision.ParentId points at the original quotation (NoAction FK) — must delete the
        // child (revision) before the parent, or the cleanup itself throws an FK violation.
        await CleanupQuotationOnly(db, revision.Id);
        await CleanupAsync(db, quotation.Id, null, itemMaster.Id);
    }

    private static async Task CleanupAsync(AppDbContext db, Guid quotationId, Guid? salesOrderId, Guid itemMasterId)
    {
        if (salesOrderId.HasValue)
        {
            var so = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == salesOrderId.Value);
            if (so is not null) db.SalesOrders.Remove(so);
        }

        var quotation = await db.Quotations.FirstOrDefaultAsync(x => x.Id == quotationId);
        if (quotation is not null) db.Quotations.Remove(quotation);

        var itemMaster = await db.ItemMasters.FirstOrDefaultAsync(x => x.Id == itemMasterId);
        if (itemMaster is not null) db.ItemMasters.Remove(itemMaster);

        await db.SaveChangesAsync();
    }

    private static async Task CleanupQuotationOnly(AppDbContext db, Guid quotationId)
    {
        var quotation = await db.Quotations.FirstOrDefaultAsync(x => x.Id == quotationId);
        if (quotation is not null)
        {
            db.Quotations.Remove(quotation);
            await db.SaveChangesAsync();
        }
    }
}
