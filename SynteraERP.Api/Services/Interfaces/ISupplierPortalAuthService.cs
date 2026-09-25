using SynteraERP.Api.DTOs.VendorPortal;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISupplierPortalAuthService
{
    Task<VendorLoginResponse?> LoginAsync(VendorLoginRequest request);
}
