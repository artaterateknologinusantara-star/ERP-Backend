using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.SystemReset;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class SystemResetService : ISystemResetService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SystemResetService> _log;

    public SystemResetService(AppDbContext db, ILogger<SystemResetService> log)
    {
        _db = db;
        _log = log;
    }

    // ─── Quotations ───────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetQuotationsAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();
        await using var tx = await _db.Database.BeginTransactionAsync();

        counts["CustomerPOs"] = await _db.CustomerPOs.IgnoreQueryFilters().ExecuteDeleteAsync();
        // QuotationTabs → Groups → Items cascade from Quotation
        counts["Quotations"] = await _db.Quotations.IgnoreQueryFilters().ExecuteDeleteAsync();

        var result = await WriteAuditAndReturn("RESET_QUOTATIONS", "Quotations & Customer POs", counts, userId, userIp);
        await tx.CommitAsync();
        return result;
    }

    // ─── Sales ────────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetSalesAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();
        await using var tx = await _db.Database.BeginTransactionAsync();

        // DownPaymentApplication → Invoice (Restrict) AND → SalesOrderPayment (Restrict) — must go
        // before both Invoices and SalesOrderPayments below, or their deletes fail with an FK
        // violation the moment a DP has ever been applied to an invoice on this Sales Order.
        counts["DownPaymentApplications"] = await _db.DownPaymentApplications.ExecuteDeleteAsync();
        // RetentionRelease → Invoice (Restrict) — must go before Invoices below, or the delete fails
        // the moment retention has ever been released against one of these invoices.
        counts["RetentionReleases"] = await _db.RetentionReleases.IgnoreQueryFilters().ExecuteDeleteAsync();

        // Payments + InvoiceItems cascade from Invoice
        counts["Invoices"] = await _db.Invoices.IgnoreQueryFilters().ExecuteDeleteAsync();

        // SalesOrderPayment → SalesOrder (Restrict) — must go before SalesOrders below.
        counts["SalesOrderPayments"] = await _db.SalesOrderPayments.ExecuteDeleteAsync();
        // SalesOrderItems cascade from SalesOrder; PurchaseRequest.SalesOrderId → null; DO.SalesOrderId → null
        counts["SalesOrders"] = await _db.SalesOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        var result = await WriteAuditAndReturn("RESET_SALES", "Sales Orders, Invoices & Accounts Receivable", counts, userId, userIp);
        await tx.CommitAsync();
        return result;
    }

    // ─── Purchasing ───────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetPurchasingAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();
        await using var tx = await _db.Database.BeginTransactionAsync();

        // SupplierInvoice → PurchaseOrder (Restrict); its cascaded SupplierInvoiceItem →
        // PurchaseOrderItem (Restrict) and SupplierInvoicePayment → POPayment (Restrict) would
        // otherwise block PurchaseOrder's own cascade delete of its Items/Payments below.
        counts["SupplierInvoices"] = await _db.SupplierInvoices.IgnoreQueryFilters().ExecuteDeleteAsync();

        // POPayments + PurchaseOrderItems cascade from PurchaseOrder
        counts["PurchaseOrders"] = await _db.PurchaseOrders.IgnoreQueryFilters().ExecuteDeleteAsync();
        // PurchaseRequestItems cascade from PurchaseRequest
        counts["PurchaseRequests"] = await _db.PurchaseRequests.IgnoreQueryFilters().ExecuteDeleteAsync();

        var result = await WriteAuditAndReturn("RESET_PURCHASING", "Purchase Requests, Purchase Orders & Accounts Payable", counts, userId, userIp);
        await tx.CommitAsync();
        return result;
    }

    // ─── Finance ─────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetFinanceAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();
        await using var tx = await _db.Database.BeginTransactionAsync();

        // DownPaymentApplication + RetentionRelease → Invoice (Restrict) — must go before Invoices.
        counts["DownPaymentApplications"] = await _db.DownPaymentApplications.ExecuteDeleteAsync();
        counts["RetentionReleases"] = await _db.RetentionReleases.IgnoreQueryFilters().ExecuteDeleteAsync();

        // BankStatementLine.MatchedJournalEntryLineId → JournalEntryLine (Restrict) — not relevant
        // here since JournalEntry is untouched by this scope, but bank reconciliation ledger is
        // Finance data like Invoices/POPayments, so it resets together with the rest of the ledger.
        counts["BankStatementImports"] = await _db.BankStatementImports.IgnoreQueryFilters().ExecuteDeleteAsync();

        // Payments + InvoiceItems cascade from Invoice (AR)
        counts["Invoices"] = await _db.Invoices.IgnoreQueryFilters().ExecuteDeleteAsync();

        // SupplierInvoice → PurchaseOrder (Restrict) is irrelevant here (POs are preserved), but its
        // cascaded SupplierInvoicePayment → POPayment (Restrict) would block the standalone POPayment
        // delete below the moment a supplier invoice has ever been marked paid — must go first.
        counts["SupplierInvoices"] = await _db.SupplierInvoices.IgnoreQueryFilters().ExecuteDeleteAsync();
        // AP payments (AP — standalone deletion since POs are preserved)
        counts["POPayments"] = await _db.POPayments.ExecuteDeleteAsync();

        var result = await WriteAuditAndReturn("RESET_FINANCE", "Accounts Receivable & Accounts Payable ledger", counts, userId, userIp);
        await tx.CommitAsync();
        return result;
    }

    // ─── Projects ─────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetProjectsAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();
        await using var tx = await _db.Database.BeginTransactionAsync();

        // ProjectRevenueRecognition → Project (Restrict) — must go before Projects below.
        counts["ProjectRevenueRecognitions"] = await _db.ProjectRevenueRecognitions.ExecuteDeleteAsync();
        // ProjectTasks cascade from Project
        counts["Projects"] = await _db.Projects.IgnoreQueryFilters().ExecuteDeleteAsync();

        var result = await WriteAuditAndReturn("RESET_PROJECTS", "Projects, Tasks & Revenue Recognition", counts, userId, userIp);
        await tx.CommitAsync();
        return result;
    }

    // ─── Inventory ────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetInventoryAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();
        await using var tx = await _db.Database.BeginTransactionAsync();

        counts["StockTransactions"] = await _db.StockTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        // DeliveryOrderItems cascade from DeliveryOrder
        counts["DeliveryOrders"] = await _db.DeliveryOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        // Reset all item stock/cost fields to zero since the transactions/prices that produced
        // them are gone — leaving CurrentAverageCost/LastPurchasePrice stale after StockTransactions
        // are wiped would make Moving Average Cost postings wrong for the next PO received.
        await _db.ItemMasters.ExecuteUpdateAsync(s => s
            .SetProperty(i => i.Stock, 0m)
            .SetProperty(i => i.CurrentAverageCost, 0m)
            .SetProperty(i => i.LastPurchasePrice, (decimal?)0m));

        var result = await WriteAuditAndReturn("RESET_INVENTORY", "Stock Transactions & Delivery Orders", counts, userId, userIp);
        await tx.CommitAsync();
        return result;
    }

    // ─── Reset All ────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetAllAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();
        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1. DownPaymentApplication → SalesOrderPayment (Restrict) AND → Invoice (Restrict) — delete first.
        counts["DownPaymentApplications"] = await _db.DownPaymentApplications.ExecuteDeleteAsync();

        // 2. RetentionRelease → Invoice (Restrict) — delete before Invoices.
        counts["RetentionReleases"] = await _db.RetentionReleases.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 3. CustomerPO → Quotation (Restrict) — delete before Quotations.
        counts["CustomerPOs"] = await _db.CustomerPOs.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 4. SupplierInvoice → PurchaseOrder (Restrict); cascaded SupplierInvoiceItem →
        //    PurchaseOrderItem (Restrict) and SupplierInvoicePayment → POPayment (Restrict) —
        //    delete before PurchaseOrders.
        counts["SupplierInvoices"] = await _db.SupplierInvoices.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 5. ProjectRevenueRecognition → Project (Restrict) — delete before Projects.
        counts["ProjectRevenueRecognitions"] = await _db.ProjectRevenueRecognitions.ExecuteDeleteAsync();

        // 6. BankStatementImport (cascades BankStatementLine) — must go before JournalEntry below:
        //    BankStatementLine.MatchedJournalEntryLineId → JournalEntryLine (Restrict) would block
        //    JournalEntry's cascade delete of its Lines otherwise.
        counts["BankStatementImports"] = await _db.BankStatementImports.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 7. SalesOrderPayment → SalesOrder (Restrict) — delete before SalesOrders.
        counts["SalesOrderPayments"] = await _db.SalesOrderPayments.ExecuteDeleteAsync();

        // 8. Finance: Payments + InvoiceItems cascade from Invoice
        counts["Invoices"] = await _db.Invoices.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 9. Sales: SalesOrderItems cascade from SalesOrder
        counts["SalesOrders"] = await _db.SalesOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 10. Purchasing: POPayments + POItems cascade from PO
        counts["PurchaseOrders"] = await _db.PurchaseOrders.IgnoreQueryFilters().ExecuteDeleteAsync();
        // 11. PRItems cascade from PR
        counts["PurchaseRequests"] = await _db.PurchaseRequests.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 12. Quotations: Tabs → Groups → Items cascade
        counts["Quotations"] = await _db.Quotations.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 13. Expense (Operational cash-out ledger)
        counts["Expenses"] = await _db.Expenses.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 14-15. Inventory
        counts["StockTransactions"] = await _db.StockTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        counts["DeliveryOrders"] = await _db.DeliveryOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 16. Projects: Tasks cascade
        counts["Projects"] = await _db.Projects.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 17. JournalEntry: Lines cascade — safe now that BankStatementImport (step 6) already
        //     removed every BankStatementLine that could Restrict-block a JournalEntryLine delete.
        counts["JournalEntries"] = await _db.JournalEntries.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 18. Reset stock quantities and moving-average cost — the transactions/purchases that
        //     produced them no longer exist after steps 4/10/14 above.
        await _db.ItemMasters.ExecuteUpdateAsync(s => s
            .SetProperty(i => i.Stock, 0m)
            .SetProperty(i => i.CurrentAverageCost, 0m)
            .SetProperty(i => i.LastPurchasePrice, (decimal?)0m));

        // 19. Document numbering counters
        await _db.NumberingConfigs.ExecuteUpdateAsync(s => s.SetProperty(n => n.LastNumber, 0));

        var result = await WriteAuditAndReturn("RESET_ALL", "All transaction data across every module", counts, userId, userIp);
        await tx.CommitAsync();
        return result;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ResetResultDto> WriteAuditAndReturn(
        string action, string scope,
        Dictionary<string, int> counts,
        Guid userId, string? userIp)
    {
        var userName = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync() ?? "Unknown";

        var entry = new AuditLog
        {
            Action = action,
            Scope = scope,
            TotalDeleted = counts.Values.Sum(),
            Details = JsonSerializer.Serialize(counts),
            PerformedBy = userId,
            PerformedByName = userName,
            IpAddress = userIp,
        };
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();

        _log.LogWarning(
            "[AUDIT] {Action} by {User} ({UserId}) from {Ip} — {Total} records deleted. Details: {Details}",
            action, userName, userId, userIp ?? "unknown", entry.TotalDeleted, entry.Details);

        var message = $"{action.Replace('_', ' ')} berhasil. {entry.TotalDeleted} record dihapus.";
        return new ResetResultDto(true, message, counts, entry.Id);
    }
}
