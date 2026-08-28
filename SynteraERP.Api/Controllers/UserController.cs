using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Auth;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UserController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserListDto>>>> List()
    {
        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.Name)
            .Select(u => ToDto(u))
            .ToListAsync();

        return Ok(ApiResponse<List<UserListDto>>.Ok(users));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<List<object>>>> ListRoles()
    {
        var roles = await db.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, r.Description })
            .ToListAsync();

        return Ok(ApiResponse<List<object>>.Ok(roles.Cast<object>().ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserListDto>>> Get(Guid id)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(ApiResponse<UserListDto>.Fail("User tidak ditemukan."));
        return Ok(ApiResponse<UserListDto>.Ok(ToDto(user)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserListDto>>> Create([FromBody] CreateUserRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(ApiResponse<UserListDto>.Fail("Email sudah digunakan."));

        if (!await db.Roles.AnyAsync(r => r.Id == req.RoleId))
            return BadRequest(ApiResponse<UserListDto>.Fail("Role tidak ditemukan."));

        var user = new User
        {
            Name         = req.Name,
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            RoleId       = req.RoleId,
            IsActive     = true,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await db.Entry(user).Reference(u => u.Role).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = user.Id },
            ApiResponse<UserListDto>.Ok(ToDto(user), "User berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserListDto>>> Update(Guid id, [FromBody] UpdateUserRequest req)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(ApiResponse<UserListDto>.Fail("User tidak ditemukan."));

        if (await db.Users.AnyAsync(u => u.Email == req.Email && u.Id != id))
            return BadRequest(ApiResponse<UserListDto>.Fail("Email sudah digunakan user lain."));

        if (!await db.Roles.AnyAsync(r => r.Id == req.RoleId))
            return BadRequest(ApiResponse<UserListDto>.Fail("Role tidak ditemukan."));

        user.Name      = req.Name;
        user.Email     = req.Email;
        user.RoleId    = req.RoleId;
        user.IsActive  = req.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(req.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        await db.SaveChangesAsync();
        await db.Entry(user).Reference(u => u.Role).LoadAsync();

        return Ok(ApiResponse<UserListDto>.Ok(ToDto(user), "User berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound(ApiResponse.Fail("User tidak ditemukan."));

        user.IsDeleted  = true;
        user.IsActive   = false;
        user.UpdatedAt  = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("User berhasil dihapus."));
    }

    private static UserListDto ToDto(User u) => new()
    {
        Id          = u.Id,
        Name        = u.Name,
        Email       = u.Email,
        RoleName    = u.Role?.Name ?? string.Empty,
        RoleId      = u.RoleId,
        IsActive    = u.IsActive,
        LastLoginAt = u.LastLoginAt,
        CreatedAt   = u.CreatedAt,
    };
}
