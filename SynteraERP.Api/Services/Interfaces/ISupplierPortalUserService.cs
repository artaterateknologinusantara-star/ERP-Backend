using SynteraERP.Api.DTOs.VendorPortal;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISupplierPortalUserService
{
    Task<List<SupplierPortalUserDto>> ListBySupplierAsync(Guid supplierId);
    Task<SupplierPortalUserDto> CreateAsync(Guid supplierId, CreateSupplierPortalUserRequest request);
    Task<bool> SetActiveAsync(Guid id, bool isActive);
}
