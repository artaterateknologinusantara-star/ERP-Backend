using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Expense;

namespace SynteraERP.Api.Services.Interfaces;

public interface IExpenseService
{
    Task<PaginatedResponse<ExpenseListDto>> ListAsync(ExpenseQueryParams p);
    Task<ExpenseDto?> GetByIdAsync(Guid id);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, Microsoft.AspNetCore.Http.IFormFile? attachment);
    Task<ExpenseDto?> SubmitAsync(Guid id);
    Task<ExpenseDto?> ApproveAsync(Guid id, Guid? userId);
    Task<ExpenseDto?> RejectAsync(Guid id, string? reason);
    Task<(byte[] data, string contentType, string fileName)?> GetAttachmentAsync(Guid id);
}
