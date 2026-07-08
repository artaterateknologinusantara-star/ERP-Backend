using SynteraERP.Api.DTOs.Expense;

namespace SynteraERP.Api.Services.Interfaces;

public interface IExpenseCategoryService
{
    Task<List<ExpenseCategoryDto>> ListAsync(bool? isActive = null);
    Task<ExpenseCategoryDto?> GetByIdAsync(Guid id);
    Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequest request);
    Task<ExpenseCategoryDto?> UpdateAsync(Guid id, UpdateExpenseCategoryRequest request);
}
