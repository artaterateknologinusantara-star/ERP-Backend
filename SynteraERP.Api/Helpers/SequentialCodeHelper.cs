using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace SynteraERP.Api.Helpers;

// Centralizes sequential code generation for Branch/ItemMaster/Customer/Supplier/Project.
//
// Task #25 (26 Sep 2026): this used to be `COUNT(WHERE IsDeleted=0) + 1`, which produces a
// PERMANENT gap the moment any row anywhere in the sequence gets soft-deleted — the count drops
// but the codes already issued don't, so the next "count+1" code collides with a still-ACTIVE
// row further down the sequence (not just the deleted row's own code, which a separate filtered
// unique index on Code — WHERE IsDeleted=0 — already allows reusing safely). Confirmed via real
// incidents: PRJ-2026-029 (2 Sep 2026, production) and twice more during task #41/#45 test runs
// (PRJ-2026-030, PRJ-2026-031) — and, per task #25's own Step-0 investigation, CUST0018 and
// SUPP0010 were ALREADY collided against active rows in the dev DB before this fix.
//
// Fixed by switching to MAX(numeric suffix ever issued) + 1, read via IgnoreQueryFilters() so
// soft-deleted rows' codes still count toward the max (only the DELETED row's own code is safe to
// reuse — everything after it in the sequence is not). This closes the "any single deletion
// causes a permanent, deterministic collision" failure mode. It does NOT remove the underlying
// TOCTOU race (two concurrent creates can still read the same MAX before either commits) — that's
// what RunWithRetryAsync below is still for, and still required at every call site.
//
// Existing data in both dev and scratch DB has legacy/seed codes that don't match the generator's
// own format (dash-separated seed literals like "CUST-001"/"ITM-001", ad-hoc test fixtures like
// "SW-41bfe0"/"TST-PRJ-...") — ParseMaxSuffix below only matches the EXACT current generator
// pattern per prefix and silently ignores anything else, so unrelated/legacy-format codes can
// never inflate (or corrupt) the max.
public static class SequentialCodeHelper
{
    private const int MaxAttempts = 3;

    // e.g. NextCodeAsync(db.Branches, x => x.Code, "BR", 4) => "BR0007"
    public static async Task<string> NextCodeAsync<T>(
        IQueryable<T> table, Expression<Func<T, string>> codeSelector, string prefix, int padding)
        where T : class
    {
        var codes = await table.IgnoreQueryFilters().Select(codeSelector).ToListAsync();
        var pattern = new Regex($"^{Regex.Escape(prefix)}(\\d+)$");
        var next = ParseMaxSuffix(codes, pattern) + 1;
        return $"{prefix}{next.ToString().PadLeft(padding, '0')}";
    }

    // e.g. NextYearCodeAsync(db.Projects, x => x.Code, "PRJ", 3, 2026) => "PRJ-2026-004". Numbering
    // is per-year (matches the method's own name/signature) — a prior year's codes never count
    // toward this year's max, so each year restarts at 1.
    public static async Task<string> NextYearCodeAsync<T>(
        IQueryable<T> table, Expression<Func<T, string>> codeSelector, string prefix, int padding, int year)
        where T : class
    {
        var codes = await table.IgnoreQueryFilters().Select(codeSelector).ToListAsync();
        var pattern = new Regex($"^{Regex.Escape(prefix)}-{year}-(\\d+)$");
        var next = ParseMaxSuffix(codes, pattern) + 1;
        return $"{prefix}-{year}-{next.ToString().PadLeft(padding, '0')}";
    }

    private static int ParseMaxSuffix(List<string> codes, Regex pattern)
    {
        var max = 0;
        foreach (var code in codes)
        {
            var match = pattern.Match(code ?? string.Empty);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var n) && n > max)
                max = n;
        }
        return max;
    }

    // Runs `attempt` (which generates a code, builds the entity, and calls SaveChangesAsync) up to
    // MaxAttempts times, retrying only on a unique-constraint violation so a collision on the Code
    // column self-heals with a freshly generated code instead of surfacing as a raw 500. Still
    // needed after the MAX()+1 fix above — that closes the deterministic post-deletion gap, not
    // the TOCTOU race between two concurrent creates reading the same MAX before either commits.
    // `db` is needed to clear the change tracker between attempts — the entity from the failed
    // attempt is still tracked as Added, and would otherwise get inserted again (and collide
    // again) alongside the retry's freshly-built entity.
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
