using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

// Internal/maincon-facing review workflow — scheme default ("Bearer"). SetLineMarkup/Approve/Reject
// digerbang dengan RequirePermission(Sales, Approve), sama seperti Quotation.Approve/Reject — markup
// ikut digerbang karena langsung menentukan FinalUnitPrice yang ditulis ke Quotation resmi saat
// approve, jadi bukan cuma "lihat", harus permission yang sama dengan approve itu sendiri.
[Authorize]
[ApiController]
[Route("api/vendor-submissions")]
public class VendorRabSubmissionController : ControllerBase
{
    private readonly IVendorRabSubmissionService _svc;

    public VendorRabSubmissionController(IVendorRabSubmissionService svc)
    {
        _svc = svc;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VendorRabSubmissionDto>>> GetById(Guid id)
    {
        var result = await _svc.GetByIdAsync(id);
        if (result is null) return NotFound(ApiResponse<VendorRabSubmissionDto>.Fail("Submission tidak ditemukan."));
        return Ok(ApiResponse<VendorRabSubmissionDto>.Ok(result));
    }

    [RequirePermission(Modules.Sales, PermissionActions.Approve)]
    [HttpPut("{id:guid}/lines/{lineId:guid}/markup")]
    public async Task<ActionResult<ApiResponse>> SetLineMarkup(
        Guid id, Guid lineId, [FromBody] SetSubmissionLineMarkupRequest request)
    {
        try
        {
            var ok = await _svc.SetLineMarkupAsync(id, lineId, request.MarkupAmount);
            if (!ok) return NotFound(ApiResponse.Fail("Baris submission tidak ditemukan."));
            return Ok(ApiResponse.Ok("Markup berhasil disimpan."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [RequirePermission(Modules.Sales, PermissionActions.Approve)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<Guid>>> Approve(Guid id)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<Guid>.Fail("User tidak teridentifikasi."));

        try
        {
            var workItemId = await _svc.ApproveAsync(id, userId);
            return Ok(ApiResponse<Guid>.Ok(workItemId, "Submission disetujui, baris RAB resmi sudah ditambahkan ke Quotation."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<Guid>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<Guid>.Fail(ex.Message));
        }
    }

    [RequirePermission(Modules.Sales, PermissionActions.Approve)]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse>> Reject(Guid id, [FromBody] RejectVendorRabSubmissionRequest request)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse.Fail("User tidak teridentifikasi."));

        try
        {
            var ok = await _svc.RejectAsync(id, userId, request.Reason);
            if (!ok) return NotFound(ApiResponse.Fail("Submission tidak ditemukan."));
            return Ok(ApiResponse.Ok("Submission ditolak. Vendor bisa submit ulang."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
