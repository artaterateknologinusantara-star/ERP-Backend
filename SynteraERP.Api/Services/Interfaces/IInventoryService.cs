using SynteraERP.Api.DTOs.Inventory;

namespace SynteraERP.Api.Services.Interfaces;

public interface IInventoryService
{
    Task<List<StockTransactionDto>> GetStockHistoryAsync(Guid? itemMasterId, int page, int perPage);
    Task<StockTransactionDto> RecordStockInAsync(RecordStockInRequest request, Guid userId);

    Task<List<DeliveryOrderListDto>> GetDeliveryOrdersAsync(int page, int perPage, string? search, string? status);
    Task<DeliveryOrderDetailDto?> GetDeliveryOrderByIdAsync(Guid id);
    Task<DeliveryOrderDetailDto> CreateDeliveryOrderAsync(CreateDeliveryOrderRequest request, Guid userId);
    Task<DeliveryOrderDetailDto> ConfirmDeliveryOrderAsync(Guid id, Guid userId);
    Task<DeliveryOrderDetailDto> MarkDeliveredAsync(Guid id);
    Task DeleteDeliveryOrderAsync(Guid id);

    Task<DeliveryOrderDetailDto> CreateDOFromSOAsync(Guid soId, Guid userId);

    Task<InventoryStatsDto> GetStatsAsync();
    Task<List<LowStockItemDto>> GetLowStockItemsAsync();
}
