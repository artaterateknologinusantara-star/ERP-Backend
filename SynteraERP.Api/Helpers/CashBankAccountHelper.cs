using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;

namespace SynteraERP.Api.Helpers;

// Backend duplicate-logic audit (26 Sep 2026): SalesOrderPaymentService, InvoiceService, and
// PurchaseOrderService each had a byte-identical private ResolveCashBankAccountAsync (all three
// carrying the comment "Pola sama persis dengan ExpenseService.CreateAsync"), plus a 4th inline
// variant in ExpenseService.CreateAsync that: (a) didn't fetch Code upfront (ExpenseService got it
// later via the CashBankAccount navigation property instead), and (b) used a slightly different
// "not found" message ("Akun Kas/Bank tidak ditemukan." vs "...yang dipilih tidak ditemukan.").
// Extracted here so all 4 share one implementation and one error message; ExpenseService now
// fetches Code upfront like the other 3 instead of relying on the navigation property.
public static class CashBankAccountHelper
{
    public static async Task<(Guid Id, string Code)> ResolveAsync(AppDbContext db, Guid? cashBankAccountId)
    {
        if (cashBankAccountId.HasValue)
        {
            var account = await db.Accounts
                .Where(x => x.Id == cashBankAccountId.Value && !x.IsDeleted)
                .Select(x => new { x.Id, x.Code })
                .FirstOrDefaultAsync();

            if (account is null)
                throw new InvalidOperationException("Akun Kas/Bank yang dipilih tidak ditemukan.");

            return (account.Id, account.Code);
        }

        var defaultAccount = await db.Accounts
            .Where(x => x.Code == "1-1001" && !x.IsDeleted)
            .Select(x => new { x.Id, x.Code })
            .FirstOrDefaultAsync();

        if (defaultAccount is null)
            throw new InvalidOperationException("Akun default Kas (1-1001) tidak ditemukan di Chart of Accounts.");

        return (defaultAccount.Id, defaultAccount.Code);
    }
}
