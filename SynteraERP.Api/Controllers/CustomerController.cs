using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Customer;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _svc;

    public CustomerController(ICustomerService svc) => _svc = svc;

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CustomerDto>>>> List([FromQuery] CustomerParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<CustomerDto>>.Ok(result));
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<CustomerDto>.Fail("Customer tidak ditemukan."));
        return Ok(ApiResponse<CustomerDto>.Ok(item));
    }

    [RequirePermission(Modules.Sales, PermissionActions.Create)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create([FromBody] CreateCustomerRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<CustomerDto>.Ok(item, "Customer berhasil dibuat."));
    }

    [RequirePermission(Modules.Sales, PermissionActions.Edit)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var item = await _svc.UpdateAsync(id, request);
        if (item is null) return NotFound(ApiResponse<CustomerDto>.Fail("Customer tidak ditemukan."));
        return Ok(ApiResponse<CustomerDto>.Ok(item, "Customer berhasil diperbarui."));
    }

    [RequirePermission(Modules.Sales, PermissionActions.Edit)]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> SetStatus(Guid id, [FromBody] SetStatusRequest request)
    {
        var ok = await _svc.SetStatusAsync(id, request.IsActive);
        if (!ok) return NotFound(ApiResponse.Fail("Customer tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }

    [RequirePermission(Modules.Sales, PermissionActions.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Customer tidak ditemukan."));
        return Ok(ApiResponse.Ok("Customer berhasil dihapus."));
    }
}

public class SetStatusRequest
{
    public bool IsActive { get; set; }
}
