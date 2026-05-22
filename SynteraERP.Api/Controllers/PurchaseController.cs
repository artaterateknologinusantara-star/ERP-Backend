using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/purchase-requests")]
public class PurchaseRequestController : ControllerBase
{
    private readonly IPurchaseRequestService _svc;

    public PurchaseRequestController(IPurchaseRequestService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PurchaseRequestListDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<PurchaseRequestListDto>>.Ok(result));
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

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdatePRStatusRequest request)
    {
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

    public PurchaseOrderController(IPurchaseOrderService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PurchaseOrderListDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<PurchaseOrderListDto>>.Ok(result));
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
        var item = await _svc.ReceiveGoodsAsync(id, request);
        if (item is null) return NotFound(ApiResponse<PurchaseOrderDto>.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(item, "Penerimaan barang berhasil dicatat."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse.Ok("Purchase Order berhasil dihapus."));
    }
}
