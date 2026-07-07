using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Supplier;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISupplierService
{
    Task<PaginatedResponse<SupplierDto>> ListAsync(PaginationParams p);
    Task<SupplierDto?> GetByIdAsync(Guid id);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request);
    Task<SupplierDto?> UpdateAsync(Guid id, UpdateSupplierRequest request);
    Task<bool> SetStatusAsync(Guid id, bool isActive);
    Task<bool> DeleteAsync(Guid id);
}
