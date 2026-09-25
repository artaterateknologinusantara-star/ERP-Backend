using SynteraERP.Api.DTOs.VendorPortal;

namespace SynteraERP.Api.Services.Interfaces;

public interface IVendorRabSubmissionService
{
    // Vendor-facing — requestId + supplierId (dari claim token vendor, bukan input caller).
    Task<VendorRabSubmissionDto> CreateAsync(
        Guid requestId, Guid supplierId, Guid portalUserId, CreateVendorRabSubmissionRequest request);

    // Internal-facing
    Task<VendorRabSubmissionDto?> GetByIdAsync(Guid submissionId);
    Task<bool> SetLineMarkupAsync(Guid submissionId, Guid lineId, decimal markupAmount);
    Task<Guid> ApproveAsync(Guid submissionId, Guid approvedByUserId);
    Task<bool> RejectAsync(Guid submissionId, Guid rejectedByUserId, string? reason);
}
