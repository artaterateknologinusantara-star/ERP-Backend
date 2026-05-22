using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.ItemMaster;

namespace SynteraERP.Api.Services.Interfaces;

public interface IItemMasterService
{
    Task<PaginatedResponse<ItemMasterDto>> ListAsync(ItemMasterParams p);
    Task<ItemMasterDto?> GetByIdAsync(Guid id);
    Task<ItemMasterStatsDto> GetStatsAsync();
}
