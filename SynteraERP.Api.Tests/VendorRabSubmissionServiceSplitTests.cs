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
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Task #44 Bagian 2 (Opsi B, 26 Sep 2026): vendor submit ServicePrice/MaterialPrice terpisah
// (bukan 1 UnitPrice blended), maincon markup juga per-kategori (ServiceMarkup/MaterialMarkup).
// Menutup end-to-end: submit vendor -> markup maincon -> approve -> QuotationWorkDetail resmi
// harus punya ServicePrice/MaterialPrice yang benar (FinalServicePrice/FinalMaterialPrice dari
// ApproveAsync), dan Quotation.RecalcTotals (task #44 Bagian 1) ikut merefleksikannya.
public class VendorRabSubmissionServiceSplitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededSupplierId = new("60000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public VendorRabSubmissionServiceSplitTests(WebApplicationFactory<Program> factory)
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
    public async Task Vendor_split_price_plus_split_markup_flows_correctly_into_WorkDetail_on_approve()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await SupplierSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var vendorSubmissionSvc = scope.ServiceProvider.GetRequiredService<IVendorRabSubmissionService>();

        // ── 1. Quotation Civil ME kosong (grup belum ada WorkItem/WorkDetail) ──
        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Test VendorRab Split " + Guid.NewGuid().ToString("N")[..6],
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
                    Groups = [ new SaveQuotationGroupRequest { Name = "Group 1", SortOrder = 0 } ],
                },
            ],
        });
        var groupId = quotation.Tabs[0].Groups[0].Id;

        // ── 2. VendorRabRequest + 1 Line, dibangun langsung (bypass endpoint create/send —
        //      di luar scope test ini) + 1 SupplierPortalUser sebagai pengirim submission ──
        var portalUser = new SupplierPortalUser
        {
            SupplierId = SeededSupplierId,
            Name = "Test Vendor PIC",
            Email = $"pic-{Guid.NewGuid():N}@example.com",
            PasswordHash = "x",
        };
        db.SupplierPortalUsers.Add(portalUser);

        var rabRequest = new VendorRabRequest
        {
            QuotationGroupId = groupId,
            SupplierId = SeededSupplierId,
            Name = "Pekerjaan Sipil",
            Status = VendorRabRequestStatus.Sent,
            SentAt = DateTimeOffset.UtcNow,
        };
        var requestLine = new VendorRabRequestLine
        {
            VendorRabRequest = rabRequest,
            Name = "Cor Beton", Volume = 10, Unit = "m3", SortOrder = 0,
        };
        rabRequest.Lines.Add(requestLine);
        db.VendorRabRequests.Add(rabRequest);
        await db.SaveChangesAsync();

        // ── 3. Vendor submit: ServicePrice 300rb + MaterialPrice 200rb per unit (BUKAN 1 angka
        //      blended — ini inti Opsi B) ──
        var submission = await vendorSubmissionSvc.CreateAsync(
            rabRequest.Id, SeededSupplierId, portalUser.Id,
            new CreateVendorRabSubmissionRequest
            {
                Lines =
                [
                    new CreateVendorRabSubmissionLineRequest
                    {
                        VendorRabRequestLineId = requestLine.Id,
                        ServicePrice = 300_000,
                        MaterialPrice = 200_000,
                    },
                ],
            });

        var submissionLineId = submission.Lines[0].Id;

        try
        {
            // ── 4. Maincon markup per-kategori: +50rb Jasa, +20rb Material ──
            var markupOk = await vendorSubmissionSvc.SetLineMarkupAsync(
                submission.Id, submissionLineId, serviceMarkup: 50_000, materialMarkup: 20_000);
            markupOk.Should().BeTrue();

            // ── 5. Approve — FinalServicePrice=350rb, FinalMaterialPrice=220rb harus masuk
            //      QuotationWorkDetail.ServicePrice/MaterialPrice apa adanya ──
            var workItemId = await vendorSubmissionSvc.ApproveAsync(submission.Id, SeededAdminId);

            var reloaded = await quotationSvc.GetByIdAsync(quotation.Id);
            var workItem = reloaded!.Tabs[0].Groups[0].WorkItems.Should().ContainSingle().Subject;
            workItem.Id.Should().Be(workItemId);
            var detail = workItem.WorkDetails.Should().ContainSingle().Subject;

            detail.ServicePrice.Should().Be(350_000);
            detail.MaterialPrice.Should().Be(220_000);
            detail.TotalHarga.Should().Be(10 * (350_000m + 220_000m)); // 5.700.000

            // RecalcTotals (task #44 Bagian 1) harus ikut split ini, bukan nyemplung semua ke satu
            // kategori: TotalMaterial = 10*220rb = 2.200.000, TotalService = 10*350rb = 3.500.000
            // (FinalSellingPrice grup ini null/0, tidak ada QuotationItem).
            reloaded.TotalMaterial.Should().Be(2_200_000);
            reloaded.TotalService.Should().Be(3_500_000);
            reloaded.GrandTotal.Should().Be(6_327_000); // 5.700.000 * 1.11
        }
        finally
        {
            // Cleanup, urutan wajib: VendorRabSubmissionLine.VendorRabRequestLineId FK adalah
            // Restrict (bukan Cascade), jadi Submission+Lines harus dihapus manual DULU sebelum
            // VendorRabRequest (yang baru bisa cascade-hapus Lines-nya sendiri dengan aman) ->
            // SupplierPortalUser -> Quotation (cascade Tabs/Groups/WorkItems/WorkDetails).
            var submissionIds = await db.VendorRabSubmissions
                .Where(s => s.VendorRabRequestId == rabRequest.Id).Select(s => s.Id).ToListAsync();
            await db.VendorRabSubmissionLines.Where(l => submissionIds.Contains(l.VendorRabSubmissionId)).ExecuteDeleteAsync();
            await db.VendorRabSubmissions.Where(s => s.VendorRabRequestId == rabRequest.Id).ExecuteDeleteAsync();

            // ExecuteDeleteAsync bypasses the change tracker (bulk SQL DELETE) — clear it so the
            // tracked VendorRabRequestLine/VendorRabRequest below don't see stale in-memory
            // references to the SubmissionLines that were just deleted server-side.
            db.ChangeTracker.Clear();

            var toDeleteRequest = await db.VendorRabRequests.FirstOrDefaultAsync(r => r.Id == rabRequest.Id);
            if (toDeleteRequest is not null) db.VendorRabRequests.Remove(toDeleteRequest);
            await db.SaveChangesAsync();

            var toDeletePortalUser = await db.SupplierPortalUsers.FirstOrDefaultAsync(u => u.Id == portalUser.Id);
            if (toDeletePortalUser is not null) db.SupplierPortalUsers.Remove(toDeletePortalUser);
            await db.SaveChangesAsync();

            var toDeleteQuotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (toDeleteQuotation is not null) db.Quotations.Remove(toDeleteQuotation);
            await db.SaveChangesAsync();
        }
    }
}
