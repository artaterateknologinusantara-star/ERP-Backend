using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Invoice;
using SynteraERP.Api.DTOs.SalesOrderPayment;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _svc;
    private readonly InvoicePdfService _pdfSvc;
    private readonly ISalesOrderPaymentService _dpSvc;

    public InvoiceController(IInvoiceService svc, InvoicePdfService pdfSvc, ISalesOrderPaymentService dpSvc)
    {
        _svc = svc;
        _pdfSvc = pdfSvc;
        _dpSvc = dpSvc;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<InvoiceListDto>>>> List([FromQuery] InvoiceQueryParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<InvoiceListDto>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _svc.GetStatsAsync();
        return Ok(new { success = true, data = result });
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

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var pdfBytes = await _pdfSvc.GenerateAsync(id);
        if (pdfBytes is null) return NotFound();
        return File(pdfBytes, "application/pdf", $"Invoice_{id}.pdf");
    }

    [HttpPost("{id:guid}/apply-down-payment")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> ApplyDownPayment(Guid id, [FromBody] ApplyDownPaymentRequest request)
    {
        var item = await _dpSvc.ApplyToInvoiceAsync(id, request);
        if (item is null) return NotFound(ApiResponse<InvoiceDto>.Fail("Invoice tidak ditemukan."));
        return Ok(ApiResponse<InvoiceDto>.Ok(item, "Down Payment berhasil diterapkan ke Invoice."));
    }
}
