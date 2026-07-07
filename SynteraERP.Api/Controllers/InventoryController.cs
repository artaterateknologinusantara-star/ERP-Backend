using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynteraERP.Api.DTOs.Inventory;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _svc;

    public InventoryController(IInventoryService svc) => _svc = svc;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _svc.GetStatsAsync();
        return Ok(new { success = true, data = result });
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var result = await _svc.GetLowStockItemsAsync();
        return Ok(new { success = true, data = result });
    }

    [HttpGet("stock-history")]
    public async Task<IActionResult> GetStockHistory(
        [FromQuery] Guid? itemMasterId,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var result = await _svc.GetStockHistoryAsync(itemMasterId, page, perPage);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("stock-in")]
    public async Task<IActionResult> StockIn([FromBody] RecordStockInRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { success = false, message = "User tidak teridentifikasi." });

        var result = await _svc.RecordStockInAsync(request, userId.Value);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("delivery-orders")]
    public async Task<IActionResult> GetDeliveryOrders(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var result = await _svc.GetDeliveryOrdersAsync(page, perPage, search, status);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("delivery-orders/{id:guid}")]
    public async Task<IActionResult> GetDeliveryOrder(Guid id)
    {
        var result = await _svc.GetDeliveryOrderByIdAsync(id);
        if (result is null)
            return NotFound(new { success = false, message = "Delivery Order tidak ditemukan." });
        return Ok(new { success = true, data = result });
    }

    [HttpPost("delivery-orders/from-so/{soId:guid}")]
    public async Task<IActionResult> CreateDOFromSO(Guid soId)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { success = false, message = "User tidak teridentifikasi." });

        try
        {
            var result = await _svc.CreateDOFromSOAsync(soId, userId.Value);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("delivery-orders")]
    public async Task<IActionResult> CreateDeliveryOrder([FromBody] CreateDeliveryOrderRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { success = false, message = "User tidak teridentifikasi." });

        var result = await _svc.CreateDeliveryOrderAsync(request, userId.Value);
        return CreatedAtAction(nameof(GetDeliveryOrder), new { id = result.Id },
            new { success = true, data = result });
    }

    [HttpPost("delivery-orders/{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmDeliveryOrder(Guid id)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { success = false, message = "User tidak teridentifikasi." });

        var result = await _svc.ConfirmDeliveryOrderAsync(id, userId.Value);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("delivery-orders/{id:guid}/delivered")]
    public async Task<IActionResult> MarkDelivered(Guid id)
    {
        var result = await _svc.MarkDeliveredAsync(id);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("delivery-orders/{id:guid}")]
    public async Task<IActionResult> DeleteDeliveryOrder(Guid id)
    {
        await _svc.DeleteDeliveryOrderAsync(id);
        return Ok(new { success = true, message = "Delivery Order berhasil dihapus." });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
