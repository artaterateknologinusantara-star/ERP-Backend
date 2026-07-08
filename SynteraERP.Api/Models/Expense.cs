using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Expense : BaseEntity
{
    public string ExpenseNo { get; set; } = string.Empty;
    public DateOnly ExpenseDate { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public decimal Amount { get; set; } = 0;
    public string Method { get; set; } = string.Empty;
    public Guid CashBankAccountId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? Remarks { get; set; }

    public ExpenseCategory ExpenseCategory { get; set; } = null!;
    public Supplier? Vendor { get; set; }
    public Account CashBankAccount { get; set; } = null!;
}

public enum ExpenseStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Paid,
}
