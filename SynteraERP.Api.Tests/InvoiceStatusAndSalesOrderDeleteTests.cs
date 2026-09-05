using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.DTOs.SalesOrderPayment;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Scratch-DB (SQL Server SynteraERP_Scratch, sama seperti test lain di file ini) untuk P1-B
// (Invoice Draft payment gap) + P1-F (dangling FK ke SalesOrder soft-deleted). Tidak menyentuh
// dev/production DB -- lihat CreateScratchServices().
public class InvoiceStatusAndSalesOrderDeleteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public InvoiceStatusAndSalesOrderDeleteTests(WebApplicationFactory<Program> factory)
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

    private static SalesOrder NewSalesOrder(string no) => new()
    {
        Id = Guid.NewGuid(),
        No = no,
        CustomerId = SeededCustomerId,
        ProjectName = "Test Project " + no,
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        SalesId = SeededAdminId,
        Status = SalesOrderStatus.Open,
        Total = 10_000_000,
    };

    [Fact]
    public async Task Invoice_Draft_to_Sent_payment_gating_and_overdue_flip_skips_draft()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

        // ── Draft: RecordPayment ditolak ──
        var inv = new Invoice
        {
            Id = Guid.NewGuid(),
            No = "TST-INV-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 1_000_000,
            Status = InvoiceStatus.Draft,
        };
        db.Invoices.Add(inv);
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() => invoiceSvc.RecordPaymentAsync(inv.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = 100_000,
            Method = "Transfer",
        })).Should().ThrowAsync<InvalidOperationException>().WithMessage("*belum dikirim*");

        // ── MarkAsSentAsync: Draft -> Sent ──
        var sent = await invoiceSvc.MarkAsSentAsync(inv.Id);
        sent!.Status.Should().Be("Sent");

        // ── MarkAsSentAsync lagi ditolak (status sekarang bukan Draft) ──
        await FluentActions.Invoking(() => invoiceSvc.MarkAsSentAsync(inv.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        // ── Sent: RecordPayment sebagian berhasil ──
        var partial = await invoiceSvc.RecordPaymentAsync(inv.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = 400_000,
            Method = "Transfer",
        });
        partial!.Status.Should().Be("Partial Paid");
        partial.Paid.Should().Be(400_000);

        // ── Lunasi sisanya ──
        var paid = await invoiceSvc.RecordPaymentAsync(inv.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = 600_000,
            Method = "Transfer",
        });
        paid!.Status.Should().Be("Paid");

        // ── Paid: RecordPayment lagi ditolak ──
        await FluentActions.Invoking(() => invoiceSvc.RecordPaymentAsync(inv.Id, new RecordPaymentRequest
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = 1,
            Method = "Transfer",
        })).Should().ThrowAsync<InvalidOperationException>().WithMessage("*sudah lunas*");

        // ── Overdue flip: Draft yang lewat DueDate TIDAK boleh ikut ter-flip ──
        var overdueDraft = new Invoice
        {
            Id = Guid.NewGuid(),
            No = "TST-INV-OD-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            Amount = 500_000,
            Status = InvoiceStatus.Draft,
        };
        // Kontrol negatif: Sent yang lewat DueDate HARUS tetap ke-flip (pastikan fix tidak
        // sengaja menonaktifkan seluruh logic Overdue, cuma exclude Draft).
        var overdueSent = new Invoice
        {
            Id = Guid.NewGuid(),
            No = "TST-INV-OS-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            Amount = 500_000,
            Status = InvoiceStatus.Sent,
        };
        db.Invoices.AddRange(overdueDraft, overdueSent);
        await db.SaveChangesAsync();

        var overdueSvc = new InvoiceOverdueStatusService(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<InvoiceOverdueStatusService>.Instance);
        var method = typeof(InvoiceOverdueStatusService)
            .GetMethod("UpdateOverdueInvoicesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(overdueSvc, new object[] { CancellationToken.None })!;

        var refreshedDraft = await db.Invoices.AsNoTracking().FirstAsync(x => x.Id == overdueDraft.Id);
        refreshedDraft.Status.Should().Be(InvoiceStatus.Draft, "Draft belum \"dikirim\", tidak boleh ikut ter-flip ke Overdue");

        var refreshedSent = await db.Invoices.AsNoTracking().FirstAsync(x => x.Id == overdueSent.Id);
        refreshedSent.Status.Should().Be(InvoiceStatus.Overdue, "kontrol negatif: Sent yang lewat DueDate tetap harus ke-flip");
    }

    [Fact]
    public async Task RetentionRelease_and_DownPaymentApplication_unaffected_by_new_status_guard()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var so = NewSalesOrder("TST-SO-RR-" + Guid.NewGuid().ToString("N")[..6]);
        so.RetentionPercentage = 10;
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

        // Invoice sengaja dibiarkan Draft + punya RetentionAmount > 0.
        var inv = new Invoice
        {
            Id = Guid.NewGuid(),
            No = "TST-INV-RR-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId,
            SalesOrderId = so.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 1_100_000,
            RetentionAmount = 100_000,
            Status = InvoiceStatus.Draft,
        };
        db.Invoices.Add(inv);
        await db.SaveChangesAsync();

        // ReleaseRetentionAsync TIDAK lewat RecordPaymentAsync -- harus tetap berhasil meski
        // invoice masih Draft (mengonfirmasi guard baru tidak menyentuh jalur ini).
        var released = await invoiceSvc.ReleaseRetentionAsync(inv.Id, new RetentionReleaseRequest
        {
            ReleaseDate = DateTimeOffset.UtcNow,
            Amount = 50_000,
        });
        released!.RetentionReleasedAmount.Should().Be(50_000);
        released.Status.Should().Be("Draft", "ReleaseRetentionAsync tidak pernah mengubah Status invoice");

        // Down Payment application juga TIDAK lewat RecordPaymentAsync (lihat komentar di
        // Invoice.ApplyPayment) -- harus tetap berhasil meski invoice masih Draft.
        var dpSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderPaymentService>();
        var dp = await dpSvc.RecordDownPaymentAsync(so.Id, new RecordDownPaymentRequest
        {
            Amount = 300_000,
            Method = "Transfer",
        });

        var applied = await dpSvc.ApplyToInvoiceAsync(inv.Id, new ApplyDownPaymentRequest
        {
            SalesOrderPaymentId = dp.Id,
            AmountToApply = 300_000,
        });
        applied!.Paid.Should().Be(300_000);
        applied.Status.Should().Be("Partial Paid", "ApplyPayment tetap jalan lewat jalur DP, terlepas dari guard baru di RecordPaymentAsync");
    }

    [Fact]
    public async Task SalesOrder_Delete_blocked_by_active_children_and_allowed_after_soft_delete()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var soSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        // ── Project aktif ──
        var soA = NewSalesOrder("TST-SO-A-" + Guid.NewGuid().ToString("N")[..6]);
        db.SalesOrders.Add(soA);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Code = "TST-PRJ-" + Guid.NewGuid().ToString("N")[..6],
            Name = "Test Project",
            CustomerId = SeededCustomerId,
            SalesOrderId = soA.Id,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() => soSvc.DeleteAsync(soA.Id))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*Project aktif*");

        project.IsDeleted = true;
        await db.SaveChangesAsync();

        await soSvc.DeleteAsync(soA.Id); // sekarang harus berhasil
        (await db.SalesOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == soA.Id)).IsDeleted.Should().BeTrue();

        // ── Invoice aktif ──
        var soB = NewSalesOrder("TST-SO-B-" + Guid.NewGuid().ToString("N")[..6]);
        db.SalesOrders.Add(soB);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            No = "TST-INV-B-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId,
            SalesOrderId = soB.Id,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Amount = 500_000,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() => soSvc.DeleteAsync(soB.Id))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invoice aktif*");

        invoice.IsDeleted = true;
        await db.SaveChangesAsync();

        await soSvc.DeleteAsync(soB.Id);
        (await db.SalesOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == soB.Id)).IsDeleted.Should().BeTrue();

        // ── PurchaseRequest aktif ──
        var soC = NewSalesOrder("TST-SO-C-" + Guid.NewGuid().ToString("N")[..6]);
        db.SalesOrders.Add(soC);
        var pr = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            No = "TST-PR-C-" + Guid.NewGuid().ToString("N")[..6],
            SalesOrderId = soC.Id,
            RequestedBy = SeededAdminId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() => soSvc.DeleteAsync(soC.Id))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*Purchase Request aktif*");

        pr.IsDeleted = true;
        await db.SaveChangesAsync();

        await soSvc.DeleteAsync(soC.Id);
        (await db.SalesOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == soC.Id)).IsDeleted.Should().BeTrue();
    }
}
