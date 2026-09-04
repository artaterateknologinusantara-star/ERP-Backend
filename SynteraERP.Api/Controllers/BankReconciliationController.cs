using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.BankReconciliation;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bank-reconciliation")]
public class BankReconciliationController : ControllerBase
{
    private readonly IBankReconciliationService _svc;

    public BankReconciliationController(IBankReconciliationService svc) => _svc = svc;

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] ImportBankStatementRequest request, IFormFile file)
    {
        var result = await _svc.ImportAsync(request, file);

        if (!result.Success)
        {
            return BadRequest(new ApiResponse<List<CsvRowError>>
            {
                Success = false,
                Message = $"Import CSV ditolak, ditemukan {result.RowErrors!.Count} baris error. Tidak ada data yang disimpan.",
                Data = result.RowErrors,
            });
        }

        return Ok(ApiResponse<BankStatementImportSummaryDto>.Ok(result.Summary!, "Import CSV berhasil."));
    }

    [HttpGet("imports")]
    public async Task<ActionResult<ApiResponse<List<BankStatementImportListDto>>>> ListImports([FromQuery] Guid accountId)
    {
        var result = await _svc.ListImportsAsync(accountId);
        return Ok(ApiResponse<List<BankStatementImportListDto>>.Ok(result));
    }

    [HttpGet("imports/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BankStatementImportDetailDto>>> GetImportDetail(Guid id)
    {
        var result = await _svc.GetImportDetailAsync(id);
        if (result is null) return NotFound(ApiResponse<BankStatementImportDetailDto>.Fail("Import tidak ditemukan."));
        return Ok(ApiResponse<BankStatementImportDetailDto>.Ok(result));
    }

    [HttpPost("lines/{lineId:guid}/match")]
    public async Task<ActionResult<ApiResponse<BankStatementLineDetailDto>>> Match(Guid lineId, [FromBody] MatchLineRequest request)
    {
        var result = await _svc.MatchAsync(lineId, request.JournalEntryLineId);
        return Ok(ApiResponse<BankStatementLineDetailDto>.Ok(result, "Baris berhasil di-match."));
    }

    [HttpPost("lines/{lineId:guid}/unmatch")]
    public async Task<ActionResult<ApiResponse<BankStatementLineDetailDto>>> Unmatch(Guid lineId)
    {
        var result = await _svc.UnmatchAsync(lineId);
        return Ok(ApiResponse<BankStatementLineDetailDto>.Ok(result, "Match dibatalkan."));
    }

    [HttpPost("lines/{lineId:guid}/ignore")]
    public async Task<ActionResult<ApiResponse<BankStatementLineDetailDto>>> Ignore(Guid lineId)
    {
        var result = await _svc.IgnoreAsync(lineId);
        return Ok(ApiResponse<BankStatementLineDetailDto>.Ok(result, "Baris ditandai diabaikan."));
    }

    [HttpGet("balances")]
    public async Task<ActionResult<ApiResponse<List<AccountBalanceDto>>>> GetBalances([FromQuery] DateOnly asOf)
    {
        if (asOf == default)
            return BadRequest(ApiResponse<List<AccountBalanceDto>>.Fail("Parameter asOf wajib diisi."));

        var result = await _svc.GetBalancesAsync(asOf);
        return Ok(ApiResponse<List<AccountBalanceDto>>.Ok(result));
    }
}
