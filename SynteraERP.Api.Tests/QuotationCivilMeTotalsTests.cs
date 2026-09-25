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

// Civil & ME Total kategori harus menjumlah 3 sumber: FinalSellingPrice (Subkontraktor SOW) +
// QuotationItem (equipment/material, sama seperti mode standard) + QuotationWorkDetail/BOQ
// (TotalHarga). Sebelum fix ini, RecalcTotals cuma sum FinalSellingPrice, dan UpdateAsync/
// DuplicateAsync/CreateRevisionAsync tidak nge-Include WorkItems sama sekali (jadi walau
// formula dibenerin, WorkDetail tetap tidak ke-hitung di path yang paling sering dipakai —
// "Submit Penawaran" pada draft yang sudah punya isi RAB/BQ).
public class QuotationCivilMeTotalsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public QuotationCivilMeTotalsTests(WebApplicationFactory<Program> factory)
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
    public async Task CivilMe_group_total_sums_FinalSellingPrice_plus_Item_plus_WorkDetail()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        // ── 1. Quotation Civil ME, 1 grup: FinalSellingPrice (Subkontraktor SOW) + 1 QuotationItem ──
        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Test Civil ME Totals " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Discount = 0,
            TaxRate = 11,
            IsCivilMeMode = true,
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
                            Name = "Pekerjaan Campuran",
                            SortOrder = 0,
                            FinalSellingPrice = 10_000_000,
                            Items =
                            [
                                new SaveQuotationItemRequest
                                {
                                    ItemNo = "1", Equipment = "Panel Listrik", Qty = 2, Unit = "unit",
                                    ServicePrice = 100_000, MaterialPrice = 50_000, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        var groupId = quotation.Tabs[0].Groups[0].Id;

        // ── 2. Tambah WorkItem/WorkDetail (BOQ) ke grup yang sama, lewat endpoint CRUD terpisah
        //      persis seperti alur "Isi Detail RAB/BQ" di UI ──
        var workItem = await quotationSvc.CreateWorkItemAsync(groupId, new SaveWorkItemRequest { Name = "Pemasangan Kabel", SortOrder = 0 });
        await quotationSvc.CreateWorkDetailAsync(workItem.Id, new SaveWorkDetailRequest
        {
            Name = "Kabel NYY 4x6mm", Spesifikasi = "Supreme", Volume = 5, Unit = "meter",
            ServicePrice = 120_000, MaterialPrice = 80_000, SortOrder = 0,
        });

        // ── 3. "Submit Penawaran" lagi (UpdateAsync) — path paling sering dipakai user setelah
        //      isi RAB/BQ, dan yang tadinya tidak nge-Include WorkItems sama sekali ──
        var updated = await quotationSvc.UpdateAsync(quotation.Id, new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = quotation.ProjectName,
            Date = quotation.Date,
            ValidUntil = quotation.ValidUntil,
            Discount = 0,
            TaxRate = 11,
            IsCivilMeMode = true,
            Tabs =
            [
                new SaveQuotationTabRequest
                {
                    Id = quotation.Tabs[0].Id,
                    Label = "Tab 1",
                    SortOrder = 0,
                    Groups =
                    [
                        new SaveQuotationGroupRequest
                        {
                            Id = groupId, // keep same Group.Id so WorkItems/WorkDetails survive
                            Name = "Pekerjaan Campuran",
                            SortOrder = 0,
                            FinalSellingPrice = 10_000_000,
                            Items =
                            [
                                new SaveQuotationItemRequest
                                {
                                    ItemNo = "1", Equipment = "Panel Listrik", Qty = 2, Unit = "unit",
                                    ServicePrice = 100_000, MaterialPrice = 50_000, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        // Expected: Material = Item.Qty*MaterialPrice + WorkDetail.Volume*MaterialPrice
        //                    = (2*50.000) + (5*80.000) = 100.000 + 400.000 = 500.000
        //           Service  = FinalSellingPrice + Item.Qty*ServicePrice + WorkDetail.Volume*ServicePrice
        //                    = 10.000.000 + (2*100.000) + (5*120.000) = 10.000.000 + 200.000 + 600.000 = 10.800.000
        //           Subtotal = 11.300.000, PPN 11% = 1.243.000, GrandTotal = 12.543.000
        updated!.TotalMaterial.Should().Be(500_000);
        updated.TotalService.Should().Be(10_800_000);
        updated.TotalBeforeTax.Should().Be(11_300_000);
        updated.TaxAmount.Should().Be(1_243_000);
        updated.GrandTotal.Should().Be(12_543_000);

        // Confirms the WorkDetail row survived the update (Include-fix) and is still attached
        // to the same Group rather than orphaned/lost.
        var reloaded = await quotationSvc.GetByIdAsync(quotation.Id);
        reloaded!.Tabs[0].Groups[0].WorkItems.Should().ContainSingle()
            .Which.WorkDetails.Should().ContainSingle(d => d.Name == "Kabel NYY 4x6mm");

        // ── Cleanup ──
        var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
        if (toDelete is not null)
        {
            db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }
}
