namespace SynteraERP.Api.DTOs.CustomerPO;

public class CustomerPoListDto
{
    public Guid Id { get; set; }
    public string PoNo { get; set; } = string.Empty;
    public Guid QuotationId { get; set; }
    public string QuotationNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly PoDate { get; set; }
    public decimal Amount { get; set; }
    public bool HasAttachment { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? SalesOrderNo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CustomerPoDto : CustomerPoListDto
{
    public string? Notes { get; set; }
    public string? AttachmentName { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CreateCustomerPoRequest
{
    public Guid QuotationId { get; set; }
    public string PoNo { get; set; } = string.Empty;
    public DateOnly PoDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
