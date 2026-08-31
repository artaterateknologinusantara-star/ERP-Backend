using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;

namespace SynteraERP.Api.Services;

// Runs inside the API process so a monthly full backup happens on whichever server hosts the
// app + DB (dev/demo/prod) without needing OS-level cron or SQL Server Agent access on that
// server. Checks daily but only actually fires BACKUP DATABASE once per calendar month —
// "already done this month" is derived from msdb.dbo.backupset (SQL Server's own backup
// history), not local scheduler state, so it stays correct across app restarts/redeploys.
// Disabled by default: does nothing unless DatabaseBackup:Directory is configured, since that
// path must exist on the SQL Server host's own filesystem (BACKUP DATABASE runs server-side).
public class DatabaseBackupService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<DatabaseBackupService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await RunMonthlyBackupIfDueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menjalankan backup database bulanan.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunMonthlyBackupIfDueAsync(CancellationToken ct)
    {
        var directory = _config["DatabaseBackup:Directory"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            _logger.LogDebug("DatabaseBackup:Directory belum dikonfigurasi — backup bulanan otomatis nonaktif.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var databaseName = db.Database.GetDbConnection().Database;

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (await BackupAlreadyDoneThisMonthAsync(db, databaseName, monthStart, ct))
            return;

        var separator = directory.Contains('\\') ? '\\' : '/';
        var fileName = $"{databaseName}_{DateTime.UtcNow:yyyyMM}.bak";
        var fullPath = $"{directory.TrimEnd('/', '\\')}{separator}{fileName}";

        _logger.LogInformation("Menjalankan backup bulanan database {Database} ke {Path}", databaseName, fullPath);

        // T-SQL has no way to parameterize an object identifier (BACKUP DATABASE @name is not
        // valid syntax), so the database name has to be interpolated into the statement text.
        // databaseName comes from this app's own DbConnection, not external/user input; bracket
        // quoting is still applied ("]" doubled) the same way SQL Server quotes identifiers.
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"BACKUP DATABASE [{databaseName.Replace("]", "]]")}] TO DISK = @path WITH COMPRESSION, INIT, STATS = 10",
            new SqlParameter("@path", fullPath), ct);
#pragma warning restore EF1002

        _logger.LogInformation("Backup bulanan database {Database} selesai: {Path}", databaseName, fullPath);

        var retentionMonths = _config.GetValue<int?>("DatabaseBackup:RetentionMonths") ?? 12;
        await CleanupOldBackupsAsync(db, directory, retentionMonths, ct);
    }

    private static async Task<bool> BackupAlreadyDoneThisMonthAsync(AppDbContext db, string databaseName, DateTime monthStart, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM msdb.dbo.backupset WHERE database_name = @db AND type = 'D' AND backup_start_date >= @since";
            cmd.Parameters.Add(new SqlParameter("@db", databaseName));
            cmd.Parameters.Add(new SqlParameter("@since", monthStart));
            var count = (int)(await cmd.ExecuteScalarAsync(ct))!;
            return count > 0;
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }

    // Same mechanism SQL Server's own Maintenance Plan "Cleanup Task" uses — an extended
    // stored procedure that exists by default, so it works without enabling xp_cmdshell.
    private async Task CleanupOldBackupsAsync(AppDbContext db, string directory, int retentionMonths, CancellationToken ct)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddMonths(-retentionMonths);
            await db.Database.ExecuteSqlRawAsync(
                "EXEC master.dbo.xp_delete_file 0, @directory, N'bak', @cutoff, 0",
                new SqlParameter("@directory", directory), new SqlParameter("@cutoff", cutoff), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backup bulanan berhasil, tapi cleanup backup lama gagal (folder {Directory}).", directory);
        }
    }
}
