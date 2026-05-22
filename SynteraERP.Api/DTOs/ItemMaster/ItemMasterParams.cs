using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.ItemMaster;

public class ItemMasterParams : PaginationParams
{
    public bool? IsActive { get; set; }
}
