using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.ItemMaster;

namespace SynteraERP.Api.Services.Interfaces;

public interface IItemMasterService
{
    Task<PaginatedResponse<ItemMasterDto>> ListAsync(ItemMasterParams p);
    Task<ItemMasterDto?> GetByIdAsync(Guid id);
    Task<ItemMasterStatsDto> GetStatsAsync();
    Task<ItemMasterDto> CreateAsync(CreateItemMasterRequest request);
    Task<ItemMasterDto?> UpdateAsync(Guid id, UpdateItemMasterRequest request);
    Task<bool> SetStatusAsync(Guid id, bool isActive);
    Task<bool> DeleteAsync(Guid id);
    Task<BulkApplyMarginResultDto> BulkApplyAutoMarginAsync(BulkApplyMarginRequest request);
}
