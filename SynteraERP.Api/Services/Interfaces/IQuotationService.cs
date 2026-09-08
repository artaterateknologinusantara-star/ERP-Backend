using Microsoft.AspNetCore.Http;
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
    Task<QuotationWorkItemDto> CreateWorkItemAsync(Guid groupId, SaveWorkItemRequest request);
    Task<bool> UpdateWorkItemAsync(Guid id, SaveWorkItemRequest request);
    Task<bool> DeleteWorkItemAsync(Guid id);
    Task<QuotationWorkDetailDto> CreateWorkDetailAsync(Guid workItemId, SaveWorkDetailRequest request);
    Task<bool> UpdateWorkDetailAsync(Guid id, SaveWorkDetailRequest request);
    Task<bool> DeleteWorkDetailAsync(Guid id);
    Task<QuotationWorkDetailAttachmentDto> UploadWorkDetailAttachmentAsync(Guid workDetailId, IFormFile file);
    Task<bool> DeleteWorkDetailAttachmentAsync(Guid attachmentId);
    Task<(byte[] data, string contentType, string fileName)?> GetWorkDetailAttachmentAsync(Guid attachmentId);
}
