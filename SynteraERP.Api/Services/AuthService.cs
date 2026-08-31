using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Auth;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
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
            .Include(u => u.Role).ThenInclude(r => r.Permissions)
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
            Permissions = ToPermissionDtos(user.Role),
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Role).ThenInclude(r => r.Permissions)
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
            Permissions = ToPermissionDtos(user.Role),
        };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        // Same generic message whether or not the account exists — only the ResetToken field
        // reveals a match, and only to whoever is looking at this response (there is no email
        // delivery yet, so the response itself IS the delivery channel; see PasswordResetToken).
        const string message = "Jika email terdaftar, token reset password telah dibuat.";
        if (user is null) return new ForgotPasswordResponse { Message = message };

        // Invalidate any outstanding tokens for this user before issuing a new one.
        var stale = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();
        _db.PasswordResetTokens.RemoveRange(stale);

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = expiresAt,
        });
        await _db.SaveChangesAsync();

        return new ForgotPasswordResponse { Message = message, ResetToken = rawToken, ExpiresAt = expiresAt };
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var tokenHash = HashToken(request.Token);
        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (resetToken is null || resetToken.UsedAt is not null || resetToken.ExpiresAt < DateTimeOffset.UtcNow)
            return false;

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        resetToken.User.UpdatedAt = DateTimeOffset.UtcNow;
        resetToken.UsedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));

    private static List<ModulePermissionDto> ToPermissionDtos(Role? role) =>
        role?.Permissions
            .Select(p => new ModulePermissionDto
            {
                Module = p.Module,
                CanView = p.CanView,
                CanCreate = p.CanCreate,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete,
                CanApprove = p.CanApprove,
            })
            .ToList() ?? [];
}
