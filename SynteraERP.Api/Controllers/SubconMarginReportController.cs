using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Reports;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/reports/subcon-margin")]
public class SubconMarginReportController : ControllerBase
{
    private readonly ISubconMarginReportService _svc;

    public SubconMarginReportController(ISubconMarginReportService svc)
    {
        _svc = svc;
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet("by-vendor")]
    public async Task<ActionResult<ApiResponse<SubconMarginByVendorReportDto>>> ByVendor(
        [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, [FromQuery] Guid? supplierId)
    {
        var result = await _svc.GetByVendorAsync(startDate, endDate, supplierId);
        return Ok(ApiResponse<SubconMarginByVendorReportDto>.Ok(result));
    }

    [RequirePermission(Modules.Sales, PermissionActions.View)]
    [HttpGet("by-quotation")]
    public async Task<ActionResult<ApiResponse<SubconMarginByQuotationReportDto>>> ByQuotation(
        [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, [FromQuery] Guid? supplierId)
    {
        var result = await _svc.GetByQuotationAsync(startDate, endDate, supplierId);
        return Ok(ApiResponse<SubconMarginByQuotationReportDto>.Ok(result));
    }
}
