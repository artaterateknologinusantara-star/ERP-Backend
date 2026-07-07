using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.SalesOrder;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISalesOrderService
{
    Task<PaginatedResponse<SalesOrderListResponse>> GetListAsync(int page, int perPage, string? search, string? status);
    Task<SalesOrderDetailResponse?> GetByIdAsync(Guid id);
    Task<SalesOrderDetailResponse> CreateAsync(CreateSalesOrderRequest request);
    Task UpdateStatusAsync(Guid id, string status);
    Task DeleteAsync(Guid id);
    Task<SalesOrderStatsResponse> GetStatsAsync();
    Task<SalesOrderDetailResponse> CreateFromQuotationAsync(Guid quotationId, Guid userId);
}
