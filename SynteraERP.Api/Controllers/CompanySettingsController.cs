using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m));
            var message = string.Join(" ", errors);
            return BadRequest(ApiResponse<CompanySettingsDto>.Fail(
                string.IsNullOrWhiteSpace(message) ? "Data tidak valid." : message));
        }

        var item = await _svc.UpdateAsync(request);
        return Ok(ApiResponse<CompanySettingsDto>.Ok(item, "Company settings berhasil diperbarui."));
    }

    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<CompanySettingsDto>>> UploadLogo(IFormFile? file)
    {
        if (file is null)
            return BadRequest(ApiResponse<CompanySettingsDto>.Fail("File logo wajib disertakan."));

        var item = await _svc.UploadLogoAsync(file);
        return Ok(ApiResponse<CompanySettingsDto>.Ok(item, "Logo berhasil diunggah."));
    }

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo()
    {
        var result = await _svc.GetLogoAsync();
        if (result is null) return NotFound(ApiResponse.Fail("Logo belum diunggah."));
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    [HttpDelete("logo")]
    public async Task<ActionResult<ApiResponse<CompanySettingsDto>>> DeleteLogo()
    {
        var item = await _svc.DeleteLogoAsync();
        return Ok(ApiResponse<CompanySettingsDto>.Ok(item, "Logo berhasil dihapus."));
    }
}
