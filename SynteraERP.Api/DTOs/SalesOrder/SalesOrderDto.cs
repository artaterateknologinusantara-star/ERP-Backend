using SynteraERP.Api.Models;

namespace SynteraERP.Api.DTOs.SalesOrder;

public class SalesOrderListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string SalesName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? QuotationNo { get; set; }
}

public class SalesOrderDto : SalesOrderListDto
{
    public Guid CustomerId { get; set; }
    public Guid SalesId { get; set; }
    public Guid? QuotationId { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateSalesOrderRequest
{
    public Guid CustomerId { get; set; }
    public Guid SalesId { get; set; }
    public Guid? QuotationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSalesOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
