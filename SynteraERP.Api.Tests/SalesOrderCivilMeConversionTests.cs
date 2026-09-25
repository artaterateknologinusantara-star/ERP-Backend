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

// CreateFromQuotationAsync (SalesOrderService) only ever built SalesOrderItem rows from
// QuotationItem — QuotationGroup.FinalSellingPrice and QuotationWorkDetail.TotalHarga (BOQ),
// both of which RecalcTotals (see QuotationCivilMeTotalsTests) folds into a Civil & ME
// Quotation's GrandTotal, were never represented on the resulting SalesOrder. A Quotation
// approved with real BOQ/subcon-selling-price value converted into a SalesOrder silently
// missing that value — confirmed against real scratch-DB data (Q.SYN-26.0333 -> SO.SYN-26.0111,
// short by Rp 8.880.000). Fixed by folding FinalSellingPrice+WorkDetail into one non-shippable
// lump-sum SalesOrderItem during conversion.
public class SalesOrderCivilMeConversionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public SalesOrderCivilMeConversionTests(WebApplicationFactory<Program> factory)
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

    // Skenario (a): grup Civil ME murni WorkDetail, NOL QuotationItem — kasus persis yang
    // ditanyakan investigasi #41 (reachable: Approve tidak menolak, GrandTotal>0).
    [Fact]
    public async Task CivilMe_zero_QuotationItem_pure_WorkDetail_converts_to_SO_with_matching_total()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Test CivilMe ZeroItem " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Discount = 0,
            TaxRate = 11,
            IsCivilMeMode = true,
            Tabs =
            [
                new SaveQuotationTabRequest
                {
                    Label = "Tab 1", SortOrder = 0,
                    Groups =
                    [
                        new SaveQuotationGroupRequest
                        {
                            Name = "Group 1", SortOrder = 0,
                            Items = [], // sengaja kosong
                            WorkItems =
                            [
                                new SaveQuotationWorkItemRequest
                                {
                                    Name = "Pekerjaan Sipil", SortOrder = 0,
                                    WorkDetails =
                                    [
                                        new SaveQuotationWorkDetailRequest
                                        {
                                            Name = "Cor Beton", Volume = 10, Unit = "m3",
                                            ServicePrice = 300_000, MaterialPrice = 200_000, SortOrder = 0,
                                        },
                                    ],
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        try
        {
            // Zero QuotationItem, tapi WorkDetail.MaterialPrice tetap kontribusi ke TotalMaterial
            // (task #44) — "zero item" di nama test ini soal QuotationItem, bukan soal Material.
            quotation.TotalMaterial.Should().Be(2_000_000); // 10*200.000
            quotation.GrandTotal.Should().BeGreaterThan(0); // 10*(300.000+200.000) = 5.000.000, +11% = 5.550.000
            quotation.GrandTotal.Should().Be(5_550_000);

            await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");
            var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);

            so.Items.Should().ContainSingle(); // hanya baris lump-sum
            so.Items[0].Description.Should().Contain("Jasa/BOQ");
            so.GrandTotal.Should().Be(quotation.GrandTotal);

            var soEntity = await db.SalesOrders.FirstAsync(x => x.Id == so.Id);
            soEntity.Total.Should().Be(quotation.GrandTotal);
        }
        finally
        {
            await CleanupAsync(db, quotation.Id);
        }
    }

    // Skenario (b): campuran QuotationItem + WorkDetail (+ FinalSellingPrice) dalam satu Quotation
    // — reproduksi pola nyata yang ditemukan di scratch DB (Q.SYN-26.0333).
    [Fact]
    public async Task CivilMe_mixed_Item_and_WorkDetail_and_FinalSellingPrice_converts_to_SO_with_matching_total()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Test CivilMe Mixed " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Discount = 0,
            TaxRate = 11,
            IsCivilMeMode = true,
            Tabs =
            [
                new SaveQuotationTabRequest
                {
                    Label = "Tab 1", SortOrder = 0,
                    Groups =
                    [
                        new SaveQuotationGroupRequest
                        {
                            Name = "Group 1", SortOrder = 0,
                            FinalSellingPrice = 2_000_000,
                            Items =
                            [
                                new SaveQuotationItemRequest
                                {
                                    ItemNo = "1", Equipment = "Kabel Tray", Qty = 1, Unit = "unit",
                                    MaterialPrice = 15_000_000, ServicePrice = 0, SortOrder = 0,
                                },
                            ],
                            WorkItems =
                            [
                                new SaveQuotationWorkItemRequest
                                {
                                    Name = "Pekerjaan Sipil", SortOrder = 0,
                                    WorkDetails =
                                    [
                                        new SaveQuotationWorkDetailRequest
                                        {
                                            Name = "Cor Beton", Volume = 16, Unit = "m3",
                                            ServicePrice = 300_000, MaterialPrice = 200_000, SortOrder = 0,
                                        },
                                    ],
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        try
        {
            // Material = 15.000.000 (item) + 16*200.000 (WD) = 18.200.000
            // Service  = 2.000.000 (FSP) + 0 (item service) + 16*300.000 (WD) = 6.800.000
            // Subtotal = 25.000.000, PPN 11% = 2.750.000, GrandTotal = 27.750.000
            quotation.GrandTotal.Should().Be(27_750_000);

            await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");
            var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);

            so.Items.Should().HaveCount(2); // 1 QuotationItem + 1 lump-sum (FSP+WD)
            so.GrandTotal.Should().Be(quotation.GrandTotal);

            var soEntity = await db.SalesOrders.FirstAsync(x => x.Id == so.Id);
            soEntity.Total.Should().Be(quotation.GrandTotal);
        }
        finally
        {
            await CleanupAsync(db, quotation.Id);
        }
    }

    // Skenario (c) — regresi: Quotation standard (bukan Civil ME), tanpa WorkDetail sama sekali.
    // SO.Items/Total harus persis seperti sebelum fix ini (tidak ada lump-sum ditambahkan).
    [Fact]
    public async Task Standard_mode_quotation_without_WorkDetail_is_unaffected_by_the_fix()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Test Standard Regression " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Discount = 0,
            TaxRate = 11,
            IsCivilMeMode = false,
            Tabs =
            [
                new SaveQuotationTabRequest
                {
                    Label = "Tab 1", SortOrder = 0,
                    Groups =
                    [
                        new SaveQuotationGroupRequest
                        {
                            Name = "Group 1", SortOrder = 0,
                            Items =
                            [
                                new SaveQuotationItemRequest
                                {
                                    ItemNo = "1", Equipment = "Switch 24 port", Qty = 3, Unit = "unit",
                                    MaterialPrice = 1_000_000, ServicePrice = 200_000, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        try
        {
            await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");
            var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);

            so.Items.Should().ContainSingle(); // tidak ada lump-sum tambahan
            so.GrandTotal.Should().Be(quotation.GrandTotal);
        }
        finally
        {
            await CleanupAsync(db, quotation.Id);
        }
    }
}
