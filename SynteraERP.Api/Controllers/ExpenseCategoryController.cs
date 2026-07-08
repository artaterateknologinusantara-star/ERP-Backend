using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Expense;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/expense-categories")]
public class ExpenseCategoryController : ControllerBase
{
    private readonly IExpenseCategoryService _svc;

    public ExpenseCategoryController(IExpenseCategoryService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryDto>>>> List([FromQuery] bool? isActive)
    {
        var result = await _svc.ListAsync(isActive);
        return Ok(ApiResponse<List<ExpenseCategoryDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<ExpenseCategoryDto>.Fail("Expense Category tidak ditemukan."));
        return Ok(ApiResponse<ExpenseCategoryDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> Create([FromBody] CreateExpenseCategoryRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<ExpenseCategoryDto>.Ok(item, "Expense Category berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> Update(Guid id, [FromBody] UpdateExpenseCategoryRequest request)
    {
        var item = await _svc.UpdateAsync(id, request);
        if (item is null) return NotFound(ApiResponse<ExpenseCategoryDto>.Fail("Expense Category tidak ditemukan."));
        return Ok(ApiResponse<ExpenseCategoryDto>.Ok(item, "Expense Category berhasil diperbarui."));
    }
}
