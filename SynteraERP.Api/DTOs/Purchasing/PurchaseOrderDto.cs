using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.Purchasing;

public class PurchaseOrderQueryParams : PaginationParams
{
    public string? Status { get; set; }
}

public class PurchaseOrderListDto
{
    public Guid Id { get; set; }
    public string No { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? PurchaseRequestNo { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public int ItemCount { get; set; }

    // True kalau ada minimal 1 SupplierInvoice aktif (bukan soft-deleted, bukan Cancelled) untuk PO
    // ini. Dipakai FE untuk badge "Diterima, Belum Ditagih" saat Status == Completed tapi ini false -
    // Completed untuk PO berarti "barang sudah diterima penuh" (auto-set di ReceiveGoodsAsync), BUKAN
    // "sudah ditagih supplier" - dua hal ini sengaja didesain independen (lihat pola GRNI di
    // ReceiveGoodsAsync), jadi field ini murni indikator monitoring, bukan gate.
    public bool HasActiveSupplierInvoice { get; set; }
}

public class PurchaseOrderDto : PurchaseOrderListDto
{
    public Guid SupplierId { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
    public List<POPaymentResponse> Payments { get; set; } = [];
}

public class RecordPOPaymentRequest
{
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public Guid? CashBankAccountId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class POPaymentResponse
{
    public Guid Id { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public Guid? CashBankAccountId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal InvoicedQty { get; set; }
    public Guid? ItemMasterId { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePOItemRequest> Items { get; set; } = [];
}

public class CreatePOItemRequest
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class UpdatePOStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class ReceiveGoodsRequest
{
    public List<ReceiveGoodsItemRequest> Items { get; set; } = [];
    public string? Notes { get; set; }
}

public class CreatePoFromPrRequest
{
    public Guid SupplierId { get; set; }
    public List<CreatePoFromPrItemRequest> Items { get; set; } = [];
    public string? Notes { get; set; }
    public DateOnly? DeliveryDate { get; set; }
}

public class CreatePoFromPrItemRequest
{
    public Guid PRItemId { get; set; }
    public decimal Qty { get; set; }
}

public class ReceiveGoodsItemRequest
{
    public Guid ItemId { get; set; }
    public decimal ReceivedQty { get; set; }
}

public class PurchaseOrderStatsDto
{
    public int Total { get; set; }
    public int Draft { get; set; }
    public int Ordered { get; set; }
    public int PartialReceive { get; set; }
    public int Completed { get; set; }
    public decimal TotalValue { get; set; }
}
