using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

// Internal/maincon-facing — scheme default ("Bearer"), lewat [Authorize] class-level seperti
// mayoritas controller lain di codebase ini (RequirePermission hanya dipakai di aksi approve,
// sama seperti pola di QuotationController).
[Authorize]
[ApiController]
public class VendorRabRequestController : ControllerBase
{
    private readonly IVendorRabRequestService _svc;

    public VendorRabRequestController(IVendorRabRequestService svc)
    {
        _svc = svc;
    }

    [HttpPost("api/quotations/groups/{groupId:guid}/rab-requests")]
    public async Task<ActionResult<ApiResponse<VendorRabRequestDto>>> CreateAndSend(
        Guid groupId, [FromBody] CreateVendorRabRequestRequest request)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<VendorRabRequestDto>.Fail("User tidak teridentifikasi."));

        try
        {
            var result = await _svc.CreateAndSendAsync(groupId, userId, request);
            return Ok(ApiResponse<VendorRabRequestDto>.Ok(result, "Permintaan RAB berhasil dikirim ke vendor."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VendorRabRequestDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorRabRequestDto>.Fail(ex.Message));
        }
    }

    [HttpGet("api/quotations/groups/{groupId:guid}/rab-requests")]
    public async Task<ActionResult<ApiResponse<List<VendorRabRequestDto>>>> ListByGroup(Guid groupId)
    {
        var result = await _svc.ListByGroupAsync(groupId);
        return Ok(ApiResponse<List<VendorRabRequestDto>>.Ok(result));
    }

    [HttpGet("api/vendor-rab-requests/{id:guid}")]
    public async Task<ActionResult<ApiResponse<VendorRabRequestDto>>> GetById(Guid id)
    {
        var result = await _svc.GetByIdAsync(id);
        if (result is null) return NotFound(ApiResponse<VendorRabRequestDto>.Fail("Permintaan RAB tidak ditemukan."));
        return Ok(ApiResponse<VendorRabRequestDto>.Ok(result));
    }
}
