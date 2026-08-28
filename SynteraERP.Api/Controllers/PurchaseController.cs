using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.DTOs.Purchasing;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/purchase-requests")]
public class PurchaseRequestController : ControllerBase
{
    private readonly IPurchaseRequestService _svc;

    public PurchaseRequestController(IPurchaseRequestService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PurchaseRequestListDto>>>> List([FromQuery] PaginationParams p)
    {
        var result = await _svc.ListAsync(p);
        return Ok(ApiResponse<PaginatedResponse<PurchaseRequestListDto>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _svc.GetStatsAsync();
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<PurchaseRequestDto>.Fail("Purchase Request tidak ditemukan."));
        return Ok(ApiResponse<PurchaseRequestDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDto>>> Create([FromBody] CreatePurchaseRequestRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<PurchaseRequestDto>.Ok(item, "Purchase Request berhasil dibuat."));
    }

    [HttpPost("generate-from-so/{soId:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDto>>> GenerateFromSo(Guid soId)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<PurchaseRequestDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.GenerateFromSoAsync(soId, userId);
        if (item is null)
            return NotFound(ApiResponse<PurchaseRequestDto>.Fail("Sales Order tidak ditemukan atau sudah dihapus."));

        return CreatedAtAction(nameof(Get), new { id = item.Id },
            ApiResponse<PurchaseRequestDto>.Ok(item, "Purchase Request berhasil di-generate dari Sales Order."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdatePRStatusRequest request)
    {
        var ok = await _svc.UpdateStatusAsync(id, request.Status);
        if (!ok) return BadRequest(ApiResponse.Fail("Status tidak valid atau Purchase Request tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Purchase Request tidak ditemukan."));
        return Ok(ApiResponse.Ok("Purchase Request berhasil dihapus."));
    }
}

[Authorize]
[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _svc;
    private readonly AppDbContext _db;

    public PurchaseOrderController(IPurchaseOrderService svc, AppDbContext db)
    {
        _svc = svc;
        _db  = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PurchaseOrderListDto>>>> List(
        [FromQuery] PaginationParams p, [FromQuery] Guid? purchaseRequestId, [FromQuery] string? purchaseRequestIds)
    {
        IEnumerable<Guid>? ids = null;
        if (!string.IsNullOrWhiteSpace(purchaseRequestIds))
        {
            ids = purchaseRequestIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => Guid.TryParse(s, out _))
                .Select(Guid.Parse);
        }

        var result = await _svc.ListAsync(p, purchaseRequestId, ids);
        return Ok(ApiResponse<PaginatedResponse<PurchaseOrderListDto>>.Ok(result));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _svc.GetStatsAsync();
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Get(Guid id)
    {
        var item = await _svc.GetByIdAsync(id);
        if (item is null) return NotFound(ApiResponse<PurchaseOrderDto>.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        var item = await _svc.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<PurchaseOrderDto>.Ok(item, "Purchase Order berhasil dibuat."));
    }

    [HttpPost("from-pr/{prId:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreateFromPr(Guid prId, [FromBody] CreatePoFromPrRequest request)
    {
        var item = await _svc.CreateFromPrAsync(prId, request);
        if (item is null)
            return NotFound(ApiResponse<PurchaseOrderDto>.Fail("Purchase Request tidak ditemukan, atau statusnya bukan Approved/PartiallyOrdered."));
        return CreatedAtAction(nameof(Get), new { id = item.Id },
            ApiResponse<PurchaseOrderDto>.Ok(item, "Purchase Order berhasil dibuat dari Purchase Request."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdatePOStatusRequest request)
    {
        var ok = await _svc.UpdateStatusAsync(id, request.Status);
        if (!ok) return BadRequest(ApiResponse.Fail("Status tidak valid atau Purchase Order tidak ditemukan."));
        return Ok(ApiResponse.Ok("Status berhasil diperbarui."));
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> ReceiveGoods(Guid id, [FromBody] ReceiveGoodsRequest request)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<PurchaseOrderDto>.Fail("User tidak teridentifikasi."));

        var item = await _svc.ReceiveGoodsAsync(id, request, userId);
        if (item is null) return NotFound(ApiResponse<PurchaseOrderDto>.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(item, "Penerimaan barang berhasil dicatat."));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPOPaymentRequest request)
    {
        try
        {
            var result = await _svc.RecordPaymentAsync(id, request);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse.Fail("Purchase Order tidak ditemukan."));
        return Ok(ApiResponse.Ok("Purchase Order berhasil dihapus."));
    }

    // TODO: remove after one-time backfill use
    [Authorize(Roles = "Administrator")]
    [HttpPost("admin/backfill-stock/{poId:guid}")]
    public async Task<IActionResult> BackfillStock(Guid poId)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = sub is not null && Guid.TryParse(sub, out var uid) ? uid : Guid.Empty;

        var po = await _db.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == poId);

        if (po is null) return NotFound(new { success = false, message = "PO tidak ditemukan." });

        var results = new List<object>();

        foreach (var item in po.Items.Where(x => x.ReceivedQty > 0))
        {
            ItemMaster? master = null;

            if (item.ItemMasterId.HasValue)
            {
                master = await _db.ItemMasters.FindAsync(item.ItemMasterId.Value);
            }
            else
            {
                master = await _db.ItemMasters
                    .FirstOrDefaultAsync(x =>
                        x.Name.ToLower() == item.ItemName.ToLower() && x.IsActive);

                if (master is not null)
                    item.ItemMasterId = master.Id;
            }

            if (master is null)
            {
                results.Add(new { item = item.ItemName, status = "no match found" });
                continue;
            }

            var existingTx = await _db.StockTransactions
                .AnyAsync(x => x.RefId == po.Id
                    && x.ItemMasterId == master.Id
                    && x.Source == StockTransactionSource.PurchaseOrder);

            if (existingTx)
            {
                results.Add(new { item = item.ItemName, status = "already processed" });
                continue;
            }

            var stockBefore = master.Stock;
            master.Stock += item.ReceivedQty;
            master.LastPurchasePrice = item.Price;

            if (master.PreferredVendorId == null)
                master.PreferredVendorId = po.SupplierId;

            master.UpdatedAt = DateTimeOffset.UtcNow;

            _db.StockTransactions.Add(new StockTransaction
            {
                ItemMasterId    = master.Id,
                Type            = StockTransactionType.StockIn,
                Source          = StockTransactionSource.PurchaseOrder,
                Qty             = item.ReceivedQty,
                StockBefore     = stockBefore,
                StockAfter      = master.Stock,
                RefNo           = po.No,
                RefId           = po.Id,
                Notes           = $"[BACKFILL] Goods Receipt dari PO {po.No}",
                CreatedByUserId = userId,
            });

            results.Add(new
            {
                item        = item.ItemName,
                qty         = item.ReceivedQty,
                stockBefore,
                stockAfter  = master.Stock,
                status      = "backfilled",
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, results });
    }
}
