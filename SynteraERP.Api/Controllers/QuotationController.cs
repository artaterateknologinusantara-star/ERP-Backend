using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Models;
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
    private readonly IAuthorizationService _authz;

    public QuotationController(IQuotationService svc, QuotationPdfService pdfSvc, IAuthorizationService authz)
    {
        _svc = svc;
        _pdfSvc = pdfSvc;
        _authz = authz;
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
        // Disetujui/Ditolak are also reachable via the dedicated /approve and /reject endpoints
        // below (which carry the [RequirePermission] gate) — guard them here too so this generic
        // status setter can't be used to bypass that gate.
        if (string.Equals(request.Status, nameof(QuotationStatus.Disetujui), StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.Status, nameof(QuotationStatus.Ditolak), StringComparison.OrdinalIgnoreCase))
        {
            var authResult = await _authz.AuthorizeAsync(User, new ModulePermissionRequirement(Modules.Sales, PermissionActions.Approve).PolicyName);
            if (!authResult.Succeeded) return Forbid();
        }

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

    [RequirePermission(Modules.Sales, PermissionActions.Approve)]
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

    [RequirePermission(Modules.Sales, PermissionActions.Approve)]
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

    // ── Item Pekerjaan / Detail Kerja (RAB/BQ) ──────────────────────────────────

    [HttpPost("groups/{groupId:guid}/work-items")]
    public async Task<ActionResult<ApiResponse<QuotationWorkItemDto>>> CreateWorkItem(Guid groupId, [FromBody] SaveWorkItemRequest request)
    {
        try
        {
            var result = await _svc.CreateWorkItemAsync(groupId, request);
            return Ok(ApiResponse<QuotationWorkItemDto>.Ok(result, "Item Pekerjaan berhasil ditambahkan."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPut("work-items/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateWorkItem(Guid id, [FromBody] SaveWorkItemRequest request)
    {
        var ok = await _svc.UpdateWorkItemAsync(id, request);
        if (!ok) return NotFound(ApiResponse.Fail("Item Pekerjaan tidak ditemukan."));
        return Ok(ApiResponse.Ok("Item Pekerjaan berhasil diperbarui."));
    }

    [HttpDelete("work-items/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkItem(Guid id)
    {
        var ok = await _svc.DeleteWorkItemAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Item Pekerjaan tidak ditemukan."));
        return Ok(ApiResponse.Ok("Item Pekerjaan berhasil dihapus."));
    }

    [HttpPost("work-items/{workItemId:guid}/work-details")]
    public async Task<ActionResult<ApiResponse<QuotationWorkDetailDto>>> CreateWorkDetail(Guid workItemId, [FromBody] SaveWorkDetailRequest request)
    {
        try
        {
            var result = await _svc.CreateWorkDetailAsync(workItemId, request);
            return Ok(ApiResponse<QuotationWorkDetailDto>.Ok(result, "Detail Kerja berhasil ditambahkan."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPut("work-details/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateWorkDetail(Guid id, [FromBody] SaveWorkDetailRequest request)
    {
        var ok = await _svc.UpdateWorkDetailAsync(id, request);
        if (!ok) return NotFound(ApiResponse.Fail("Detail Kerja tidak ditemukan."));
        return Ok(ApiResponse.Ok("Detail Kerja berhasil diperbarui."));
    }

    [HttpDelete("work-details/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkDetail(Guid id)
    {
        var ok = await _svc.DeleteWorkDetailAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Detail Kerja tidak ditemukan."));
        return Ok(ApiResponse.Ok("Detail Kerja berhasil dihapus."));
    }

    [HttpPost("work-details/{id:guid}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<QuotationWorkDetailAttachmentDto>>> UploadWorkDetailAttachment(Guid id, IFormFile file)
    {
        try
        {
            var result = await _svc.UploadWorkDetailAttachmentAsync(id, file);
            return Ok(ApiResponse<QuotationWorkDetailAttachmentDto>.Ok(result, "Gambar berhasil diunggah."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpGet("work-details/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> GetWorkDetailAttachment(Guid attachmentId)
    {
        var result = await _svc.GetWorkDetailAttachmentAsync(attachmentId);
        if (result is null) return NotFound(ApiResponse.Fail("Gambar tidak ditemukan."));
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    [HttpDelete("work-details/attachments/{attachmentId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkDetailAttachment(Guid attachmentId)
    {
        var ok = await _svc.DeleteWorkDetailAttachmentAsync(attachmentId);
        if (!ok) return NotFound(ApiResponse.Fail("Gambar tidak ditemukan."));
        return Ok(ApiResponse.Ok("Gambar berhasil dihapus."));
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
