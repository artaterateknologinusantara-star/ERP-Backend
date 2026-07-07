using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;

namespace SynteraERP.Api.Services.Interfaces;

public interface IPurchaseRequestService
{
    Task<PaginatedResponse<PurchaseRequestListDto>> ListAsync(PaginationParams p);
    Task<PurchaseRequestDto?> GetByIdAsync(Guid id);
    Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestRequest request);
    Task<bool> UpdateStatusAsync(Guid id, string status, Guid? userId = null);
    Task<bool> DeleteAsync(Guid id);
    Task<PurchaseRequestDto?> GenerateFromSoAsync(Guid soId, Guid requestedBy);
    Task<PurchaseRequestStatsDto> GetStatsAsync();
}

public interface IPurchaseOrderService
{
    Task<PaginatedResponse<PurchaseOrderListDto>> ListAsync(PaginationParams p);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<PurchaseOrderDto?> ReceiveGoodsAsync(Guid id, ReceiveGoodsRequest request, Guid userId);
    Task<PurchaseOrderDto?> CreateFromPrAsync(Guid prId, CreatePoFromPrRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<PurchaseOrderStatsDto> GetStatsAsync();
    Task<POPaymentResponse> RecordPaymentAsync(Guid poId, RecordPOPaymentRequest request);
}
