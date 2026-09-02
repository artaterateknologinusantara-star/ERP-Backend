using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.DemoLead;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/demo-leads")]
public class DemoLeadController : ControllerBase
{
    private readonly IDemoLeadService _svc;

    public DemoLeadController(IDemoLeadService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DemoLeadDto>>>> List()
    {
        var result = await _svc.ListAsync();
        return Ok(ApiResponse<List<DemoLeadDto>>.Ok(result));
    }

    // Anonymous: submitted from the public /demo landing page, before any JWT exists.
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DemoLeadDto>>> Create([FromBody] CreateDemoLeadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.WhatsappNumber) ||
            string.IsNullOrWhiteSpace(request.CompanyEmail) || string.IsNullOrWhiteSpace(request.CompanyName) ||
            string.IsNullOrWhiteSpace(request.Industry) || string.IsNullOrWhiteSpace(request.Need))
        {
            return BadRequest(ApiResponse<DemoLeadDto>.Fail("Mohon lengkapi semua field yang wajib diisi."));
        }

        var item = await _svc.CreateAsync(request);
        return Ok(ApiResponse<DemoLeadDto>.Ok(item, "Permintaan demo berhasil dikirim."));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<DemoLeadDto>>> UpdateStatus(Guid id, [FromBody] UpdateDemoLeadStatusRequest request)
    {
        var item = await _svc.UpdateStatusAsync(id, request);
        if (item is null) return NotFound(ApiResponse<DemoLeadDto>.Fail("Demo lead tidak ditemukan."));
        return Ok(ApiResponse<DemoLeadDto>.Ok(item, "Status berhasil diperbarui."));
    }
}
