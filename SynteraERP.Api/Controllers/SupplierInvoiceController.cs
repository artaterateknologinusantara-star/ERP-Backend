using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.DTOs.SupplierInvoice;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/supplier-invoices")]
public class SupplierInvoiceController : ControllerBase
{
    private readonly ISupplierInvoiceService _svc;

    public SupplierInvoiceController(ISupplierInvoiceService svc) => _svc = svc;

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SupplierInvoiceListDto>>>> List([FromQuery] SupplierInvoiceQueryParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<SupplierInvoiceListDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<SupplierInvoiceDto>.Fail("Supplier Invoice tidak ditemukan."));
        return Ok(ApiResponse<SupplierInvoiceDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> Create([FromBody] CreateSupplierInvoiceRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<SupplierInvoiceDto>.Ok(item, "Supplier Invoice berhasil dibuat."));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> Approve(Guid id)
    {
        var item = await _svc.ApproveAsync(id, GetUserId());
        if (item is null) return NotFound(ApiResponse<SupplierInvoiceDto>.Fail("Supplier Invoice tidak ditemukan."));
        return Ok(ApiResponse<SupplierInvoiceDto>.Ok(item, "Supplier Invoice berhasil di-approve."));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<ApiResponse<SupplierInvoiceDto>>> RecordPayment(Guid id, [FromBody] RecordPOPaymentRequest request)
    {
        var item = await _svc.RecordPaymentAsync(id, request);
        if (item is null) return NotFound(ApiResponse<SupplierInvoiceDto>.Fail("Supplier Invoice tidak ditemukan."));
        return Ok(ApiResponse<SupplierInvoiceDto>.Ok(item, "Pembayaran berhasil dicatat."));
    }
}
