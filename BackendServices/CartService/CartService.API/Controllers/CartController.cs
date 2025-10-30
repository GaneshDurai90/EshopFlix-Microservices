using CartService.Application.DTOs;
using CartService.Application.Services.Abstractions;
using CartService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CartService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        ICartAppService _cartAppService;

        public CartController(ICartAppService cartAppService)
        {
            _cartAppService = cartAppService;

        }

        [HttpGet("{UserId}")]
        public async Task<IActionResult> GetUserCart(long UserId)
        {
            var cart = await _cartAppService.GetUserCart(UserId);
            return Ok(cart);
        }

        [HttpPost("{UserId}")]
        public async Task<IActionResult> AddItem(long UserId, CartItem item)
        {
            var cart = await _cartAppService.AddItem(UserId, item);
            return Ok(cart);
        }

        [HttpGet("{CartId}")]
        public async Task<IActionResult> GetCart(int CartId)
        {
            var cart = await _cartAppService.GetCart(CartId);
            return Ok(cart);
        }

        [HttpGet("{UserId}")]
        public async Task<IActionResult> GetCartItemCount(int UserId)
        {
            var count = await _cartAppService.GetCartItemCount(UserId);
            return Ok(count);
        }

        [HttpGet("{CartId}")]
        public async Task<IEnumerable<CartItemDTO>> GetItems(int CartId)
        {
            return await _cartAppService.GetCartItems(CartId);
        }

        [HttpGet("{CartId}")]
        public async Task<IActionResult> MakeInActive(int CartId)
        {
            var status = await _cartAppService.MakeInActive(CartId);
            return Ok(status);
        }

        [HttpDelete("{CartId}/{ItemId}")]
        public async Task<IActionResult> DeleteItem(int CartId, int ItemId)
        {
            var count = await _cartAppService.DeleteItem(CartId, ItemId);
            return Ok(count);
        }

        [HttpGet("{CartId}/{ItemId}/{Quantity}")]
        public async Task<IActionResult> UpdateQuantity(int CartId, int ItemId, int Quantity)
        {
            var count = await _cartAppService.UpdateQuantity(CartId, ItemId, Quantity);
            return Ok(count);
        }

        // ===== New endpoints to expose expanded Cart features =====

        [HttpGet("{cartId}")]
        public async Task<IActionResult> Snapshot(long cartId, CancellationToken ct)
        {
            var snap = await _cartAppService.GetSnapshotAsync(cartId, ct);
            return Ok(snap);
        }

        [HttpGet("{cartId}")]
        public async Task<IActionResult> Totals(long cartId, CancellationToken ct)
        {
            var totals = await _cartAppService.GetTotalsAsync(cartId, ct);
            return Ok(totals);
        }

        [HttpGet("{cartId}")]
        public async Task<IActionResult> Shipments(long cartId, CancellationToken ct)
        {
            var data = await _cartAppService.GetShipmentsAsync(cartId, ct);
            return Ok(data);
        }

        [HttpGet("{cartId}")]
        public async Task<IActionResult> Coupons(long cartId, CancellationToken ct)
        {
            var data = await _cartAppService.GetCouponsAsync(cartId, ct);
            return Ok(data);
        }

        [HttpGet("{cartId}")]
        public async Task<IActionResult> Taxes(long cartId, CancellationToken ct)
        {
            var data = await _cartAppService.GetTaxesAsync(cartId, ct);
            return Ok(data);
        }

        public record ApplyCouponRequest(long CartId, string Code, decimal Amount, string? Description);

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest body, CancellationToken ct)
        {
            await _cartAppService.ApplyCouponAsync(body.CartId, body.Code, body.Amount, body.Description, ct);
            var totals = await _cartAppService.GetTotalsAsync(body.CartId, ct);
            return Ok(totals);
        }

        [HttpDelete("{cartId}/{code}")]
        public async Task<IActionResult> RemoveCoupon(long cartId, string code, CancellationToken ct)
        {
            await _cartAppService.RemoveCouponAsync(cartId, code, ct);
            var totals = await _cartAppService.GetTotalsAsync(cartId, ct);
            return Ok(totals);
        }

        public record SelectShippingRequest(long CartId, string Carrier, string MethodCode, string MethodName, decimal Cost, int? EstimatedDays, string? AddressSnapshotJson);

        [HttpPost]
        public async Task<IActionResult> SelectShipping([FromBody] SelectShippingRequest body, CancellationToken ct)
        {
            await _cartAppService.SelectShippingAsync(body.CartId, body.Carrier, body.MethodCode, body.MethodName, body.Cost, body.EstimatedDays, body.AddressSnapshotJson, ct);
            var totals = await _cartAppService.GetTotalsAsync(body.CartId, ct);
            return Ok(totals);
        }

        [HttpPost("{cartId}")]
        public async Task<IActionResult> RecalculateTotals(long cartId, CancellationToken ct)
        {
            await _cartAppService.RecalculateTotalsAsync(cartId, ct);
            var totals = await _cartAppService.GetTotalsAsync(cartId, ct);
            return Ok(totals);
        }

        [HttpPost("{cartId}")]
        public async Task<IActionResult> Clear(long cartId, CancellationToken ct)
        {
            await _cartAppService.ClearAsync(cartId, ct);
            var totals = await _cartAppService.GetTotalsAsync(cartId, ct);
            return Ok(totals);
        }

        [HttpGet("{cartId}")]
        public async Task<IActionResult> SavedForLater(long cartId, CancellationToken ct)
        {
            var data = await _cartAppService.GetSavedForLaterAsync(cartId, ct);
            return Ok(data);
        }

        public record SaveForLaterRequest(long CartId, int ItemId);
        [HttpPost]
        public async Task<IActionResult> SaveForLater([FromBody] SaveForLaterRequest body, CancellationToken ct)
        {
            await _cartAppService.SaveForLaterAsync(body.CartId, body.ItemId, ct);
            return Ok();
        }

        [HttpPost("{savedItemId}")]
        public async Task<IActionResult> MoveSavedToCart(int savedItemId, CancellationToken ct)
        {
            await _cartAppService.MoveSavedToCartAsync(savedItemId, ct);
            return Ok();
        }
    }
}
