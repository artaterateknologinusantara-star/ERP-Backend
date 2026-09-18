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
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.DTOs.SalesOrderPayment;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Invoice Termin (BOQ tahap/percentage-of-total billing): Quotation dengan QuotationTermin
// terstruktur -> di-copy ke SalesOrderTermin saat convert -> tiap termin cuma bisa di-invoice
// SEKALI, dan total Invoice + DP existing tidak boleh melebihi SalesOrder.Total. Scratch-DB
// (SynteraERP_Scratch, sama seperti test lain di file ini) -- lihat CreateScratchServices().
public class InvoiceTerminTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public InvoiceTerminTests(WebApplicationFactory<Program> factory)
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
    public async Task Quotation_termins_flow_to_SO_gate_invoice_per_termin_and_reject_over_total()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();
        var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var dpSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderPaymentService>();

        // ── 1. Quotation dengan 3 termin 30/40/30 ──
        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = "Test Invoice Termin " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Discount = 0,
            TaxRate = 11,
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
                            Name = "Group 1",
                            SortOrder = 0,
                            Items =
                            [
                                new SaveQuotationItemRequest
                                {
                                    ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                                    MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
            Termins =
            [
                new SaveQuotationTerminRequest { SortOrder = 1, Description = "DP", Percentage = 30 },
                new SaveQuotationTerminRequest { SortOrder = 2, Description = "Progress", Percentage = 40 },
                new SaveQuotationTerminRequest { SortOrder = 3, Description = "Pelunasan", Percentage = 30 },
            ],
        });

        quotation.Termins.Should().HaveCount(3);
        quotation.Termins.Sum(t => t.Percentage).Should().Be(100);

        await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");

        // ── 2. Convert ke SO -> SalesOrderTermin harus ikut ter-copy ──
        var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);
        so.Termins.Should().HaveCount(3);
        so.Termins.Select(t => t.Percentage).Should().Equal(30, 40, 30);
        so.Termins.Should().OnlyContain(t => !t.IsInvoiced);

        var termin1 = so.Termins.Single(t => t.SortOrder == 1);
        var termin2 = so.Termins.Single(t => t.SortOrder == 2);
        var termin3 = so.Termins.Single(t => t.SortOrder == 3);

        var expectedTermin1Amount = Math.Round(so.GrandTotal * 30 / 100m, 0, MidpointRounding.AwayFromZero);
        var expectedTermin2Amount = Math.Round(so.GrandTotal * 40 / 100m, 0, MidpointRounding.AwayFromZero);

        // ── 3. Invoice Termin 1 -> Amount = 30% dari SO.Total, 1 baris item ──
        var inv1 = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            SalesOrderTerminId = termin1.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        });
        inv1.Amount.Should().Be(expectedTermin1Amount);
        inv1.Items.Should().HaveCount(1);
        inv1.Items[0].Description.Should().Contain("Termin 1").And.Contain("30%");

        // ── 4. Invoice Termin 1 LAGI -> harus ditolak ──
        var act1 = async () => await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            SalesOrderTerminId = termin1.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        });
        await act1.Should().ThrowAsync<InvalidOperationException>().WithMessage("*sudah pernah ditagih*");

        // ── 5. Invoice Termin 2 -> sukses, total ter-invoice sekarang 70% ──
        var inv2 = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            SalesOrderTerminId = termin2.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        });
        inv2.Amount.Should().Be(expectedTermin2Amount);

        // ── 6. DP 20% + Invoice Termin 3 (30%) = 120% dari SO.Total -> gate baru harus tolak ──
        var dp = await dpSvc.RecordDownPaymentAsync(so.Id, new RecordDownPaymentRequest
        {
            Amount = Math.Round(so.GrandTotal * 20 / 100m, 0, MidpointRounding.AwayFromZero),
            Method = "Transfer",
        });

        var act2 = async () => await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            SalesOrderTerminId = termin3.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        });
        await act2.Should().ThrowAsync<InvalidOperationException>().WithMessage("*melebihi Total Sales Order*");

        // ── 7. Hapus DP percobaan itu, lalu Invoice Termin 3 harus sukses (pas 100%) ──
        db.SalesOrderPayments.Remove(await db.SalesOrderPayments.FirstAsync(x => x.Id == dp.Id));
        await db.SaveChangesAsync();

        var inv3 = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            SalesOrderTerminId = termin3.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        });
        inv3.Amount.Should().BeGreaterThan(0);

        var soAfter = await salesOrderSvc.GetByIdAsync(so.Id);
        soAfter!.Termins.Should().OnlyContain(t => t.IsInvoiced);

        // ── 8. Backward-compat: SO tanpa SalesOrderTermin tetap bisa full-amount seperti sebelumnya ──
        var soNoTermin = new Models.SalesOrder
        {
            Id = Guid.NewGuid(),
            No = "TST-SO-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId,
            ProjectName = "SO Tanpa Termin",
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            SalesId = SeededAdminId,
            Status = Models.SalesOrderStatus.Open,
            Total = 5_000_000,
        };
        db.SalesOrders.Add(soNoTermin);
        await db.SaveChangesAsync();

        var invNoTermin = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = SeededCustomerId,
            SalesOrderId = soNoTermin.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 5_000_000,
        });
        invNoTermin.SalesOrderTerminId.Should().BeNull();
        invNoTermin.Amount.Should().Be(5_000_000);

        // ── Cleanup — hapus semua data uji ──
        db.Invoices.RemoveRange(await db.Invoices.Where(i =>
            i.Id == inv1.Id || i.Id == inv2.Id || i.Id == inv3.Id || i.Id == invNoTermin.Id).ToListAsync());
        db.SalesOrders.RemoveRange(await db.SalesOrders.Where(x => x.Id == so.Id || x.Id == soNoTermin.Id).ToListAsync());
        db.Quotations.RemoveRange(await db.Quotations.Where(x => x.Id == quotation.Id).ToListAsync());
        db.Projects.RemoveRange(await db.Projects.Where(p => p.SalesOrderId == so.Id).ToListAsync());
        await db.SaveChangesAsync();
    }
}
