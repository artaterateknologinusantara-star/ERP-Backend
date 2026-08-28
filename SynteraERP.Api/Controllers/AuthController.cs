using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Auth;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly AppDbContext _db;

    public AuthController(IAuthService auth, AppDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        if (result is null)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Email atau password salah."));

        return Ok(ApiResponse<LoginResponse>.Ok(result));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> Me()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

        var profile = await _auth.GetProfileAsync(userId);
        if (profile is null)
            return NotFound(ApiResponse<UserProfileDto>.Fail("User tidak ditemukan."));

        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<UserProfileDto>>>> ListUsers()
    {
        var users = await _db.Users
            .Where(u => u.IsActive && !u.IsDeleted)
            .OrderBy(u => u.Name)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.Name,
                IsActive = u.IsActive,
            })
            .ToListAsync();

        return Ok(ApiResponse<List<UserProfileDto>>.Ok(users));
    }
}
