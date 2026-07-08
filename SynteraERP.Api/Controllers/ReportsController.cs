using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.JournalEntry;
using SynteraERP.Api.DTOs.Reports;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _svc;
    private readonly ReportsPdfService _pdfSvc;

    public ReportsController(IReportsService svc, ReportsPdfService pdfSvc)
    {
        _svc = svc;
        _pdfSvc = pdfSvc;
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<ApiResponse<List<TrialBalanceRowDto>>>> TrialBalance([FromQuery] DateOnly? asOfDate)
    {
        var result = await _svc.GetTrialBalanceAsync(asOfDate);
        return Ok(ApiResponse<List<TrialBalanceRowDto>>.Ok(result));
    }

    [HttpGet("trial-balance/pdf")]
    public async Task<IActionResult> TrialBalancePdf([FromQuery] DateOnly? asOfDate)
    {
        var bytes = await _pdfSvc.GenerateTrialBalanceAsync(asOfDate);
        return File(bytes, "application/pdf", $"TrialBalance_{DateOnly.FromDateTime(DateTime.UtcNow):yyyyMMdd}.pdf");
    }

    [HttpGet("income-statement")]
    public async Task<ActionResult<ApiResponse<IncomeStatementDto>>> IncomeStatement([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
    {
        var result = await _svc.GetIncomeStatementAsync(startDate, endDate);
        return Ok(ApiResponse<IncomeStatementDto>.Ok(result));
    }

    [HttpGet("income-statement/pdf")]
    public async Task<IActionResult> IncomeStatementPdf([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
    {
        var bytes = await _pdfSvc.GenerateIncomeStatementAsync(startDate, endDate);
        return File(bytes, "application/pdf", $"LabaRugi_{DateOnly.FromDateTime(DateTime.UtcNow):yyyyMMdd}.pdf");
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<ApiResponse<BalanceSheetDto>>> BalanceSheet([FromQuery] DateOnly? asOfDate)
    {
        var result = await _svc.GetBalanceSheetAsync(asOfDate);
        return Ok(ApiResponse<BalanceSheetDto>.Ok(result));
    }

    [HttpGet("balance-sheet/pdf")]
    public async Task<IActionResult> BalanceSheetPdf([FromQuery] DateOnly? asOfDate)
    {
        var bytes = await _pdfSvc.GenerateBalanceSheetAsync(asOfDate);
        return File(bytes, "application/pdf", $"Neraca_{DateOnly.FromDateTime(DateTime.UtcNow):yyyyMMdd}.pdf");
    }

    [HttpGet("general-ledger/{accountId:guid}")]
    public async Task<ActionResult<ApiResponse<GeneralLedgerDto>>> GeneralLedger(Guid accountId, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
    {
        var result = await _svc.GetGeneralLedgerAsync(accountId, startDate, endDate);
        if (result is null) return NotFound(ApiResponse<GeneralLedgerDto>.Fail("Akun tidak ditemukan."));
        return Ok(ApiResponse<GeneralLedgerDto>.Ok(result));
    }

    [HttpGet("general-ledger/{accountId:guid}/pdf")]
    public async Task<IActionResult> GeneralLedgerPdf(Guid accountId, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
    {
        var bytes = await _pdfSvc.GenerateGeneralLedgerAsync(accountId, startDate, endDate);
        if (bytes is null) return NotFound();
        return File(bytes, "application/pdf", $"BukuBesar_{DateOnly.FromDateTime(DateTime.UtcNow):yyyyMMdd}.pdf");
    }
}
