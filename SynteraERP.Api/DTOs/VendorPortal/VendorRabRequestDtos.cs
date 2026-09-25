namespace SynteraERP.Api.DTOs.VendorPortal;

public class VendorRabRequestDto
{
    public Guid Id { get; set; }
    public Guid QuotationGroupId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public Guid? ApprovedWorkItemId { get; set; }
    public List<VendorRabRequestLineDto> Lines { get; set; } = [];
    public List<VendorRabSubmissionSummaryDto> Submissions { get; set; } = [];
}

public class VendorRabRequestLineDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class VendorRabSubmissionSummaryDto
{
    public Guid Id { get; set; }
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
}

public class CreateVendorRabRequestRequest
{
    public Guid SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? DueDate { get; set; }
    public List<CreateVendorRabRequestLineRequest> Lines { get; set; } = [];
}

public class CreateVendorRabRequestLineRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Spesifikasi { get; set; }
    public decimal Volume { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
