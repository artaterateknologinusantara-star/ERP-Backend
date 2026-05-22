using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Auth;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;

    public AuthService(AppDbContext db, JwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var (token, expiresAt) = _jwt.Generate(user);

        return new LoginResponse
        {
            Token = token,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            ExpiresAt = expiresAt,
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
        };
    }
}
