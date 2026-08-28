using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Invoice;

namespace SynteraERP.Api.Services.Interfaces;

public interface IInvoiceService
{
    Task<PaginatedResponse<InvoiceListDto>> ListAsync(InvoiceQueryParams p);
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request);
    Task<InvoiceDto?> RecordPaymentAsync(Guid id, RecordPaymentRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<InvoiceStatsResponse> GetStatsAsync();
}
