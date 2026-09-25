using SynteraERP.Api.DTOs.CatalogLink;

namespace SynteraERP.Api.Services.Interfaces;

public interface ICatalogLinkService
{
    Task<List<UnlinkedCatalogItemDto>> GetUnlinkedItemsAsync();
    Task LinkAsync(string sourceType, Guid itemRowId, Guid itemMasterId);
}
