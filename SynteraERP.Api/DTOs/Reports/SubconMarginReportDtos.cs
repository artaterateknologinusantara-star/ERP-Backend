namespace SynteraERP.Api.DTOs.Reports;

public class SubconMarginByVendorRowDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int ApprovedSubmissionCount { get; set; }
    public decimal TotalNilaiJual { get; set; }
    public decimal TotalMargin { get; set; }
    public decimal MarginPercent { get; set; }
}

public class SubconMarginByVendorReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<SubconMarginByVendorRowDto> Rows { get; set; } = [];
    public decimal TotalNilaiJual { get; set; }
    public decimal TotalMargin { get; set; }
    public decimal MarginPercent { get; set; }
}

// 1 baris = 1 approved VendorRabSubmission, digabung ke Quotation lewat
// VendorRabRequest.QuotationGroupId. 1 Group boleh punya >1 vendor (approve simultan dari
// request berbeda), jadi VendorGroups di dalam SubconMarginByQuotationRowDto bisa >1 baris.
public class SubconMarginQuotationVendorGroupDto
{
    public Guid QuotationGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid VendorRabSubmissionId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public decimal TotalNilaiJual { get; set; }
    public decimal TotalMargin { get; set; }
}

public class SubconMarginByQuotationRowDto
{
    public Guid QuotationId { get; set; }
    public string No { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<SubconMarginQuotationVendorGroupDto> VendorGroups { get; set; } = [];
    public decimal TotalNilaiJual { get; set; }
    public decimal TotalMargin { get; set; }
}

public class SubconMarginByQuotationReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<SubconMarginByQuotationRowDto> Rows { get; set; } = [];
}
