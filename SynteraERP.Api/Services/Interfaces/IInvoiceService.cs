using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Invoice;

namespace SynteraERP.Api.Services.Interfaces;

public interface IInvoiceService
{
    Task<PaginatedResponse<InvoiceListDto>> ListAsync(InvoiceQueryParams p);
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request);
    Task<InvoiceDto?> MarkAsSentAsync(Guid id);
    Task<InvoiceDto?> RecordPaymentAsync(Guid id, RecordPaymentRequest request);
    Task<InvoiceDto?> ReleaseRetentionAsync(Guid id, RetentionReleaseRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<InvoiceStatsResponse> GetStatsAsync();
}
