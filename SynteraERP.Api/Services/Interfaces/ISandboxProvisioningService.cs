namespace SynteraERP.Api.Services.Interfaces;

public interface ISandboxProvisioningService
{
    // Creates a new, fully isolated SQL Server database, migrates it to the current schema,
    // seeds it with the same reference data as a fresh install, and copies Role/Permission
    // rows from production so permission checks work for the given role inside the sandbox.
    // desiredName is sanitized into a valid SQL identifier and de-duplicated against existing
    // sandbox databases (suffixing _2, _3, ... on collision). Returns the actual database name.
    Task<string> ProvisionAsync(Guid roleId, string desiredName, CancellationToken ct = default);

    // Drops a sandbox database previously created by ProvisionAsync. Refuses to touch anything
    // not carrying the sandbox naming prefix. Idempotent — logs and returns if already gone.
    Task DropAsync(string dbName, CancellationToken ct = default);
}
