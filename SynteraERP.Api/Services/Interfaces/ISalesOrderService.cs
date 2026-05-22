using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.SalesOrder;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISalesOrderService
{
    Task<PaginatedResponse<SalesOrderListDto>> ListAsync(PaginationParams p);
    Task<SalesOrderDto?> GetByIdAsync(Guid id);
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<bool> DeleteAsync(Guid id);
}
