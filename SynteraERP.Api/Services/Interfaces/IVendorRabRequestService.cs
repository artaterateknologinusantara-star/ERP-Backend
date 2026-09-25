using SynteraERP.Api.DTOs.VendorPortal;

namespace SynteraERP.Api.Services.Interfaces;

public interface IVendorRabRequestService
{
    // Internal (maincon) — "buat" dan "kirim" digabung 1 aksi: begitu dibuat, request langsung
    // berstatus Sent dan terlihat oleh vendor (lihat keputusan produk 24 Sep 2026, section 8).
    Task<VendorRabRequestDto> CreateAndSendAsync(Guid quotationGroupId, Guid sentByUserId, CreateVendorRabRequestRequest request);
    Task<List<VendorRabRequestDto>> ListByGroupAsync(Guid quotationGroupId);
    Task<VendorRabRequestDto?> GetByIdAsync(Guid requestId);

    // Vendor-facing — selalu discope ke SupplierId milik token vendor yang login, tidak pernah
    // menerima SupplierId dari input caller.
    Task<List<VendorRabRequestDto>> ListForVendorAsync(Guid supplierId);
    Task<VendorRabRequestDto?> GetForVendorAsync(Guid requestId, Guid supplierId);
}
