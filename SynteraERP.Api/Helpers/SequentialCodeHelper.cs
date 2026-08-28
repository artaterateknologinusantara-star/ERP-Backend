using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace SynteraERP.Api.Helpers;

// Centralizes the "count existing rows + 1, zero-pad" code generation that used to be copy-pasted
// per entity (Branch/ItemMaster/Customer/Supplier/Project) with its own CountAsync() + 1 one-liner.
// Also centralizes the race-safe retry: two concurrent creates can both read the same count before
// either commits, so the second insert can collide with the first on the Code column's unique index
// (already defined in AppDbContext for all these entities) — RunWithRetryAsync catches that specific
// DB error and lets the caller regenerate a fresh code and try again, instead of the request just
// failing with a raw 500 on the rare concurrent-create collision.
public static class SequentialCodeHelper
{
    private const int MaxAttempts = 3;

    // e.g. NextCodeAsync(db.Branches, "BR", 4) => "BR0007"
    public static async Task<string> NextCodeAsync<T>(IQueryable<T> table, string prefix, int padding)
    {
        var count = await table.CountAsync() + 1;
        return $"{prefix}{count.ToString().PadLeft(padding, '0')}";
    }

    // e.g. NextYearCodeAsync(db.Projects, "PRJ", 3, 2026) => "PRJ-2026-004"
    public static async Task<string> NextYearCodeAsync<T>(IQueryable<T> table, string prefix, int padding, int year)
    {
        var count = await table.CountAsync() + 1;
        return $"{prefix}-{year}-{count.ToString().PadLeft(padding, '0')}";
    }

    // Runs `attempt` (which generates a code, builds the entity, and calls SaveChangesAsync) up to
    // MaxAttempts times, retrying only on a unique-constraint violation so a collision on the Code
    // column self-heals with a freshly generated code instead of surfacing as a raw 500. `db` is
    // needed to clear the change tracker between attempts — the entity from the failed attempt is
    // still tracked as Added, and would otherwise get inserted again (and collide again) alongside
    // the retry's freshly-built entity.
    public static async Task<TResult> RunWithRetryAsync<TResult>(DbContext db, Func<Task<TResult>> attempt)
    {
        for (var i = 1; ; i++)
        {
            try
            {
                return await attempt();
            }
            catch (DbUpdateException ex) when (i < MaxAttempts && IsUniqueConstraintViolation(ex))
            {
                db.ChangeTracker.Clear();
            }
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
}
