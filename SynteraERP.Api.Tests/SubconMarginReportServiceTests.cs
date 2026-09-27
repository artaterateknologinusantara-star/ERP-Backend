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

// Laporan margin subkontraktor (toggle Per Vendor / Per Quotation) dibaca langsung dari
// VendorRabSubmissionLine (Volume * ServiceMarkup/MaterialMarkup) untuk submission Approved saja
// — sisi Quotation/QuotationWorkDetail cuma menyimpan harga jual akhir yang sudah digabung, jadi
// tidak bisa dipakai untuk menghitung margin. Lihat SubconMarginReportService.
public class SubconMarginReportServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededSupplierId = new("60000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public SubconMarginReportServiceTests(WebApplicationFactory<Program> factory)
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
    public async Task Approved_submission_with_markup_reports_correct_margin_by_vendor_and_by_quotation()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await SupplierSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var reportSvc = scope.ServiceProvider.GetRequiredService<ISubconMarginReportService>();

        // ── 1. Quotation Civil ME kosong, 1 grup ──
        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Test Subcon Margin Report " + Guid.NewGuid().ToString("N")[..6],
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
            Status = VendorRabRequestStatus.Approved,
            SentAt = DateTimeOffset.UtcNow,
        };
        db.VendorRabRequests.Add(rabRequest);
        await db.SaveChangesAsync();

        // Dibuat langsung sebagai Approved (bypass alur submit/review/approve — di luar scope
        // laporan ini, sudah ditutup end-to-end oleh VendorRabSubmissionServiceSplitTests) supaya
        // ReviewedAt bisa dikontrol persis untuk uji filter tanggal.
        var reviewedAt = DateTimeOffset.UtcNow;
        var submission = new VendorRabSubmission
        {
            VendorRabRequestId = rabRequest.Id,
            AttemptNumber = 1,
            Status = VendorRabSubmissionStatus.Approved,
            SubmittedAt = reviewedAt.AddHours(-1),
            SubmittedByPortalUserId = portalUser.Id,
            ReviewedBy = SeededAdminId,
            ReviewedAt = reviewedAt,
        };
        db.VendorRabSubmissions.Add(submission);
        await db.SaveChangesAsync();

        // Volume 1 * (ServicePrice 500rb + ServiceMarkup 200rb + MaterialPrice 250rb + MaterialMarkup
        // 50rb) = NilaiJual 1.000.000; Margin = 1 * (200rb + 50rb) = 250.000 => MarginPercent 25%.
        db.VendorRabSubmissionLines.Add(new VendorRabSubmissionLine
        {
            VendorRabSubmissionId = submission.Id,
            Name = "Cor Beton",
            Volume = 1,
            Unit = "ls",
            SortOrder = 0,
            ServicePrice = 500_000,
            MaterialPrice = 250_000,
            ServiceMarkup = 200_000,
            MaterialMarkup = 50_000,
        });
        await db.SaveChangesAsync();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // ── Per Vendor ──
            var byVendor = await reportSvc.GetByVendorAsync(today, today, null);
            var vendorRow = byVendor.Rows.Should().ContainSingle(r => r.SupplierId == SeededSupplierId).Subject;
            vendorRow.RequestCount.Should().Be(1);
            vendorRow.ApprovedSubmissionCount.Should().Be(1);
            vendorRow.TotalNilaiJual.Should().Be(1_000_000);
            vendorRow.TotalMargin.Should().Be(250_000);
            vendorRow.MarginPercent.Should().Be(25m);

            // ── Per Quotation ──
            var byQuotation = await reportSvc.GetByQuotationAsync(today, today, null);
            var quotationRow = byQuotation.Rows.Should().ContainSingle(r => r.QuotationId == quotation.Id).Subject;
            quotationRow.TotalNilaiJual.Should().Be(1_000_000);
            quotationRow.TotalMargin.Should().Be(250_000);
            var vendorGroup = quotationRow.VendorGroups.Should().ContainSingle().Subject;
            vendorGroup.SupplierId.Should().Be(SeededSupplierId);
            vendorGroup.QuotationGroupId.Should().Be(groupId);
            vendorGroup.TotalMargin.Should().Be(250_000);

            // ── Filter supplier lain -> kosong ──
            var otherSupplierId = new Guid("60000000-0000-0000-0000-000000000002");
            var filteredOut = await reportSvc.GetByVendorAsync(today, today, otherSupplierId);
            filteredOut.Rows.Should().BeEmpty();

            // ── Di luar rentang tanggal -> kosong (submission ini ReviewedAt = hari ini) ──
            var yesterday = today.AddDays(-1);
            var outOfRange = await reportSvc.GetByVendorAsync(yesterday.AddDays(-5), yesterday, null);
            outOfRange.Rows.Should().BeEmpty();
        }
        finally
        {
            db.VendorRabSubmissionLines.RemoveRange(db.VendorRabSubmissionLines.Where(l => l.VendorRabSubmissionId == submission.Id));
            await db.SaveChangesAsync();

            var toDeleteSubmission = await db.VendorRabSubmissions.FirstOrDefaultAsync(s => s.Id == submission.Id);
            if (toDeleteSubmission is not null) db.VendorRabSubmissions.Remove(toDeleteSubmission);
            await db.SaveChangesAsync();

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
