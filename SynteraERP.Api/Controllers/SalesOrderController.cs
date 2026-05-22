using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.SalesOrder;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sales-orders")]
public class SalesOrderController : ControllerBase
{
    private readonly ISalesOrderService _svc;

    public SalesOrderController(ISalesOrderService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SalesOrderListDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<SalesOrderListDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<SalesOrderDto>.Fail("Sales Order tidak ditemukan."));
        return Ok(ApiResponse<SalesOrderDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Create([FromBody] CreateSalesOrderRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<SalesOrderDto>.Ok(item, "Sales Order berhasil dibuat."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateSalesOrderStatusRequest request)
    {
        var ok = await _svc.UpdateStatusAsync(id, request.Status);
        if (!ok) return BadRequest(ApiResponse.Fail("Status tidak valid atau Sales Order tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Sales Order tidak ditemukan."));
        return Ok(ApiResponse.Ok("Sales Order berhasil dihapus."));
    }
}
