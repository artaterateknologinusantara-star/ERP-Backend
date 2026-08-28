using SynteraERP.Api.Models;

namespace SynteraERP.Api.Services;

public static class ItemPricingCalculator
{
    public static decimal? ComputeAutoSellingPrice(ItemMaster item) => Compute(item, item.MarginDefault);

    public static decimal? ComputeFloorPrice(ItemMaster item) => Compute(item, item.MarginMinimum);

    private static decimal? Compute(ItemMaster item, decimal? margin)
    {
        if (item.PurchasePrice is not { } cost || item.MarginType is not { } type || margin is not { } m)
            return null;

        return type == MarginType.Percent ? cost * (1 + m / 100) : cost + m;
    }
}
