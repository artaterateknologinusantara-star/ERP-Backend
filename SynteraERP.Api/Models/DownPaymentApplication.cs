namespace SynteraERP.Api.Models;

// Bridge table, mirip pola SupplierInvoicePayment (Fase 4) — tapi PUNYA AmountApplied sendiri,
// beda dari SupplierInvoicePayment yang selalu full-amount 1:1. Satu SalesOrderPayment (DP) bisa
// diterapkan sebagian ke satu Invoice dan sisanya nanti/ke Invoice lain, jadi Amount tidak bisa
// diambil dari SalesOrderPayment yang terhubung saja seperti SupplierInvoicePayment mengambil dari POPayment.
public class DownPaymentApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SalesOrderPaymentId { get; set; }
    public SalesOrderPayment SalesOrderPayment { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal AmountApplied { get; set; }
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
}
