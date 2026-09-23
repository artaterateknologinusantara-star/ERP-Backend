using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.CatalogLink;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

/// <summary>Layar admin "Item Belum Terhubung" — lihat CatalogLinkService untuk konteksnya.</summary>
[Authorize]
[ApiController]
[Route("api/catalog-links")]
public class CatalogLinkController : ControllerBase
{
    private readonly ICatalogLinkService _svc;

    public CatalogLinkController(ICatalogLinkService svc) => _svc = svc;

    [HttpGet("unlinked")]
    public async Task<IActionResult> GetUnlinked()
    {
        var result = await _svc.GetUnlinkedItemsAsync();
        return Ok(new { success = true, data = result });
    }

    [HttpPost("{sourceType}/{itemRowId:guid}/link")]
    public async Task<IActionResult> Link(string sourceType, Guid itemRowId, [FromBody] LinkCatalogItemRequest request)
    {
        try
        {
            await _svc.LinkAsync(sourceType, itemRowId, request.ItemMasterId);
            return Ok(new { success = true, message = "Item berhasil ditautkan." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
