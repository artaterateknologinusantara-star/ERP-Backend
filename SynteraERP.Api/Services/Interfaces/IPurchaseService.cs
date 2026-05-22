using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;

namespace SynteraERP.Api.Services.Interfaces;

public interface IPurchaseRequestService
{
    Task<PaginatedResponse<PurchaseRequestListDto>> ListAsync(PaginationParams p);
    Task<PurchaseRequestDto?> GetByIdAsync(Guid id);
    Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestRequest request);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<bool> DeleteAsync(Guid id);
}

public interface IPurchaseOrderService
{
    Task<PaginatedResponse<PurchaseOrderListDto>> ListAsync(PaginationParams p);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<PurchaseOrderDto?> ReceiveGoodsAsync(Guid id, ReceiveGoodsRequest request);
    Task<bool> DeleteAsync(Guid id);
}
