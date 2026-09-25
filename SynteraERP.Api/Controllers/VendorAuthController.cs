using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.VendorPortal;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

// Sengaja terpisah total dari AuthController (internal) — tidak reuse AuthService/JwtHelper/
// tabel User sama sekali (lihat SupplierPortalAuthService/VendorJwtHelper), supaya tidak ada
// satu jalur kode pun yang bisa mencampur klaim internal dan vendor.
[ApiController]
[Route("api/vendor/auth")]
public class VendorAuthController : ControllerBase
{
    private readonly ISupplierPortalAuthService _auth;

    public VendorAuthController(ISupplierPortalAuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<VendorLoginResponse>>> Login([FromBody] VendorLoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        if (result is null)
            return Unauthorized(ApiResponse<VendorLoginResponse>.Fail("Email atau password salah."));

        return Ok(ApiResponse<VendorLoginResponse>.Ok(result));
    }
}
