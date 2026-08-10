using SynteraERP.Api.DTOs.Branch;
using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.Services.Interfaces;

public interface IBranchService
{
    Task<PaginatedResponse<BranchDto>> ListAsync(PaginationParams p);
    Task<BranchDto?> GetByIdAsync(Guid id);
    Task<BranchDto> CreateAsync(CreateBranchRequest request);
    Task<BranchDto?> UpdateAsync(Guid id, UpdateBranchRequest request);
    Task<bool> DeleteAsync(Guid id);
}
