using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Branch;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class BranchService : IBranchService
{
    private readonly AppDbContext _db;

    public BranchService(AppDbContext db) => _db = db;

    public async Task<PaginatedResponse<BranchDto>> ListAsync(PaginationParams p)
    {
        var q = _db.Branches.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s)
                           || x.Code.ToLower().Contains(s)
                           || (x.Manager != null && x.Manager.ToLower().Contains(s)));
        }

        q = p.SortBy switch
        {
            "code"      => p.IsDescending ? q.OrderByDescending(x => x.Code) : q.OrderBy(x => x.Code),
            "createdAt" => p.IsDescending ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
            _           => p.IsDescending ? q.OrderByDescending(x => x.Name) : q.OrderBy(x => x.Name),
        };

        var total = await q.CountAsync();
        var data  = await q.Skip(p.Skip).Take(p.PerPage).Select(x => ToDto(x)).ToListAsync();
        return PaginatedResponse<BranchDto>.Create(data, total, p.Page, p.PerPage);
    }

    public async Task<BranchDto?> GetByIdAsync(Guid id)
    {
        var x = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        return x is null ? null : ToDto(x);
    }

    public Task<BranchDto> CreateAsync(CreateBranchRequest req) =>
        SequentialCodeHelper.RunWithRetryAsync(_db, async () =>
        {
            var branch = new Branch
            {
                Code     = await GenerateCodeAsync(),
                Name     = req.Name,
                Address  = req.Address,
                Phone    = req.Phone,
                Manager  = req.Manager,
                IsActive = true,
            };
            _db.Branches.Add(branch);
            await _db.SaveChangesAsync();
            return ToDto(branch);
        });

    public async Task<BranchDto?> UpdateAsync(Guid id, UpdateBranchRequest req)
    {
        var branch = await _db.Branches.FindAsync(id);
        if (branch is null) return null;

        branch.Name      = req.Name;
        branch.Address   = req.Address;
        branch.Phone     = req.Phone;
        branch.Manager   = req.Manager;
        branch.IsActive  = req.IsActive;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(branch);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var branch = await _db.Branches.FindAsync(id);
        if (branch is null) return false;
        branch.IsDeleted = true;
        branch.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private Task<string> GenerateCodeAsync() =>
        SequentialCodeHelper.NextCodeAsync(_db.Branches, "BR", 4);

    private static BranchDto ToDto(Branch x) => new()
    {
        Id        = x.Id,
        Code      = x.Code,
        Name      = x.Name,
        Address   = x.Address,
        Phone     = x.Phone,
        Manager   = x.Manager,
        IsActive  = x.IsActive,
        CreatedAt = x.CreatedAt,
    };
}
