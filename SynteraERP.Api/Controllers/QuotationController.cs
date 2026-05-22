using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/quotations")]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _svc;
    private readonly QuotationPdfService _pdfSvc;

    public QuotationController(IQuotationService svc, QuotationPdfService pdfSvc)
    {
        _svc = svc;
        _pdfSvc = pdfSvc;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<QuotationListDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<QuotationListDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<QuotationDto>.Fail("Quotation tidak ditemukan."));
        return Ok(ApiResponse<QuotationDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Create([FromBody] SaveQuotationRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<QuotationDto>.Ok(item, "Quotation berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Update(Guid id, [FromBody] SaveQuotationRequest request)
    {
        var item = await _svc.UpdateAsync(id, request);
        if (item is null) return NotFound(ApiResponse<QuotationDto>.Fail("Quotation tidak ditemukan."));
        return Ok(ApiResponse<QuotationDto>.Ok(item, "Quotation berhasil diperbarui."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateQuotationStatusRequest request)
    {
        var ok = await _svc.UpdateStatusAsync(id, request.Status);
        if (!ok) return BadRequest(ApiResponse.Fail("Status tidak valid atau quotation tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Duplicate(Guid id)
    {
        var item = await _svc.DuplicateAsync(id);
        return Ok(ApiResponse<QuotationDto>.Ok(item, "Quotation berhasil diduplikasi."));
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<ApiResponse<SendQuotationResultDto>>> Send(Guid id)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<SendQuotationResultDto>.Fail("User tidak teridentifikasi."));

        var result = await _svc.SendAsync(id, userId);
        if (result is null) return NotFound(ApiResponse<SendQuotationResultDto>.Fail("Quotation tidak ditemukan."));
        return Ok(ApiResponse<SendQuotationResultDto>.Ok(result, "Penawaran berhasil dikirim."));
    }

    [HttpPost("{id:guid}/revision")]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> CreateRevision(Guid id)
    {
        var result = await _svc.CreateRevisionAsync(id);
        if (result is null) return NotFound(ApiResponse<QuotationDto>.Fail("Quotation tidak ditemukan."));
        return Ok(ApiResponse<QuotationDto>.Ok(result, "Revisi berhasil dibuat."));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse>> Approve(Guid id)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse.Fail("User tidak teridentifikasi."));

        var ok = await _svc.ApproveAsync(id, userId);
        if (!ok) return BadRequest(ApiResponse.Fail("Penawaran tidak ditemukan atau statusnya bukan Terkirim."));
        return Ok(ApiResponse.Ok("Penawaran berhasil disetujui."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse>> Reject(Guid id)
    {
        var ok = await _svc.RejectAsync(id);
        if (!ok) return BadRequest(ApiResponse.Fail("Penawaran tidak ditemukan atau statusnya bukan Terkirim."));
        return Ok(ApiResponse.Ok("Penawaran berhasil ditolak."));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var pdfBytes = await _pdfSvc.GenerateAsync(id);
        if (pdfBytes is null) return NotFound();
        return File(pdfBytes, "application/pdf", $"Quotation_{id}.pdf");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Quotation tidak ditemukan."));
        return Ok(ApiResponse.Ok("Quotation berhasil dihapus."));
    }

    [HttpPost("bulk-delete")]
    public async Task<ActionResult<ApiResponse>> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        var deleted = 0;
        foreach (var id in request.Ids)
        {
            if (await _svc.DeleteAsync(id)) deleted++;
        }
        return Ok(ApiResponse.Ok($"{deleted} penawaran berhasil dihapus."));
    }
}
