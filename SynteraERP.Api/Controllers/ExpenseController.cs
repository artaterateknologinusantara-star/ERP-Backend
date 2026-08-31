using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Expense;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/expenses")]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _svc;

    public ExpenseController(IExpenseService svc) => _svc = svc;

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ExpenseListDto>>>> List([FromQuery] ExpenseQueryParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<ExpenseListDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<ExpenseDto>.Fail("Expense tidak ditemukan."));
        return Ok(ApiResponse<ExpenseDto>.Ok(item));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Create(
        [FromForm] CreateExpenseRequest request,
        IFormFile? attachment)
    {
        var item = await _svc.CreateAsync(request, attachment);
        return CreatedAtAction(nameof(Get), new { id = item.Id },
            ApiResponse<ExpenseDto>.Ok(item, "Expense berhasil dibuat."));
    }

    [HttpGet("{id:guid}/attachment")]
    public async Task<IActionResult> GetAttachment(Guid id)
    {
        var result = await _svc.GetAttachmentAsync(id);
        if (result is null) return NotFound(ApiResponse.Fail("Lampiran tidak ditemukan."));
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Submit(Guid id)
    {
        var item = await _svc.SubmitAsync(id);
        if (item is null) return NotFound(ApiResponse<ExpenseDto>.Fail("Expense tidak ditemukan."));
        return Ok(ApiResponse<ExpenseDto>.Ok(item, "Expense berhasil di-submit."));
    }

    [RequirePermission(Modules.Finance, PermissionActions.Approve)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Approve(Guid id)
    {
        var item = await _svc.ApproveAsync(id, GetUserId());
        if (item is null) return NotFound(ApiResponse<ExpenseDto>.Fail("Expense tidak ditemukan."));
        return Ok(ApiResponse<ExpenseDto>.Ok(item, "Expense berhasil di-approve."));
    }

    [RequirePermission(Modules.Finance, PermissionActions.Approve)]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Reject(Guid id, [FromBody] RejectExpenseRequest? request)
    {
        var item = await _svc.RejectAsync(id, request?.Reason);
        if (item is null) return NotFound(ApiResponse<ExpenseDto>.Fail("Expense tidak ditemukan."));
        return Ok(ApiResponse<ExpenseDto>.Ok(item, "Expense berhasil ditolak."));
    }
}
