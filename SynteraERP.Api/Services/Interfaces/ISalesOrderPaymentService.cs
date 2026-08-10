using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.DTOs.SalesOrderPayment;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISalesOrderPaymentService
{
    Task<SalesOrderPaymentDto> RecordDownPaymentAsync(Guid salesOrderId, RecordDownPaymentRequest request);
    Task<List<SalesOrderPaymentDto>> ListForSalesOrderAsync(Guid salesOrderId);
    Task<InvoiceDto?> ApplyToInvoiceAsync(Guid invoiceId, ApplyDownPaymentRequest request);
}
