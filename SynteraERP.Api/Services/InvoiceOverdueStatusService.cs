using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Services;

// Sebelumnya invoice yang lewat jatuh tempo ditandai Overdue di dalam InvoiceService.ListAsync,
// jadi setiap kali endpoint GET /api/invoices dibuka, sistem ikut menulis ke DB. Logika itu
// dipindahkan ke sini supaya jalan berkala di background, bukan menumpang pada request baca.
public class InvoiceOverdueStatusService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceOverdueStatusService> _logger;

    public InvoiceOverdueStatusService(IServiceScopeFactory scopeFactory, ILogger<InvoiceOverdueStatusService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await UpdateOverdueInvoicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal memperbarui status Overdue invoice.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task UpdateOverdueInvoicesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        var updated = await db.Invoices
            .Where(x => !x.IsDeleted
                && x.Status != InvoiceStatus.Paid
                && x.Status != InvoiceStatus.Overdue
                && x.DueDate < today)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, InvoiceStatus.Overdue)
                .SetProperty(x => x.UpdatedAt, now), ct);

        if (updated > 0)
            _logger.LogInformation("{Count} invoice ditandai Overdue.", updated);
    }
}
