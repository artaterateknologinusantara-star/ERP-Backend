using Microsoft.AspNetCore.Http;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.CustomerPO;

namespace SynteraERP.Api.Services.Interfaces;

public interface ICustomerPoService
{
    Task<PaginatedResponse<CustomerPoListDto>> ListAsync(PaginationParams p);
    Task<CustomerPoDto?> GetByIdAsync(Guid id);
    Task<CustomerPoDto?> GetByQuotationIdAsync(Guid quotationId);
    Task<CustomerPoDto> CreateAsync(CreateCustomerPoRequest request, IFormFile? attachment);
    Task<bool> DeleteAsync(Guid id);
    Task<(byte[] data, string contentType, string fileName)?> GetAttachmentAsync(Guid id);
    Task<CustomerPoDto> UpdateNumberAsync(Guid customerPoId, string newPoNo, string? reason);
    Task<IEnumerable<CustomerPoHistoryDto>> GetHistoryAsync(Guid customerPoId);
}
