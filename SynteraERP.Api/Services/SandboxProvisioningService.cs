using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

// Provisions and tears down isolated per-sandbox-user SQL Server databases. Each sandbox gets
// a full clone of the current schema (via EF migrations) plus the same reference-data seeders
// a fresh install gets, so a sandbox account can never see or touch real production data.
public class SandboxProvisioningService : ISandboxProvisioningService
{
    private const string NamePrefix = "SynteraERP_Sandbox_";

    private readonly IConfiguration _config;
    private readonly ILogger<SandboxProvisioningService> _logger;

    public SandboxProvisioningService(IConfiguration config, ILogger<SandboxProvisioningService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<string> ProvisionAsync(Guid roleId, CancellationToken ct = default)
    {
        var baseConnStr = _config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default belum dikonfigurasi.");

        var dbName = NamePrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();

        _logger.LogInformation("Provisioning sandbox database {Database}", dbName);
        await CreateDatabaseAsync(baseConnStr, dbName, ct);

        try
        {
            var sandboxConnStr = SandboxConnectionStringHelper.ForDatabase(baseConnStr, dbName);
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(sandboxConnStr).Options;
            await using (var sandboxDb = new AppDbContext(options))
            {
                await sandboxDb.Database.MigrateAsync(ct);

                await CustomerSeeder.SeedAsync(sandboxDb);
                await ItemMasterSeeder.SeedAsync(sandboxDb);
                await SupplierSeeder.SeedAsync(sandboxDb);
                await NumberingConfigSeeder.SeedAsync(sandboxDb);

                await CopyRolesAndPermissionsAsync(baseConnStr, sandboxDb, ct);
            }

            _logger.LogInformation("Sandbox database {Database} siap dipakai.", dbName);
            return dbName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provisioning sandbox {Database} gagal di tengah jalan — menghapus database yang sudah dibuat.", dbName);
            await DropAsync(dbName, CancellationToken.None);
            throw;
        }
    }

    public async Task DropAsync(string dbName, CancellationToken ct = default)
    {
        if (!dbName.StartsWith(NamePrefix, StringComparison.Ordinal))
        {
            _logger.LogError("Menolak drop database {Database} — bukan database sandbox (prefix tidak cocok).", dbName);
            return;
        }

        var baseConnStr = _config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default belum dikonfigurasi.");
        var masterConnStr = SandboxConnectionStringHelper.ForDatabase(baseConnStr, "master");
        var bracketed = dbName.Replace("]", "]]");

        try
        {
            await using var conn = new SqlConnection(masterConnStr);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"IF DB_ID(N'{dbName.Replace("'", "''")}') IS NOT NULL " +
                $"BEGIN ALTER DATABASE [{bracketed}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{bracketed}]; END";
            await cmd.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("Sandbox database {Database} dihapus.", dbName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal menghapus sandbox database {Database}.", dbName);
        }
    }

    private static async Task CreateDatabaseAsync(string baseConnStr, string dbName, CancellationToken ct)
    {
        var masterConnStr = SandboxConnectionStringHelper.ForDatabase(baseConnStr, "master");
        var bracketed = dbName.Replace("]", "]]");

        // T-SQL has no way to parameterize a database identifier for CREATE/DROP DATABASE — the
        // name has to be interpolated. It's server-generated (hex suffix), not user input, but
        // still bracket-quoted the way SQL Server quotes identifiers, matching the existing
        // convention in DatabaseBackupService.cs.
        await using var conn = new SqlConnection(masterConnStr);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE [{bracketed}]";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // The 3 originally-seeded roles (and their Permissions) come from migration HasData and
    // land in the sandbox automatically via MigrateAsync above with identical GUIDs. Roles
    // created later through the UI (e.g. "Guest") only exist in production — copy every
    // Role/Permission row not already present so ModulePermissionHandler works for any role,
    // not just the built-in ones.
    private static async Task CopyRolesAndPermissionsAsync(string baseConnStr, AppDbContext sandboxDb, CancellationToken ct)
    {
        var prodOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(baseConnStr).Options;
        await using var prodDb = new AppDbContext(prodOptions);

        var prodRoles = await prodDb.Roles.AsNoTracking().ToListAsync(ct);
        var existingRoleIds = await sandboxDb.Roles.Select(r => r.Id).ToListAsync(ct);
        var missingRoles = prodRoles.Where(r => !existingRoleIds.Contains(r.Id)).ToList();
        if (missingRoles.Count > 0)
        {
            sandboxDb.Roles.AddRange(missingRoles.Select(r => new Role
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                CreatedBy = r.CreatedBy,
                UpdatedBy = r.UpdatedBy,
                IsDeleted = r.IsDeleted,
            }));
            await sandboxDb.SaveChangesAsync(ct);
        }

        var prodPermissions = await prodDb.Permissions.AsNoTracking().ToListAsync(ct);
        var existingPermissionIds = await sandboxDb.Permissions.Select(p => p.Id).ToListAsync(ct);
        var missingPermissions = prodPermissions.Where(p => !existingPermissionIds.Contains(p.Id)).ToList();
        if (missingPermissions.Count > 0)
        {
            sandboxDb.Permissions.AddRange(missingPermissions.Select(p => new Permission
            {
                Id = p.Id,
                RoleId = p.RoleId,
                Module = p.Module,
                CanView = p.CanView,
                CanCreate = p.CanCreate,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete,
                CanApprove = p.CanApprove,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedBy = p.UpdatedBy,
                IsDeleted = p.IsDeleted,
            }));
            await sandboxDb.SaveChangesAsync(ct);
        }
    }
}
