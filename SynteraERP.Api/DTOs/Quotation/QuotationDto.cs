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
    public Guid? ParentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<QuotationTabDto> Tabs { get; set; } = [];
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
    public List<QuotationItemDto> Items { get; set; } = [];
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
    public int SortOrder { get; set; }
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
    public List<SaveQuotationTabRequest> Tabs { get; set; } = [];
}

public class SaveQuotationTabRequest
{
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<SaveQuotationGroupRequest> Groups { get; set; } = [];
}

public class SaveQuotationGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<SaveQuotationItemRequest> Items { get; set; } = [];
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
    public int SortOrder { get; set; }
}

public class UpdateQuotationStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class BulkDeleteRequest
{
    public List<Guid> Ids { get; set; } = [];
}
