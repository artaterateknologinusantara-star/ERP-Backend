using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/purchase-requests")]
public class PurchaseRequestController : ControllerBase
{
    private readonly IPurchaseRequestService _svc;
    private readonly IAuthorizationService _authz;

    public PurchaseRequestController(IPurchaseRequestService svc, IAuthorizationService authz)
    {
        _svc = svc;
        _authz = authz;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PurchaseRequestListDto>>>> List([FromQuery] PurchaseRequestQueryParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<PurchaseRequestListDto>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _svc.GetStatsAsync();
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<PurchaseRequestDto>.Fail("Purchase Request tidak ditemukan."));
        return Ok(ApiResponse<PurchaseRequestDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDto>>> Create([FromBody] CreatePurchaseRequestRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<PurchaseRequestDto>.Ok(item, "Purchase Request berhasil dibuat."));
    }

    [HttpPost("generate-from-so/{soId:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDto>>> GenerateFromSo(Guid soId)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<PurchaseRequestDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.GenerateFromSoAsync(soId, userId);
        if (item is null)
            return NotFound(ApiResponse<PurchaseRequestDto>.Fail("Sales Order tidak ditemukan atau sudah dihapus."));

        return CreatedAtAction(nameof(Get), new { id = item.Id },
            ApiResponse<PurchaseRequestDto>.Ok(item, "Purchase Request berhasil di-generate dari Sales Order."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdatePRStatusRequest request)
    {
        // Purchase Request has no dedicated /approve endpoint — Approved/Rejected are set through
        // this generic status setter, so the Approve-permission gate has to live here.
        if (string.Equals(request.Status, nameof(PurchaseRequestStatus.Approved), StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.Status, nameof(PurchaseRequestStatus.Rejected), StringComparison.OrdinalIgnoreCase))
        {
            var authResult = await _authz.AuthorizeAsync(User, new ModulePermissionRequirement(Modules.Purchasing, PermissionActions.Approve).PolicyName);
            if (!authResult.Succeeded) return Forbid();
        }

        var ok = await _svc.UpdateStatusAsync(id, request.Status);
        if (!ok) return BadRequest(ApiResponse.Fail("Status tidak valid atau Purchase Request tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Purchase Request tidak ditemukan."));
        return Ok(ApiResponse.Ok("Purchase Request berhasil dihapus."));
    }
}

[Authorize]
[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _svc;

    public PurchaseOrderController(IPurchaseOrderService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PurchaseOrderListDto>>>> List(
        [FromQuery] PurchaseOrderQueryParams p, [FromQuery] Guid? purchaseRequestId, [FromQuery] string? purchaseRequestIds)
    {
        IEnumerable<Guid>? ids = null;
        if (!string.IsNullOrWhiteSpace(purchaseRequestIds))
        {
            ids = purchaseRequestIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => Guid.TryParse(s, out _))
                .Select(Guid.Parse);
        }

        var result = await _svc.ListAsync(p, purchaseRequestId, ids);
        return Ok(ApiResponse<PaginatedResponse<PurchaseOrderListDto>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _svc.GetStatsAsync();
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<PurchaseOrderDto>.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<PurchaseOrderDto>.Ok(item, "Purchase Order berhasil dibuat."));
    }

    [HttpPost("from-pr/{prId:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreateFromPr(Guid prId, [FromBody] CreatePoFromPrRequest request)
    {
        var item = await _svc.CreateFromPrAsync(prId, request);
        if (item is null)
            return NotFound(ApiResponse<PurchaseOrderDto>.Fail("Purchase Request tidak ditemukan, atau statusnya bukan Approved/PartiallyOrdered."));
        return CreatedAtAction(nameof(Get), new { id = item.Id },
            ApiResponse<PurchaseOrderDto>.Ok(item, "Purchase Order berhasil dibuat dari Purchase Request."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdatePOStatusRequest request)
    {
        var ok = await _svc.UpdateStatusAsync(id, request.Status);
        if (!ok) return BadRequest(ApiResponse.Fail("Status tidak valid atau Purchase Order tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> ReceiveGoods(Guid id, [FromBody] ReceiveGoodsRequest request)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<PurchaseOrderDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.ReceiveGoodsAsync(id, request, userId);
        if (item is null) return NotFound(ApiResponse<PurchaseOrderDto>.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(item, "Penerimaan barang berhasil dicatat."));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPOPaymentRequest request)
    {
        try
        {
            var result = await _svc.RecordPaymentAsync(id, request);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse.Ok("Purchase Order berhasil dihapus."));
    }
}
