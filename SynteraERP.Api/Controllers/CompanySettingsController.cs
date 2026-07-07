using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.CompanySettings;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/company-settings")]
public class CompanySettingsController : ControllerBase
{
    private readonly ICompanySettingsService _svc;

    public CompanySettingsController(ICompanySettingsService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CompanySettingsDto>>> Get()
    {
        var item = await _svc.GetAsync();
        return Ok(ApiResponse<CompanySettingsDto>.Ok(item));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<CompanySettingsDto>>> Update([FromBody] UpdateCompanySettingsRequest request)
    {
        var item = await _svc.UpdateAsync(request);
        return Ok(ApiResponse<CompanySettingsDto>.Ok(item, "Company settings berhasil diperbarui."));
    }
}
