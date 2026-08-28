using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.SystemReset;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

// Dev/testing utility only: hard-deletes (ExecuteDeleteAsync + IgnoreQueryFilters) every
// transactional record in the system — Quotation, SO, Invoice, PO/PR, Stock, DO, Project.
// There is no legitimate production use case for a self-service "wipe all data" endpoint in
// a system of record for official reporting (SPT PPN, auditor, bank), so it is hard-disabled
// outside Development rather than just role-gated — a role check alone would still be a single
// privilege-escalation bug away from a catastrophic, irreversible data-loss endpoint.
[Authorize]
[ApiController]
[Route("api/system/reset")]
public class SystemResetController : ControllerBase
{
    private readonly ISystemResetService _svc;
    private readonly IWebHostEnvironment _env;

    public SystemResetController(ISystemResetService svc, IWebHostEnvironment env)
    {
        _svc = svc;
        _env = env;
    }

    [HttpPost("quotations")]
    public Task<IActionResult> ResetQuotations() => Execute(uid => _svc.ResetQuotationsAsync(uid, UserIp));

    [HttpPost("sales")]
    public Task<IActionResult> ResetSales() => Execute(uid => _svc.ResetSalesAsync(uid, UserIp));

    [HttpPost("purchasing")]
    public Task<IActionResult> ResetPurchasing() => Execute(uid => _svc.ResetPurchasingAsync(uid, UserIp));

    [HttpPost("finance")]
    public Task<IActionResult> ResetFinance() => Execute(uid => _svc.ResetFinanceAsync(uid, UserIp));

    [HttpPost("projects")]
    public Task<IActionResult> ResetProjects() => Execute(uid => _svc.ResetProjectsAsync(uid, UserIp));

    [HttpPost("inventory")]
    public Task<IActionResult> ResetInventory() => Execute(uid => _svc.ResetInventoryAsync(uid, UserIp));

    [HttpPost("all")]
    public Task<IActionResult> ResetAll() => Execute(uid => _svc.ResetAllAsync(uid, UserIp));

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private string? UserIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private Guid UserId => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
        out var id) ? id : Guid.Empty;

    private async Task<IActionResult> Execute(Func<Guid, Task<ResetResultDto>> action)
    {
        if (_env.IsProduction())
            return NotFound();

        try
        {
            var result = await action(UserId);
            return Ok(ApiResponse<ResetResultDto>.Ok(result, result.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.Fail(ex.Message));
        }
    }
}
