using SynteraERP.Api.DTOs.Account;

namespace SynteraERP.Api.Services.Interfaces;

public interface IAccountService
{
    Task<List<AccountDto>> GetTreeAsync();
    Task<AccountDto?> GetByIdAsync(Guid id);
    Task<AccountDto> CreateAsync(CreateAccountRequest request);
    Task<AccountDto?> UpdateAsync(Guid id, UpdateAccountRequest request);
}
