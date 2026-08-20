using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.CustomerPO;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/customer-pos")]
public class CustomerPoController : ControllerBase
{
    private readonly ICustomerPoService _svc;

    public CustomerPoController(ICustomerPoService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CustomerPoListDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<CustomerPoListDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerPoDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<CustomerPoDto>.Fail("Customer PO tidak ditemukan."));
        return Ok(ApiResponse<CustomerPoDto>.Ok(item));
    }

    [HttpGet("by-quotation/{quotationId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerPoDto>>> GetByQuotation(Guid quotationId)
    {
        var item = await _svc.GetByQuotationIdAsync(quotationId);
        if (item is null) return NotFound(ApiResponse<CustomerPoDto>.Fail("Customer PO untuk quotation ini belum diinput."));
        return Ok(ApiResponse<CustomerPoDto>.Ok(item));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<CustomerPoDto>>> Create(
        [FromForm] CreateCustomerPoRequest request,
        IFormFile? attachment)
    {
        var item = await _svc.CreateAsync(request, attachment);
        return CreatedAtAction(nameof(Get), new { id = item.Id },
            ApiResponse<CustomerPoDto>.Ok(item, "Customer PO berhasil disimpan."));
    }

    [HttpGet("{id:guid}/attachment")]
    public async Task<IActionResult> GetAttachment(Guid id)
    {
        var result = await _svc.GetAttachmentAsync(id);
        if (result is null) return NotFound(ApiResponse.Fail("Lampiran tidak ditemukan."));
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Customer PO tidak ditemukan."));
        return Ok(ApiResponse.Ok("Customer PO berhasil dihapus."));
    }

    [HttpPatch("{id:guid}/number")]
    public async Task<ActionResult<ApiResponse<CustomerPoDto>>> UpdateNumber(Guid id, [FromBody] UpdateNumberRequest req)
    {
        try
        {
            var updated = await _svc.UpdateNumberAsync(id, req.NewPoNo, req.Reason);
            return Ok(ApiResponse<CustomerPoDto>.Ok(updated, "Nomor PO berhasil diperbarui."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CustomerPoDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CustomerPoDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CustomerPoDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomerPoHistoryDto>>>> History(Guid id)
    {
        var items = await _svc.GetHistoryAsync(id);
        return Ok(ApiResponse<IEnumerable<CustomerPoHistoryDto>>.Ok(items));
    }
}

public class UpdateNumberRequest
{
    public string NewPoNo { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
