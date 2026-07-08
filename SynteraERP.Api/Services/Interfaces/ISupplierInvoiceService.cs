using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.DTOs.SupplierInvoice;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISupplierInvoiceService
{
    Task<PaginatedResponse<SupplierInvoiceListDto>> ListAsync(SupplierInvoiceQueryParams p);
    Task<SupplierInvoiceDto?> GetByIdAsync(Guid id);
    Task<SupplierInvoiceDto> CreateAsync(CreateSupplierInvoiceRequest request);
    Task<SupplierInvoiceDto?> ApproveAsync(Guid id, Guid? userId);
    Task<SupplierInvoiceDto?> RecordPaymentAsync(Guid id, RecordPOPaymentRequest request);
}
