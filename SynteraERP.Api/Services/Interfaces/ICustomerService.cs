using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Customer;

namespace SynteraERP.Api.Services.Interfaces;

public interface ICustomerService
{
    Task<PaginatedResponse<CustomerDto>> ListAsync(CustomerParams p);
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);
    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request);
    Task<bool> SetStatusAsync(Guid id, bool isActive);
    Task<bool> DeleteAsync(Guid id);
}
