using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.TaxRate;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tax-rates")]
public class TaxRateController : ControllerBase
{
    private readonly ITaxRateService _svc;

    public TaxRateController(ITaxRateService svc) => _svc = svc;

    [HttpGet]
    [RequirePermission(Modules.Settings, PermissionActions.View)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<TaxRateDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<TaxRateDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Modules.Settings, PermissionActions.View)]
    public async Task<ActionResult<ApiResponse<TaxRateDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<TaxRateDto>.Fail("Tax rate tidak ditemukan."));
        return Ok(ApiResponse<TaxRateDto>.Ok(item));
    }

    [HttpPost]
    [RequirePermission(Modules.Settings, PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<TaxRateDto>>> Create([FromBody] CreateTaxRateRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<TaxRateDto>.Ok(item, "Tax rate berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Modules.Settings, PermissionActions.Edit)]
    public async Task<ActionResult<ApiResponse<TaxRateDto>>> Update(Guid id, [FromBody] UpdateTaxRateRequest request)
    {
        var item = await _svc.UpdateAsync(id, request);
        if (item is null) return NotFound(ApiResponse<TaxRateDto>.Fail("Tax rate tidak ditemukan."));
        return Ok(ApiResponse<TaxRateDto>.Ok(item, "Tax rate berhasil diperbarui."));
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission(Modules.Settings, PermissionActions.Edit)]
    public async Task<ActionResult<ApiResponse>> SetStatus(Guid id, [FromBody] SetStatusRequest request)
    {
        var ok = await _svc.SetStatusAsync(id, request.IsActive);
        if (!ok) return NotFound(ApiResponse.Fail("Tax rate tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }
}
