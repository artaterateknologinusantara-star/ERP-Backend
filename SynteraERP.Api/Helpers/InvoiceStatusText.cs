using SynteraERP.Api.Models;

namespace SynteraERP.Api.Helpers;

// Single source of truth for "PartialPaid" -> "Partial Paid" display text -- was duplicated
// (and, in InvoicePdfService's badge, missed entirely) across ToListDto/ToDto/PDF badge.
public static class InvoiceStatusText
{
    public static string Format(InvoiceStatus status) =>
        status == InvoiceStatus.PartialPaid ? "Partial Paid" : status.ToString();
}
