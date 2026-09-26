using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;

namespace SynteraERP.Api.Helpers;

// Centralizes the self-healing NumberingConfig-based document-number generator used by all 9
// DocTypes (QUOTATION, SALES_ORDER, INVOICE, PURCHASE_ORDER, PURCHASE_REQUEST, SUPPLIER_INVOICE,
// EXPENSE, DELIVERY_ORDER, JOURNAL_ENTRY).
//
// Backend duplicate-logic audit (26 Sep 2026): every one of the 9 DocTypes had its own private
// `NextNumberAsync`-style method with an identical 4-line body (fetch NumberingConfig by DocType,
// call GenerateNext(), SaveChangesAsync(), return) — except QuotationService, which additionally
// re-derives the actual highest number issued this year directly from the Quotations table
// (IgnoreQueryFilters() — a soft-deleted row's number still counts as "used") and bumps
// LastNumber up to match if the stored counter has drifted behind, BEFORE calling GenerateNext().
// This resync logic has existed since the project's initial commit (`git log -S`, commit
// a62977c) — it is NOT the fix that closed the 2026-07-08 NumberingConfig HasData incident (see
// 00_PROJECT_STATUS.md's "[DITUTUP PERMANEN 2026-07-08]" entry; that fix was
// Data/NumberingConfigSeeder.cs, applied uniformly to all 9 DocTypes already). It's a separate,
// broader defensive layer unique to Quotation that the other 8 never had. Extracted here so all
// 9 share one implementation instead of 9 (1 with the resync guard, 8 without).
//
// Deliberately mirrors SequentialCodeHelper's style — pull every issued number for the DocType via
// IgnoreQueryFilters() and parse in C#, rather than composing a dynamic
// `Where(x => selector(x).StartsWith(...))` expression tree — consistent with the one existing
// precedent for this class of problem in this codebase. Trade-off: this pulls every number ever
// issued for that DocType into memory on every call (the original Quotation-only version filtered
// the prefix server-side via `.Where(...).Select(...)`); negligible at this project's
// single-tenant-per-installation row-count scale, but worth revisiting if that stops being true.
public static class NumberingResyncHelper
{
    // e.g. NextNumberAsync(db, db.Quotations, q => q.No, "QUOTATION") => "Q.SYN-26.0150"
    public static async Task<string> NextNumberAsync<T>(
        AppDbContext db, IQueryable<T> table, Expression<Func<T, string>> numberSelector, string docType)
        where T : class
    {
        var config = await db.NumberingConfigs
            .FirstOrDefaultAsync(n => n.DocType == docType)
            ?? throw new InvalidOperationException($"NumberingConfig for {docType} not found");

        var year = DateTime.UtcNow.ToString("yy");
        var yearPrefix = $"{config.Prefix}-{year}.";

        var allNos = await table.IgnoreQueryFilters().Select(numberSelector).ToListAsync();
        var existingNos = allNos.Where(no => no != null && no.StartsWith(yearPrefix)).ToList();

        if (existingNos.Count > 0)
        {
            var actualMax = existingNos.Select(no =>
            {
                var suffix = no.Length > yearPrefix.Length ? no[yearPrefix.Length..] : "0";
                return int.TryParse(suffix, out var n) ? n : 0;
            }).Max();

            if (actualMax >= config.LastNumber)
                config.LastNumber = actualMax;
        }

        var docNo = config.GenerateNext();
        await db.SaveChangesAsync();
        return docNo;
    }
}
