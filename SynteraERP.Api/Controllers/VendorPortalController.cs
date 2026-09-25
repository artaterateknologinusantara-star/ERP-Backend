using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

// Vendor-facing — HANYA scheme "Vendor" + policy "VendorOnly" (lihat Program.cs). SupplierId dan
// portalUserId SELALU dibaca dari claim token, tidak pernah dari input caller — supaya vendor A
// tidak bisa menyamar sebagai vendor B lewat parameter request.
[Authorize(AuthenticationSchemes = "Vendor", Policy = "VendorOnly")]
[ApiController]
[Route("api/vendor/rab-requests")]
public class VendorPortalController : ControllerBase
{
    private readonly IVendorRabRequestService _requestSvc;
    private readonly IVendorRabSubmissionService _submissionSvc;

    public VendorPortalController(IVendorRabRequestService requestSvc, IVendorRabSubmissionService submissionSvc)
    {
        _requestSvc = requestSvc;
        _submissionSvc = submissionSvc;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<VendorRabRequestDto>>>> List()
    {
        var supplierId = GetSupplierId();
        var result = await _requestSvc.ListForVendorAsync(supplierId);
        return Ok(ApiResponse<List<VendorRabRequestDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VendorRabRequestDto>>> GetById(Guid id)
    {
        var supplierId = GetSupplierId();
        var result = await _requestSvc.GetForVendorAsync(id, supplierId);
        if (result is null) return NotFound(ApiResponse<VendorRabRequestDto>.Fail("Permintaan RAB tidak ditemukan."));
        return Ok(ApiResponse<VendorRabRequestDto>.Ok(result));
    }

    [HttpPost("{id:guid}/submissions")]
    public async Task<ActionResult<ApiResponse<VendorRabSubmissionDto>>> Submit(
        Guid id, [FromBody] CreateVendorRabSubmissionRequest request)
    {
        var supplierId = GetSupplierId();
        var portalUserId = GetPortalUserId();

        try
        {
            var result = await _submissionSvc.CreateAsync(id, supplierId, portalUserId, request);
            return Ok(ApiResponse<VendorRabSubmissionDto>.Ok(result, "Submission berhasil dikirim, menunggu review."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VendorRabSubmissionDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorRabSubmissionDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:guid}/template.xlsx")]
    public async Task<IActionResult> DownloadTemplate(Guid id)
    {
        var supplierId = GetSupplierId();
        var request = await _requestSvc.GetForVendorAsync(id, supplierId);
        if (request is null) return NotFound(ApiResponse.Fail("Permintaan RAB tidak ditemukan."));

        var bytes = VendorRabExcelService.GenerateTemplate(request);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"RAB-{request.Name}-{request.Id}.xlsx");
    }

    // Sama seperti prinsip reject-all di BankReconciliation import: baris parsing yang gagal
    // (ID/harga rusak) ATAU gagal validasi semantik (ID tidak dikenal/hilang/duplikat, dicek di
    // IVendorRabSubmissionService.CreateAsync) membatalkan SELURUH file — tidak ada partial-import.
    [HttpPost("{id:guid}/submissions/import")]
    public async Task<ActionResult<ApiResponse<VendorRabSubmissionDto>>> ImportSubmission(Guid id, IFormFile file)
    {
        var supplierId = GetSupplierId();
        var portalUserId = GetPortalUserId();

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<VendorRabSubmissionDto>.Fail("File Excel tidak boleh kosong."));

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<VendorRabSubmissionDto>.Fail("File harus berformat .xlsx."));

        await using var stream = file.OpenReadStream();
        var (parsed, parseErrors) = VendorRabExcelService.ParseSubmission(stream);
        if (parsed is null)
        {
            return BadRequest(new ApiResponse<List<string>>
            {
                Success = false,
                Message = "File ditolak — perbaiki baris berikut lalu upload ulang.",
                Data = parseErrors,
            });
        }

        try
        {
            var result = await _submissionSvc.CreateAsync(id, supplierId, portalUserId, parsed);
            return Ok(ApiResponse<VendorRabSubmissionDto>.Ok(result, "Submission berhasil dikirim, menunggu review."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VendorRabSubmissionDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorRabSubmissionDto>.Fail(ex.Message));
        }
    }

    private Guid GetSupplierId()
    {
        var claim = User.FindFirstValue("supplierId")
            ?? throw new InvalidOperationException("Token vendor tidak punya claim supplierId.");
        return Guid.Parse(claim);
    }

    private Guid GetPortalUserId()
    {
        // JwtBearerHandler secara default me-remap claim pendek seperti "sub" ke URI legacy
        // (ClaimTypes.NameIdentifier) lewat JwtSecurityTokenHandler.DefaultInboundClaimTypeMap —
        // makanya perlu fallback, persis pola yang sudah dipakai di AuthController.Me()/
        // QuotationController.Approve() untuk User internal.
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token vendor tidak punya claim sub.");
        return Guid.Parse(claim);
    }
}
