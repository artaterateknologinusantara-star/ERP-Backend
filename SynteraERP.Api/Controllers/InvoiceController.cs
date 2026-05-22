using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _svc;

    public InvoiceController(IInvoiceService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<InvoiceListDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<InvoiceListDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<InvoiceDto>.Fail("Invoice tidak ditemukan."));
        return Ok(ApiResponse<InvoiceDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> Create([FromBody] CreateInvoiceRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<InvoiceDto>.Ok(item, "Invoice berhasil dibuat."));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        var item = await _svc.RecordPaymentAsync(id, request);
        if (item is null) return NotFound(ApiResponse<InvoiceDto>.Fail("Invoice tidak ditemukan."));
        return Ok(ApiResponse<InvoiceDto>.Ok(item, "Pembayaran berhasil dicatat."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Invoice tidak ditemukan."));
        return Ok(ApiResponse.Ok("Invoice berhasil dihapus."));
    }
}
