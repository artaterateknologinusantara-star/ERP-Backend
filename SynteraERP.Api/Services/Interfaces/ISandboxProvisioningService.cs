namespace SynteraERP.Api.Services.Interfaces;

public interface ISandboxProvisioningService
{
    // Creates a new, fully isolated SQL Server database, migrates it to the current schema,
    // seeds it with the same reference data as a fresh install, and copies Role/Permission
    // rows from production so permission checks work for the given role inside the sandbox.
    // Returns the generated database name.
    Task<string> ProvisionAsync(Guid roleId, CancellationToken ct = default);

    // Drops a sandbox database previously created by ProvisionAsync. Refuses to touch anything
    // not carrying the sandbox naming prefix. Idempotent — logs and returns if already gone.
    Task DropAsync(string dbName, CancellationToken ct = default);
}
