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
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Scratch-DB (SQL Server SynteraERP_Scratch, sama seperti InvoiceStatusAndSalesOrderDeleteTests)
// untuk perketatan guard SalesOrderService.UpdateStatusAsync: SO Completed sekarang butuh semua
// Invoice aktif LUNAS di luar retensi (Paid >= Amount - RetentionAmount), bukan cuma EXISTS.
// Tidak menyentuh dev/production DB.
public class SalesOrderCompletionInvoiceLunasTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public SalesOrderCompletionInvoiceLunasTests(WebApplicationFactory<Program> factory)
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
        Status = SalesOrderStatus.Delivered,
        Total = 10_000_000,
    };

    private static Invoice NewInvoice(Guid soId, string no, decimal amount, decimal paid, decimal retentionAmount = 0) => new()
    {
        Id = Guid.NewGuid(),
        No = no,
        CustomerId = SeededCustomerId,
        SalesOrderId = soId,
        InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        Amount = amount,
        Paid = paid,
        RetentionAmount = retentionAmount,
        Status = paid >= amount ? InvoiceStatus.Paid : paid > 0 ? InvoiceStatus.PartialPaid : InvoiceStatus.Draft,
    };

    [Fact]
    public async Task Completed_rejected_when_no_active_invoice_at_all()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var so = NewSalesOrder("TST-SO-NOINV-" + Guid.NewGuid().ToString("N")[..6]);
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        var soSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        await FluentActions.Invoking(() => soSvc.UpdateStatusAsync(so.Id, "Completed"))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*belum ada Invoice aktif*");

        (await db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == so.Id)).Status
            .Should().Be(SalesOrderStatus.Delivered, "regresi P0-D lama: SO tanpa invoice tidak boleh Completed");
    }

    [Fact]
    public async Task Completed_rejected_when_invoice_exists_but_unpaid_or_partial()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var so = NewSalesOrder("TST-SO-UNPAID-" + Guid.NewGuid().ToString("N")[..6]);
        db.SalesOrders.Add(so);
        // Draft, Paid = 0 -- reproduksi persis skenario "Completed tanpa lunas" yang dilaporkan.
        db.Invoices.Add(NewInvoice(so.Id, "TST-INV-UNPAID-" + Guid.NewGuid().ToString("N")[..6], amount: 21_700_500, paid: 0));
        await db.SaveChangesAsync();

        var soSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        await FluentActions.Invoking(() => soSvc.UpdateStatusAsync(so.Id, "Completed"))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*belum lunas*");

        (await db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == so.Id)).Status
            .Should().Be(SalesOrderStatus.Delivered);
    }

    [Fact]
    public async Task Completed_allowed_when_invoice_without_retention_fully_paid()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var so = NewSalesOrder("TST-SO-PAIDNORET-" + Guid.NewGuid().ToString("N")[..6]);
        db.SalesOrders.Add(so);
        db.Invoices.Add(NewInvoice(so.Id, "TST-INV-PAIDNORET-" + Guid.NewGuid().ToString("N")[..6], amount: 5_000_000, paid: 5_000_000));
        await db.SaveChangesAsync();

        var soSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        await soSvc.UpdateStatusAsync(so.Id, "Completed");

        (await db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == so.Id)).Status
            .Should().Be(SalesOrderStatus.Completed, "RetentionAmount=0 => Paid>=Amount-RetentionAmount sama dengan Paid>=Amount, tidak boleh regresi");
    }

    [Fact]
    public async Task Completed_allowed_when_retention_outstanding_but_non_retention_portion_paid()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var so = NewSalesOrder("TST-SO-RETOK-" + Guid.NewGuid().ToString("N")[..6]);
        so.RetentionPercentage = 10;
        db.SalesOrders.Add(so);
        // Amount=100jt, RetentionAmount=10jt, Paid=90jt -- retensi BELUM dicairkan sama sekali
        // (RetentionReleasedAmount tetap 0). Kasus kunci: ini HARUS berhasil Completed.
        db.Invoices.Add(NewInvoice(so.Id, "TST-INV-RETOK-" + Guid.NewGuid().ToString("N")[..6],
            amount: 100_000_000, paid: 90_000_000, retentionAmount: 10_000_000));
        await db.SaveChangesAsync();

        var soSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        await soSvc.UpdateStatusAsync(so.Id, "Completed");

        (await db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == so.Id)).Status
            .Should().Be(SalesOrderStatus.Completed, "retensi yang belum dicairkan tidak boleh menghalangi Completed");
    }

    [Fact]
    public async Task Completed_rejected_when_retention_outstanding_and_non_retention_portion_not_yet_paid()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var so = NewSalesOrder("TST-SO-RETBAD-" + Guid.NewGuid().ToString("N")[..6]);
        so.RetentionPercentage = 10;
        db.SalesOrders.Add(so);
        // Sama seperti kasus di atas tapi Paid=80jt (< Amount-RetentionAmount=90jt) -- bagian
        // non-retensi belum lunas, harus tetap ditolak walau ada retensi.
        db.Invoices.Add(NewInvoice(so.Id, "TST-INV-RETBAD-" + Guid.NewGuid().ToString("N")[..6],
            amount: 100_000_000, paid: 80_000_000, retentionAmount: 10_000_000));
        await db.SaveChangesAsync();

        var soSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        await FluentActions.Invoking(() => soSvc.UpdateStatusAsync(so.Id, "Completed"))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*belum lunas*");

        (await db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == so.Id)).Status
            .Should().Be(SalesOrderStatus.Delivered);
    }

    [Fact]
    public async Task Completed_rejected_when_one_of_two_active_invoices_still_unpaid()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var so = NewSalesOrder("TST-SO-MULTIINV-" + Guid.NewGuid().ToString("N")[..6]);
        db.SalesOrders.Add(so);
        db.Invoices.Add(NewInvoice(so.Id, "TST-INV-MULTI-A-" + Guid.NewGuid().ToString("N")[..6], amount: 3_000_000, paid: 3_000_000));
        db.Invoices.Add(NewInvoice(so.Id, "TST-INV-MULTI-B-" + Guid.NewGuid().ToString("N")[..6], amount: 2_000_000, paid: 500_000));
        await db.SaveChangesAsync();

        var soSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        await FluentActions.Invoking(() => soSvc.UpdateStatusAsync(so.Id, "Completed"))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*belum lunas*");

        (await db.SalesOrders.AsNoTracking().FirstAsync(x => x.Id == so.Id)).Status
            .Should().Be(SalesOrderStatus.Delivered, "satu dari dua invoice aktif belum lunas -- harus tetap ditolak");
    }
}
