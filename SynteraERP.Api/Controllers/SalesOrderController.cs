using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.SalesOrder;
using SynteraERP.Api.DTOs.SalesOrderPayment;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sales-orders")]
public class SalesOrderController : ControllerBase
{
    private readonly ISalesOrderService _svc;
    private readonly SalesOrderPdfService _pdfSvc;
    private readonly ISalesOrderPaymentService _dpSvc;

    public SalesOrderController(ISalesOrderService svc, SalesOrderPdfService pdfSvc, ISalesOrderPaymentService dpSvc)
    {
        _svc = svc;
        _pdfSvc = pdfSvc;
        _dpSvc = dpSvc;
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SalesOrderListResponse>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var result = await _svc.GetListAsync(page, perPage, search, status);
        return Ok(ApiResponse<PaginatedResponse<SalesOrderListResponse>>.Ok(result));
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _svc.GetStatsAsync();
        return Ok(new { success = true, data = result });
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesOrderDetailResponse>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<SalesOrderDetailResponse>.Fail("Sales Order tidak ditemukan."));
        return Ok(ApiResponse<SalesOrderDetailResponse>.Ok(item));
    }

    [RequirePermission(Modules.Sales, PermissionActions.Create)]
    [HttpPost("from-quotation/{quotationId:guid}")]
    public async Task<IActionResult> CreateFromQuotation(Guid quotationId)
    {
        try
        {
            var userId = Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
                out var uid) ? uid : Guid.Empty;

            var result = await _svc.CreateFromQuotationAsync(quotationId, userId);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [RequirePermission(Modules.Sales, PermissionActions.Edit)]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateSalesOrderStatusRequest request)
    {
        try
        {
            await _svc.UpdateStatusAsync(id, request.Status);
            return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [RequirePermission(Modules.Sales, PermissionActions.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        try
        {
            await _svc.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Sales Order berhasil dihapus."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var pdfBytes = await _pdfSvc.GenerateAsync(id);
        if (pdfBytes is null) return NotFound();
        return File(pdfBytes, "application/pdf", $"SO_{id}.pdf");
    }

    [RequirePermission(Modules.Sales, PermissionActions.Edit)]
    [HttpPost("{id:guid}/down-payments")]
    public async Task<ActionResult<ApiResponse<SalesOrderPaymentDto>>> RecordDownPayment(Guid id, [FromBody] RecordDownPaymentRequest request)
    {
        var item = await _dpSvc.RecordDownPaymentAsync(id, request);
        return Ok(ApiResponse<SalesOrderPaymentDto>.Ok(item, "Down Payment berhasil dicatat."));
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet("{id:guid}/down-payments")]
    public async Task<ActionResult<ApiResponse<List<SalesOrderPaymentDto>>>> ListDownPayments(Guid id)
    {
        var items = await _dpSvc.ListForSalesOrderAsync(id);
        return Ok(ApiResponse<List<SalesOrderPaymentDto>>.Ok(items));
    }
}
