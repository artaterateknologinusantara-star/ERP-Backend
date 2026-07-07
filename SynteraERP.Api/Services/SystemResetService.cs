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

        counts["CustomerPOs"] = await _db.CustomerPOs.IgnoreQueryFilters().ExecuteDeleteAsync();
        // QuotationTabs → Groups → Items cascade from Quotation
        counts["Quotations"] = await _db.Quotations.IgnoreQueryFilters().ExecuteDeleteAsync();

        return await WriteAuditAndReturn("RESET_QUOTATIONS", "Quotations & Customer POs", counts, userId, userIp);
    }

    // ─── Sales ────────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetSalesAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();

        // Payments + InvoiceItems cascade from Invoice
        counts["Invoices"] = await _db.Invoices.IgnoreQueryFilters().ExecuteDeleteAsync();
        // SalesOrderItems cascade from SalesOrder; PurchaseRequest.SalesOrderId → null; DO.SalesOrderId → null
        counts["SalesOrders"] = await _db.SalesOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        return await WriteAuditAndReturn("RESET_SALES", "Sales Orders, Invoices & Accounts Receivable", counts, userId, userIp);
    }

    // ─── Purchasing ───────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetPurchasingAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();

        // POPayments + PurchaseOrderItems cascade from PurchaseOrder
        counts["PurchaseOrders"] = await _db.PurchaseOrders.IgnoreQueryFilters().ExecuteDeleteAsync();
        // PurchaseRequestItems cascade from PurchaseRequest
        counts["PurchaseRequests"] = await _db.PurchaseRequests.IgnoreQueryFilters().ExecuteDeleteAsync();

        return await WriteAuditAndReturn("RESET_PURCHASING", "Purchase Requests, Purchase Orders & Accounts Payable", counts, userId, userIp);
    }

    // ─── Finance ─────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetFinanceAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();

        // Payments + InvoiceItems cascade from Invoice (AR)
        counts["Invoices"] = await _db.Invoices.IgnoreQueryFilters().ExecuteDeleteAsync();
        // AP payments (AP — standalone deletion since POs are preserved)
        counts["POPayments"] = await _db.POPayments.ExecuteDeleteAsync();

        return await WriteAuditAndReturn("RESET_FINANCE", "Accounts Receivable & Accounts Payable ledger", counts, userId, userIp);
    }

    // ─── Projects ─────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetProjectsAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();

        // ProjectTasks cascade from Project
        counts["Projects"] = await _db.Projects.IgnoreQueryFilters().ExecuteDeleteAsync();

        return await WriteAuditAndReturn("RESET_PROJECTS", "Projects & Tasks", counts, userId, userIp);
    }

    // ─── Inventory ────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetInventoryAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();

        counts["StockTransactions"] = await _db.StockTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        // DeliveryOrderItems cascade from DeliveryOrder
        counts["DeliveryOrders"] = await _db.DeliveryOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        // Reset all item stock quantities to zero since transactions are gone
        await _db.ItemMasters.ExecuteUpdateAsync(s =>
            s.SetProperty(i => i.Stock, 0m));

        return await WriteAuditAndReturn("RESET_INVENTORY", "Stock Transactions & Delivery Orders", counts, userId, userIp);
    }

    // ─── Reset All ────────────────────────────────────────────────────────────

    public async Task<ResetResultDto> ResetAllAsync(Guid userId, string? userIp)
    {
        var counts = new Dictionary<string, int>();

        // 1. CustomerPOs reference Quotations (Restrict) — delete first
        counts["CustomerPOs"] = await _db.CustomerPOs.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 2. Finance: Payments + InvoiceItems cascade from Invoice
        counts["Invoices"] = await _db.Invoices.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 3. Sales: SalesOrderItems cascade from SalesOrder
        counts["SalesOrders"] = await _db.SalesOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 4. Purchasing: POPayments + POItems cascade from PO; PRItems cascade from PR
        counts["PurchaseOrders"] = await _db.PurchaseOrders.IgnoreQueryFilters().ExecuteDeleteAsync();
        counts["PurchaseRequests"] = await _db.PurchaseRequests.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 5. Quotations: Tabs → Groups → Items cascade
        counts["Quotations"] = await _db.Quotations.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 6. Inventory
        counts["StockTransactions"] = await _db.StockTransactions.IgnoreQueryFilters().ExecuteDeleteAsync();
        counts["DeliveryOrders"] = await _db.DeliveryOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 7. Projects: Tasks cascade
        counts["Projects"] = await _db.Projects.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 8. Reset stock to zero and document numbering counters
        await _db.ItemMasters.ExecuteUpdateAsync(s => s.SetProperty(i => i.Stock, 0m));
        await _db.NumberingConfigs.ExecuteUpdateAsync(s => s.SetProperty(n => n.LastNumber, 0));

        return await WriteAuditAndReturn("RESET_ALL", "All transaction data across every module", counts, userId, userIp);
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
