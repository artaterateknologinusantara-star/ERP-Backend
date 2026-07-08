namespace SynteraERP.Api.Models;

// Tabel penghubung murni: menandai POPayment mana yang dianggap melunasi SupplierInvoice mana.
// Amount/PaymentDate diambil dari POPayment yang terhubung, tidak disimpan ulang di sini.
public class SupplierInvoicePayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierInvoiceId { get; set; }
    public Guid POPaymentId { get; set; }

    public SupplierInvoice SupplierInvoice { get; set; } = null!;
    public POPayment POPayment { get; set; } = null!;
}
