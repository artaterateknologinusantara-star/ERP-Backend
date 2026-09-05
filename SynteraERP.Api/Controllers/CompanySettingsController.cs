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

    // Anonymous: the logo needs to render on the pre-login screen (and browser tabs generally),
    // where no JWT exists yet. A company logo image isn't sensitive — nothing else here is exposed.
    [AllowAnonymous]
    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo()
    {
        var result = await _svc.GetLogoAsync();
        if (result is null) return NotFound(ApiResponse.Fail("Logo belum diunggah."));
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    // Anonymous, deliberately minimal: only CompanyName + whether a logo exists — everything else on
    // CompanySettings (email, NPWP, bank details, etc.) stays behind [Authorize]. Used by the login
    // page and the browser tab title, both of which render before any JWT is available.
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<ApiResponse<PublicCompanySettingsDto>>> GetPublic()
    {
        var item = await _svc.GetPublicAsync();
        return Ok(ApiResponse<PublicCompanySettingsDto>.Ok(item));
    }

    [HttpDelete("logo")]
    public async Task<ActionResult<ApiResponse<CompanySettingsDto>>> DeleteLogo()
    {
        var item = await _svc.DeleteLogoAsync();
        return Ok(ApiResponse<CompanySettingsDto>.Ok(item, "Logo berhasil dihapus."));
    }

    [HttpPost("regenerate-prefixes")]
    public async Task<ActionResult<ApiResponse<RegeneratePrefixesResponse>>> RegeneratePrefixes()
    {
        var result = await _svc.RegeneratePrefixesAsync();
        return Ok(ApiResponse<RegeneratePrefixesResponse>.Ok(result,
            $"Prefix diperbarui untuk {result.UpdatedCount} tipe dokumen. Nomor dokumen yang sudah ada tidak berubah."));
    }

    [HttpGet("numbering-configs")]
    public async Task<ActionResult<ApiResponse<List<NumberingConfigDto>>>> GetNumberingConfigs()
    {
        var result = await _svc.GetNumberingConfigsAsync();
        return Ok(ApiResponse<List<NumberingConfigDto>>.Ok(result));
    }
}
