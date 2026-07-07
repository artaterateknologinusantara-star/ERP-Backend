using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Account;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _svc;

    public AccountController(IAccountService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AccountDto>>>> GetTree()
    {
        var result = await _svc.GetTreeAsync();
        return Ok(ApiResponse<List<AccountDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<AccountDto>.Fail("Account tidak ditemukan."));
        return Ok(ApiResponse<AccountDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountDto>>> Create([FromBody] CreateAccountRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<AccountDto>.Ok(item, "Account berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountDto>>> Update(Guid id, [FromBody] UpdateAccountRequest request)
    {
        var item = await _svc.UpdateAsync(id, request);
        if (item is null) return NotFound(ApiResponse<AccountDto>.Fail("Account tidak ditemukan."));
        return Ok(ApiResponse<AccountDto>.Ok(item, "Account berhasil diperbarui."));
    }
}
