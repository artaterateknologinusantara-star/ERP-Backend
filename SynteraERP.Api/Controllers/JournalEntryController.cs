using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/journal-entries")]
public class JournalEntryController : ControllerBase
{
    private readonly IJournalPostingService _svc;

    public JournalEntryController(IJournalPostingService svc) => _svc = svc;

    [RequirePermission(Modules.Accounting, PermissionActions.View)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<JournalEntryListDto>>>> List([FromQuery] JournalEntryQueryParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<JournalEntryListDto>>.Ok(result));
    }

    [RequirePermission(Modules.Accounting, PermissionActions.View)]
    [HttpGet("trial-balance")]
    public async Task<ActionResult<ApiResponse<List<TrialBalanceRowDto>>>> TrialBalance([FromQuery] DateTimeOffset? asOfDate)
    {
        var result = await _svc.GetTrialBalanceAsync(asOfDate);
        return Ok(ApiResponse<List<TrialBalanceRowDto>>.Ok(result));
    }

    [RequirePermission(Modules.Accounting, PermissionActions.View)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<JournalEntryDto>.Fail("Journal entry tidak ditemukan."));
        return Ok(ApiResponse<JournalEntryDto>.Ok(item));
    }

    [RequirePermission(Modules.Accounting, PermissionActions.Create)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> Create([FromBody] CreateJournalEntryRequest request)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<JournalEntryDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.CreateManualEntryAsync(request, userId);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<JournalEntryDto>.Ok(item, "Journal entry berhasil dibuat."));
    }

    [RequirePermission(Modules.Accounting, PermissionActions.Create)]
    [HttpPost("opening-balance")]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> CreateOpeningBalance([FromBody] CreateOpeningBalanceRequest request)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<JournalEntryDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.CreateOpeningBalanceAsync(request, userId);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<JournalEntryDto>.Ok(item, "Opening Balance berhasil dibuat."));
    }

    [RequirePermission(Modules.Accounting, PermissionActions.Approve)]
    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> Post(Guid id)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<JournalEntryDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.PostDraftEntryAsync(id, userId);
        return Ok(ApiResponse<JournalEntryDto>.Ok(item, "Journal entry berhasil di-post."));
    }

    [RequirePermission(Modules.Accounting, PermissionActions.Approve)]
    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> Reverse(Guid id)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<JournalEntryDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.ReverseAsync(id, userId);
        return Ok(ApiResponse<JournalEntryDto>.Ok(item, "Journal entry berhasil di-reverse."));
    }
}
