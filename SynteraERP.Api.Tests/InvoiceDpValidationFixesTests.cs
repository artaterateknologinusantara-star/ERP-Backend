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
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Covers the 4 fixes from the E2E QA pass on Invoice/DP/PDF (2026-09-23):
//   1. CreateAsync rejects a partial Amount for a non-termin SO-with-items invoice instead of
//      silently overwriting it.
//   2. DP + Invoice vs SO.Total is now validated symmetrically regardless of which is recorded
//      first (previously only the Invoice-then-DP direction was checked).
//   3. InvoiceStatusText.Format is the single source of the "Partial Paid" display string.
//   4. ApplyToInvoiceAsync now requires the invoice to be Sent (not Draft), matching
//      RecordPaymentAsync's existing gate.
// Scratch-DB (SynteraERP_Scratch, sama seperti test lain di file ini).
public class InvoiceDpValidationFixesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public InvoiceDpValidationFixesTests(WebApplicationFactory<Program> factory)
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

    private async Task<(SynteraERP.Api.DTOs.SalesOrder.SalesOrderDetailResponse so, decimal grandTotal)> CreateNonTerminSoAsync(
        IServiceProvider services, decimal materialPrice, string label)
    {
        var quotationSvc = services.GetRequiredService<IQuotationService>();
        var salesOrderSvc = services.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = SeededAdminId,
            ProjectName = $"{label} " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Discount = 0,
            TaxRate = 11,
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
                                    ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                                    MaterialPrice = materialPrice, ServicePrice = 0, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });
        await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");
        var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, SeededAdminId);
        return (so, so.GrandTotal);
    }

    [Fact]
    public async Task CreateAsync_rejects_partial_Amount_for_nontermin_SO_with_items()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var (so, grandTotal) = await CreateNonTerminSoAsync(scope.ServiceProvider, 10_000_000, "Bug1 Reject");
        var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

        // Partial amount -> must be rejected, not silently overwritten.
        var actPartial = async () => await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = grandTotal - 1_000_000,
        });
        await actPartial.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*harus mencakup jumlah penuh Sales Order*");

        // Full amount -> succeeds, Amount matches SO items total exactly.
        var inv = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = grandTotal,
        });
        inv.Amount.Should().Be(grandTotal);
    }

    // Revised Bug #2: the invoice-creation cap must be DP-invariant -- whether a DP exists for the
    // SO or not must never change whether the invoice itself gets created. DP is a payment method
    // applied AGAINST an existing invoice (reducing its balance), not a separate charge stacked on
    // top of SO.Total, so it has no place in this particular cap. RecordDownPaymentAsync's own
    // DP-vs-SO.Total cap (a separate, standing rule -- can't receive advance payment beyond the
    // order value) is untouched and still enforced independently.
    [Fact]
    public async Task Invoice_creation_cap_is_DP_invariant_but_still_blocks_double_invoicing()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var dpSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderPaymentService>();

        // ── Baseline: full invoice with NO pre-existing DP -> succeeds. ──
        var (soBaseline, totalBaseline) = await CreateNonTerminSoAsync(scope.ServiceProvider, 10_000_000, "Bug2 Baseline");
        var invBaseline = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = soBaseline.CustomerId,
            SalesOrderId = soBaseline.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = totalBaseline,
        });
        invBaseline.Amount.Should().Be(totalBaseline);

        // ── DP recorded FIRST (well within SO.Total on its own), THEN full Invoice -> must ALSO
        // succeed now -- this is the corrected behavior (previously wrongly rejected). ──
        var (soB, totalB) = await CreateNonTerminSoAsync(scope.ServiceProvider, 10_000_000, "Bug2 DpThenInvoice");
        await dpSvc.RecordDownPaymentAsync(soB.Id, new RecordDownPaymentRequest
        {
            Amount = 1_000_000,
            Method = "Transfer",
        });
        var invAfterDp = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = soB.CustomerId,
            SalesOrderId = soB.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = totalB,
        });
        invAfterDp.Amount.Should().Be(totalB, "the invoice cap must not depend on whether a DP exists for the SO");

        // ── Double-invoicing guard preserved: a SECOND full invoice for an already-fully-invoiced
        // SO must still be rejected -- this cap is about invoices vs SO.Total, not about DP. ──
        var actDoubleInvoice = async () => await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = soBaseline.CustomerId,
            SalesOrderId = soBaseline.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = totalBaseline,
        });
        await actDoubleInvoice.Should().ThrowAsync<InvalidOperationException>().WithMessage("*melebihi Total Sales Order*");

        // ── RecordDownPaymentAsync's own cap (untouched, standing rule) still rejects DP that
        // would push (existing invoices + DP) over SO.Total -- independent of the invoice cap. ──
        var actDpOverCap = async () => await dpSvc.RecordDownPaymentAsync(soBaseline.Id, new RecordDownPaymentRequest
        {
            Amount = 1_000_000,
            Method = "Transfer",
        });
        await actDpOverCap.Should().ThrowAsync<InvalidOperationException>().WithMessage("*melebihi total Sales Order*");
    }

    // Replicates QA report Scenario 1/3: DP received first -> SO later invoiced in full (as
    // required post-Bug #1) -> DP applied to reduce the invoice's balance -> remainder collected
    // via a normal RecordPaymentAsync. This is the real-world flow the DP-Diterapkan-on-PDF
    // feature exists for, and must keep working end to end.
    [Fact]
    public async Task DP_first_then_full_invoice_then_apply_DP_remainder_payable_via_regular_payment()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var dpSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderPaymentService>();

        var (so, grandTotal) = await CreateNonTerminSoAsync(scope.ServiceProvider, 10_000_000, "Bug2 FullLifecycle");
        grandTotal.Should().Be(11_100_000);

        var dp = await dpSvc.RecordDownPaymentAsync(so.Id, new RecordDownPaymentRequest
        {
            Amount = 8_000_000,
            Method = "Transfer",
            Reference = "DP-LIFECYCLE-001",
        });

        var inv = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = grandTotal,
        });
        inv.Amount.Should().Be(11_100_000);

        await invoiceSvc.MarkAsSentAsync(inv.Id);

        var afterDp = await dpSvc.ApplyToInvoiceAsync(inv.Id, new ApplyDownPaymentRequest
        {
            SalesOrderPaymentId = dp.Id,
            AmountToApply = 8_000_000,
        });
        afterDp!.Paid.Should().Be(8_000_000);
        afterDp.Balance.Should().Be(3_100_000);
        afterDp.Status.Should().Be("Partial Paid");

        var afterRegularPayment = await invoiceSvc.RecordPaymentAsync(inv.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = 3_100_000,
            Method = "Transfer",
            Reference = "TRF-REMAINDER-001",
        });
        afterRegularPayment!.Paid.Should().Be(11_100_000);
        afterRegularPayment.Balance.Should().Be(0);
        afterRegularPayment.Status.Should().Be("Paid");
    }

    [Fact]
    public void InvoiceStatusText_Format_adds_space_for_PartialPaid_matches_other_statuses_unchanged()
    {
        InvoiceStatusText.Format(InvoiceStatus.PartialPaid).Should().Be("Partial Paid");
        InvoiceStatusText.Format(InvoiceStatus.Draft).Should().Be("Draft");
        InvoiceStatusText.Format(InvoiceStatus.Sent).Should().Be("Sent");
        InvoiceStatusText.Format(InvoiceStatus.Paid).Should().Be("Paid");
        InvoiceStatusText.Format(InvoiceStatus.Overdue).Should().Be("Overdue");
    }

    [Fact]
    public async Task ApplyToInvoiceAsync_rejects_Draft_invoice_then_succeeds_after_MarkAsSent()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var dpSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderPaymentService>();

        // Raw SO (no SalesOrderItems) -- deliberately isolates the Bug #4 Draft-gate check from
        // Bug #1/#2's SO.Total cap (a non-termin SO-with-items invoice is now always forced to
        // 100% of SO.Total, which would leave zero headroom for any DP; see report).
        var so = new SalesOrder
        {
            Id = Guid.NewGuid(),
            No = "TST-SO-B4-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId,
            ProjectName = "Bug4 DraftGate",
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            SalesId = SeededAdminId,
            Status = SalesOrderStatus.Open,
            Total = 8_000_000,
        };
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        var dp = await dpSvc.RecordDownPaymentAsync(so.Id, new RecordDownPaymentRequest
        {
            Amount = 2_000_000,
            Method = "Transfer",
        });
        var inv = await invoiceSvc.CreateAsync(new CreateInvoiceRequest
        {
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 8_000_000,
        });
        inv.Status.Should().Be("Draft");

        var actDraft = async () => await dpSvc.ApplyToInvoiceAsync(inv.Id, new ApplyDownPaymentRequest
        {
            SalesOrderPaymentId = dp.Id,
            AmountToApply = 2_000_000,
        });
        await actDraft.Should().ThrowAsync<InvalidOperationException>().WithMessage("*belum dikirim*");

        await invoiceSvc.MarkAsSentAsync(inv.Id);

        var applied = await dpSvc.ApplyToInvoiceAsync(inv.Id, new ApplyDownPaymentRequest
        {
            SalesOrderPaymentId = dp.Id,
            AmountToApply = 2_000_000,
        });
        applied!.Paid.Should().Be(2_000_000);
    }
}
