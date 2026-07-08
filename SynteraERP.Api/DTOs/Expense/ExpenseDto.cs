using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.Expense;

public class ExpenseListDto
{
    public Guid Id { get; set; }
    public string ExpenseNo { get; set; } = string.Empty;
    public DateOnly ExpenseDate { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public string ExpenseCategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ExpenseDto : ExpenseListDto
{
    public Guid? VendorId { get; set; }
    public string Method { get; set; } = string.Empty;
    public Guid CashBankAccountId { get; set; }
    public string CashBankAccountCode { get; set; } = string.Empty;
    public string CashBankAccountName { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public bool HasAttachment { get; set; }
    public string? AttachmentName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ExpenseQueryParams : PaginationParams
{
    public string? Status { get; set; }
    public Guid? ExpenseCategoryId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public class CreateExpenseRequest
{
    public DateOnly ExpenseDate { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public Guid? CashBankAccountId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Remarks { get; set; }
}

public class RejectExpenseRequest
{
    public string? Reason { get; set; }
}
