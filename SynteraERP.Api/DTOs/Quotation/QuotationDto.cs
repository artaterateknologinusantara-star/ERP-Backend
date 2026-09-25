using SynteraERP.Api.Models;

namespace SynteraERP.Api.DTOs.Quotation;

// ── List ──────────────────────────────────────────────────────────────────────
public class QuotationListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string SalesName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public int Revision { get; set; }
    public bool IsLatestRevision { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public bool HasCustomerPO { get; set; }

    // Null kalau Status bukan Disetujui, atau sudah ada SalesOrder aktif untuk Quotation ini.
    // Jumlah hari sejak ApprovedAt kalau Disetujui dan belum ada SalesOrder — monitoring read-only,
    // mirror pola PurchaseOrderDto.HasActiveSupplierInvoice (bukan gate/blocking).
    public int? DaysApprovedWithoutSalesOrder { get; set; }
}

// ── Send Result ───────────────────────────────────────────────────────────────
public class SendQuotationResultDto
{
    public string QuotationNo { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public DateTimeOffset SentAt { get; set; }
}

// ── Detail ────────────────────────────────────────────────────────────────────
public class QuotationDto : QuotationListDto
{
    public Guid CustomerId { get; set; }
    public Guid SalesId { get; set; }
    public string? Notes { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? AdditionalNotes { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TotalMaterial { get; set; }
    public decimal TotalService { get; set; }
    public decimal TotalBeforeTax { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsCivilMeMode { get; set; }
    public decimal? TotalAreaSqm { get; set; }
    public string? FacilityId { get; set; }
    public string? RenovPic { get; set; }
    public string? FacilityName { get; set; }
    public string? ScopeOfWork { get; set; }
    public string? Location { get; set; }
    public string? Contractor { get; set; }
    public string? ValidityPeriod { get; set; }
    public string? AreaBlockTender { get; set; }
    public Guid? ParentId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<QuotationTabDto> Tabs { get; set; } = [];
    public List<QuotationTerminDto> Termins { get; set; } = [];
}

public class QuotationTerminDto
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

public class QuotationTabDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<QuotationGroupDto> Groups { get; set; } = [];
}

public class QuotationGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal? RecapVolume { get; set; }
    public string? RecapUnit { get; set; }
    public Guid? SubcontractorId { get; set; }
    public string? SubcontractorName { get; set; }
    public decimal? FinalSubconCost { get; set; }
    public decimal? FinalSellingPrice { get; set; }
    public List<QuotationItemDto> Items { get; set; } = [];
    public List<QuotationWorkItemDto> WorkItems { get; set; } = [];
}

public class QuotationWorkItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<QuotationWorkDetailDto> WorkDetails { get; set; } = [];
}

public class QuotationWorkDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalHarga { get; set; }
    public int SortOrder { get; set; }
    public List<QuotationWorkDetailAttachmentDto> Attachments { get; set; } = [];
}

public class QuotationWorkDetailAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class QuotationItemDto
{
    public Guid Id { get; set; }
    public string ItemNo { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public decimal MaterialPrice { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public int SortOrder { get; set; }
    public Guid? ItemMasterId { get; set; }
    public string? ItemMasterCode { get; set; }
    public string? ItemMasterName { get; set; }
}

// ── Create / Update ───────────────────────────────────────────────────────────
public class SaveQuotationRequest
{
    public Guid CustomerId { get; set; }
    public Guid SalesId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string? Notes { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? AdditionalNotes { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal TaxRate { get; set; } = 11;
    public bool IsCivilMeMode { get; set; } = false;
    public decimal? TotalAreaSqm { get; set; }
    public string? FacilityId { get; set; }
    public string? RenovPic { get; set; }
    public string? FacilityName { get; set; }
    public string? ScopeOfWork { get; set; }
    public string? Location { get; set; }
    public string? Contractor { get; set; }
    public string? ValidityPeriod { get; set; }
    public string? AreaBlockTender { get; set; }
    public List<SaveQuotationTabRequest> Tabs { get; set; } = [];
    public List<SaveQuotationTerminRequest> Termins { get; set; } = [];
}

public class SaveQuotationTerminRequest
{
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

public class SaveQuotationTabRequest
{
    /// <summary>Existing Tab id, when known — lets Update match &amp; keep this row in place
    /// instead of deleting and recreating it. Omit/null for a brand-new tab.</summary>
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<SaveQuotationGroupRequest> Groups { get; set; } = [];
}

public class SaveQuotationGroupRequest
{
    /// <summary>Existing Group id, when known — lets Update match &amp; keep this row (and
    /// anything FK'd to it, like WorkItems/RAB data) in place instead of deleting and recreating
    /// it. Omit/null for a brand-new group.</summary>
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal? RecapVolume { get; set; }
    public string? RecapUnit { get; set; }
    public Guid? SubcontractorId { get; set; }
    public decimal? FinalSubconCost { get; set; }
    public decimal? FinalSellingPrice { get; set; }
    public List<SaveQuotationItemRequest> Items { get; set; } = [];

    /// <summary>Null (field absent from the request body) means "leave this group's WorkItems
    /// untouched" — needed so a caller that doesn't yet know about this field (old frontend,
    /// still using the standalone WorkItem/WorkDetail CRUD endpoints) can keep updating a
    /// Quotation via this endpoint without silently wiping every WorkItem/WorkDetail (and their
    /// uploaded attachments) on every save. An explicit empty list DOES mean "delete all
    /// WorkItems in this group" — same upsert-by-Id contract as Tabs/Groups above otherwise.</summary>
    public List<SaveQuotationWorkItemRequest>? WorkItems { get; set; }
}

public class SaveQuotationItemRequest
{
    public string ItemNo { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public decimal MaterialPrice { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public int SortOrder { get; set; }
    public Guid? ItemMasterId { get; set; }
}

public class SaveQuotationWorkItemRequest
{
    /// <summary>Existing WorkItem id, when known — lets Update match &amp; keep this row (and
    /// its WorkDetails/attachments) in place instead of deleting and recreating it. Omit/null
    /// for a brand-new work item.</summary>
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<SaveQuotationWorkDetailRequest> WorkDetails { get; set; } = [];
}

public class SaveQuotationWorkDetailRequest
{
    /// <summary>Existing WorkDetail id, when known — lets Update match &amp; keep this row (and
    /// its uploaded attachments) in place instead of deleting and recreating it. Omit/null for a
    /// brand-new work detail.</summary>
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateQuotationStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

// ── Item Pekerjaan / Detail Kerja (RAB/BQ) ─────────────────────────────────────
public class SaveWorkItemRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class SaveWorkDetailRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int SortOrder { get; set; }
}

public class BulkDeleteRequest
{
    public List<Guid> Ids { get; set; } = [];
}
