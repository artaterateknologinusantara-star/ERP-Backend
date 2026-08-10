using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Branch;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/branches")]
public class BranchController : ControllerBase
{
    private readonly IBranchService _svc;

    public BranchController(IBranchService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<BranchDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<BranchDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<BranchDto>.Fail("Cabang tidak ditemukan."));
        return Ok(ApiResponse<BranchDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Create([FromBody] CreateBranchRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<BranchDto>.Fail(FirstModelError()));

        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<BranchDto>.Ok(item, "Cabang berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Update(Guid id, [FromBody] UpdateBranchRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<BranchDto>.Fail(FirstModelError()));

        var item = await _svc.UpdateAsync(id, request);
        if (item is null) return NotFound(ApiResponse<BranchDto>.Fail("Cabang tidak ditemukan."));
        return Ok(ApiResponse<BranchDto>.Ok(item, "Cabang berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Cabang tidak ditemukan."));
        return Ok(ApiResponse.Ok("Cabang berhasil dihapus."));
    }

    private string FirstModelError()
    {
        var message = string.Join(" ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m)));
        return string.IsNullOrWhiteSpace(message) ? "Data tidak valid." : message;
    }
}
