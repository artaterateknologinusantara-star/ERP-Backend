using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Auth;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Controllers;

[Authorize(Roles = "Administrator")]
[ApiController]
[Route("api/roles")]
public class RoleController(AppDbContext db) : ControllerBase
{
    private const string ProtectedRoleName = "Administrator";

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleListDto>>>> List()
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.Users)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return Ok(ApiResponse<List<RoleListDto>>.Ok(roles.Select(ToDto).ToList()));
    }

    [HttpGet("modules")]
    public ActionResult<ApiResponse<List<string>>> ListModules() =>
        Ok(ApiResponse<List<string>>.Ok(Modules.All.ToList()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleListDto>>> Get(Guid id)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role is null) return NotFound(ApiResponse<RoleListDto>.Fail("Role tidak ditemukan."));
        return Ok(ApiResponse<RoleListDto>.Ok(ToDto(role)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleListDto>>> Create([FromBody] CreateRoleRequest request)
    {
        if (await db.Roles.AnyAsync(r => r.Name == request.Name))
            return BadRequest(ApiResponse<RoleListDto>.Fail("Nama role sudah digunakan."));

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = role.Id },
            ApiResponse<RoleListDto>.Ok(ToDto(role), "Role berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleListDto>>> Update(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var role = await db.Roles.Include(r => r.Permissions).Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound(ApiResponse<RoleListDto>.Fail("Role tidak ditemukan."));

        if (role.Name == ProtectedRoleName && (request.Name != ProtectedRoleName || !request.IsActive))
            return BadRequest(ApiResponse<RoleListDto>.Fail("Role Administrator tidak dapat diubah nama atau dinonaktifkan."));

        if (await db.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
            return BadRequest(ApiResponse<RoleListDto>.Fail("Nama role sudah digunakan."));

        role.Name = request.Name;
        role.Description = request.Description;
        role.IsActive = request.IsActive;
        role.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<RoleListDto>.Ok(ToDto(role), "Role berhasil diperbarui."));
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<ActionResult<ApiResponse<RoleListDto>>> UpdatePermissions(Guid id, [FromBody] UpdatePermissionsRequest request)
    {
        var role = await db.Roles.Include(r => r.Permissions).Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound(ApiResponse<RoleListDto>.Fail("Role tidak ditemukan."));

        if (role.Name == ProtectedRoleName)
            return BadRequest(ApiResponse<RoleListDto>.Fail("Akses Role Administrator selalu penuh dan tidak dapat diubah."));

        var invalidModules = request.Permissions.Select(p => p.Module).Except(Modules.All).ToList();
        if (invalidModules.Count > 0)
            return BadRequest(ApiResponse<RoleListDto>.Fail($"Modul tidak dikenali: {string.Join(", ", invalidModules)}"));

        foreach (var incoming in request.Permissions)
        {
            var existing = role.Permissions.FirstOrDefault(p => p.Module == incoming.Module);
            if (existing is null)
            {
                role.Permissions.Add(new Permission
                {
                    RoleId = role.Id,
                    Module = incoming.Module,
                    CanView = incoming.CanView,
                    CanCreate = incoming.CanCreate,
                    CanEdit = incoming.CanEdit,
                    CanDelete = incoming.CanDelete,
                    CanApprove = incoming.CanApprove,
                });
            }
            else
            {
                existing.CanView = incoming.CanView;
                existing.CanCreate = incoming.CanCreate;
                existing.CanEdit = incoming.CanEdit;
                existing.CanDelete = incoming.CanDelete;
                existing.CanApprove = incoming.CanApprove;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        // Modules omitted from the payload lose all access on this role (the matrix UI always
        // submits the full set of modules, so an omission means the user explicitly cleared it).
        var submittedModules = request.Permissions.Select(p => p.Module).ToHashSet();
        foreach (var stale in role.Permissions.Where(p => !submittedModules.Contains(p.Module)).ToList())
            db.Permissions.Remove(stale);

        await db.SaveChangesAsync();
        return Ok(ApiResponse<RoleListDto>.Ok(ToDto(role), "Akses modul berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var role = await db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound(ApiResponse.Fail("Role tidak ditemukan."));

        if (role.Name == ProtectedRoleName)
            return BadRequest(ApiResponse.Fail("Role Administrator tidak dapat dihapus."));

        if (role.Users.Any(u => !u.IsDeleted))
            return BadRequest(ApiResponse.Fail("Role masih digunakan oleh user aktif dan tidak dapat dihapus."));

        role.IsDeleted = true;
        role.IsActive = false;
        role.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Role berhasil dihapus."));
    }

    private static RoleListDto ToDto(Role r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        IsActive = r.IsActive,
        UserCount = r.Users.Count(u => !u.IsDeleted),
        Permissions = r.Permissions
            .OrderBy(p => p.Module)
            .Select(p => new ModulePermissionDto
            {
                Module = p.Module,
                CanView = p.CanView,
                CanCreate = p.CanCreate,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete,
                CanApprove = p.CanApprove,
            })
            .ToList(),
    };
}
