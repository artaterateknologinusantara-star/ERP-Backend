using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.ItemMaster;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/item-masters")]
public class ItemMasterController : ControllerBase
{
    private readonly IItemMasterService _svc;

    public ItemMasterController(IItemMasterService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ItemMasterDto>>>> List([FromQuery] ItemMasterParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<ItemMasterDto>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<ItemMasterStatsDto>>> Stats()
    {
        var stats = await _svc.GetStatsAsync();
        return Ok(ApiResponse<ItemMasterStatsDto>.Ok(stats));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ItemMasterDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<ItemMasterDto>.Fail("Item Master tidak ditemukan."));
        return Ok(ApiResponse<ItemMasterDto>.Ok(item));
    }
}
