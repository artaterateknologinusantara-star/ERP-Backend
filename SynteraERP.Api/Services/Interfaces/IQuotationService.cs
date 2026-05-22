using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Quotation;

namespace SynteraERP.Api.Services.Interfaces;

public interface IQuotationService
{
    Task<PaginatedResponse<QuotationListDto>> ListAsync(PaginationParams p);
    Task<QuotationDto?> GetByIdAsync(Guid id);
    Task<QuotationDto> CreateAsync(SaveQuotationRequest request);
    Task<QuotationDto?> UpdateAsync(Guid id, SaveQuotationRequest request);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<QuotationDto> DuplicateAsync(Guid id);
    Task<SendQuotationResultDto?> SendAsync(Guid id, Guid sentByUserId);
    Task<QuotationDto?> CreateRevisionAsync(Guid id);
    Task<bool> ApproveAsync(Guid id, Guid approvedByUserId);
    Task<bool> RejectAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
