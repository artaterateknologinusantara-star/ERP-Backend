using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/journal-entries")]
public class JournalEntryController : ControllerBase
{
    private readonly IJournalPostingService _svc;

    public JournalEntryController(IJournalPostingService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<JournalEntryListDto>>>> List([FromQuery] JournalEntryQueryParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<JournalEntryListDto>>.Ok(result));
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<ApiResponse<List<TrialBalanceRowDto>>>> TrialBalance([FromQuery] DateTimeOffset? asOfDate)
    {
        var result = await _svc.GetTrialBalanceAsync(asOfDate);
        return Ok(ApiResponse<List<TrialBalanceRowDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<JournalEntryDto>.Fail("Journal entry tidak ditemukan."));
        return Ok(ApiResponse<JournalEntryDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> Create([FromBody] CreateJournalEntryRequest request)
    {
        var item = await _svc.CreateManualEntryAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<JournalEntryDto>.Ok(item, "Journal entry berhasil dibuat."));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> Reverse(Guid id)
    {
        var item = await _svc.ReverseAsync(id);
        return Ok(ApiResponse<JournalEntryDto>.Ok(item, "Journal entry berhasil di-reverse."));
    }
}
