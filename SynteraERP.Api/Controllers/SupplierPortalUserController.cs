using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

// Admin internal mengelola akun login PIC vendor di bawah halaman Vendor/Supplier yang sudah
// ada — bukan endpoint portal vendor itu sendiri (lihat VendorAuthController/VendorPortalController
// yang pakai scheme "Vendor").
[Authorize]
[ApiController]
[Route("api/suppliers/{supplierId:guid}/portal-users")]
public class SupplierPortalUserController : ControllerBase
{
    private readonly ISupplierPortalUserService _svc;

    public SupplierPortalUserController(ISupplierPortalUserService svc)
    {
        _svc = svc;
    }

    [RequirePermission(Modules.Purchasing, PermissionActions.View)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierPortalUserDto>>>> List(Guid supplierId)
    {
        var result = await _svc.ListBySupplierAsync(supplierId);
        return Ok(ApiResponse<List<SupplierPortalUserDto>>.Ok(result));
    }

    [RequirePermission(Modules.Purchasing, PermissionActions.Create)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierPortalUserDto>>> Create(
        Guid supplierId, [FromBody] CreateSupplierPortalUserRequest request)
    {
        try
        {
            var result = await _svc.CreateAsync(supplierId, request);
            return Ok(ApiResponse<SupplierPortalUserDto>.Ok(result, "Akun portal vendor berhasil dibuat."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SupplierPortalUserDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SupplierPortalUserDto>.Fail(ex.Message));
        }
    }

    [RequirePermission(Modules.Purchasing, PermissionActions.Edit)]
    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse>> Deactivate(Guid supplierId, Guid id)
    {
        var ok = await _svc.SetActiveAsync(id, false);
        if (!ok) return NotFound(ApiResponse.Fail("Akun portal vendor tidak ditemukan."));
        return Ok(ApiResponse.Ok("Akun portal vendor berhasil dinonaktifkan."));
    }

    [RequirePermission(Modules.Purchasing, PermissionActions.Edit)]
    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse>> Activate(Guid supplierId, Guid id)
    {
        var ok = await _svc.SetActiveAsync(id, true);
        if (!ok) return NotFound(ApiResponse.Fail("Akun portal vendor tidak ditemukan."));
        return Ok(ApiResponse.Ok("Akun portal vendor berhasil diaktifkan."));
    }
}
