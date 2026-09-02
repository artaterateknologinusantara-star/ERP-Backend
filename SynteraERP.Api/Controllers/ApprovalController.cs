using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Approval;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Controllers;

// Aggregates every "awaiting approval" item across modules into one list, scoped to what the
// caller's role actually has Approve permission for — the same permission rows that already gate
// the underlying /approve endpoints (ExpenseController, QuotationController, SupplierInvoiceController,
// PurchaseRequestController), so this list can never show more than the caller is allowed to act on.
[Authorize]
[ApiController]
[Route("api/approvals")]
public class ApprovalController(AppDbContext db) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<PendingApprovalDto>>>> GetPending()
    {
        var roleIdStr = User.FindFirst("roleId")?.Value;
        if (roleIdStr is null || !Guid.TryParse(roleIdStr, out var roleId))
            return Ok(ApiResponse<List<PendingApprovalDto>>.Ok([]));

        var approvableModules = await db.Permissions
            .AsNoTracking()
            .Where(p => p.RoleId == roleId && p.CanApprove)
            .Select(p => p.Module)
            .ToListAsync();

        var results = new List<PendingApprovalDto>();

        if (approvableModules.Contains(Modules.Sales))
        {
            var quotations = await db.Quotations
                .AsNoTracking()
                .Include(q => q.Customer)
                .Include(q => q.Sales)
                .Where(q => !q.IsDeleted && q.Status == QuotationStatus.Terkirim)
                .OrderByDescending(q => q.SentAt)
                .Select(q => new PendingApprovalDto
                {
                    Id = q.Id,
                    Module = Modules.Sales,
                    Type = "Quotation",
                    TypeLabel = "Penawaran",
                    No = q.No,
                    Title = $"{q.Customer.Name} — {q.ProjectName}",
                    Amount = q.GrandTotal,
                    Date = q.Date,
                    RequestedByName = q.Sales.Name,
                })
                .ToListAsync();
            results.AddRange(quotations);
        }

        if (approvableModules.Contains(Modules.Finance))
        {
            var expenses = await db.Expenses
                .AsNoTracking()
                .Include(e => e.ExpenseCategory)
                .Where(e => !e.IsDeleted && e.Status == ExpenseStatus.Submitted)
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new PendingApprovalDto
                {
                    Id = e.Id,
                    Module = Modules.Finance,
                    Type = "Expense",
                    TypeLabel = "Pengeluaran",
                    No = e.ExpenseNo,
                    Title = $"{e.ExpenseCategory.Name} — {e.Description}",
                    Amount = e.Amount,
                    Date = e.ExpenseDate,
                    DetailUrl = $"/expense/{e.Id}",
                })
                .ToListAsync();
            results.AddRange(expenses);
        }

        if (approvableModules.Contains(Modules.Purchasing))
        {
            var purchaseRequests = await db.PurchaseRequests
                .AsNoTracking()
                .Include(p => p.RequestedByUser)
                .Where(p => !p.IsDeleted && p.Status == PurchaseRequestStatus.Submitted)
                .OrderByDescending(p => p.Date)
                .Select(p => new PendingApprovalDto
                {
                    Id = p.Id,
                    Module = Modules.Purchasing,
                    Type = "PurchaseRequest",
                    TypeLabel = "Purchase Request",
                    No = p.No,
                    Title = p.Notes ?? p.No,
                    Amount = p.Total,
                    Date = p.Date,
                    RequestedByName = p.RequestedByUser != null ? p.RequestedByUser.Name : string.Empty,
                    DetailUrl = $"/purchase-request/{p.Id}",
                })
                .ToListAsync();
            results.AddRange(purchaseRequests);

            var supplierInvoices = await db.SupplierInvoices
                .AsNoTracking()
                .Include(s => s.Supplier)
                .Where(s => !s.IsDeleted && s.Status == SupplierInvoiceStatus.Draft)
                .OrderByDescending(s => s.InvoiceDate)
                .Select(s => new PendingApprovalDto
                {
                    Id = s.Id,
                    Module = Modules.Purchasing,
                    Type = "SupplierInvoice",
                    TypeLabel = "Supplier Invoice",
                    No = s.InvoiceNumber,
                    Title = s.Supplier.Name,
                    Amount = s.Total,
                    Date = s.InvoiceDate,
                })
                .ToListAsync();
            results.AddRange(supplierInvoices);
        }

        if (approvableModules.Contains(Modules.Accounting))
        {
            // Amount dan CreatedByName dihitung dalam proyeksi (Sum/subquery correlated), bukan
            // dengan .Include(j => j.Lines) lalu di-materialize dulu — pola sama seperti
            // JournalPostingService.ListAsync, menghindari N+1 dan load baris yang tidak perlu.
            var journalEntries = await db.JournalEntries
                .AsNoTracking()
                .Where(j => !j.IsDeleted && j.Status == JournalEntryStatus.Draft)
                .OrderByDescending(j => j.Date)
                .Select(j => new PendingApprovalDto
                {
                    Id = j.Id,
                    Module = Modules.Accounting,
                    Type = "JournalEntry",
                    TypeLabel = "Jurnal Manual",
                    No = j.EntryNumber,
                    Title = j.Description,
                    Amount = j.Lines.Sum(l => l.Debit),
                    Date = DateOnly.FromDateTime(j.Date.Date),
                    RequestedByName = db.Users.Where(u => u.Id == j.CreatedBy).Select(u => u.Name).FirstOrDefault() ?? string.Empty,
                    DetailUrl = $"/journal-entry/{j.Id}",
                })
                .ToListAsync();
            results.AddRange(journalEntries);
        }

        return Ok(ApiResponse<List<PendingApprovalDto>>.Ok(results.OrderByDescending(x => x.Date).ToList()));
    }
}
