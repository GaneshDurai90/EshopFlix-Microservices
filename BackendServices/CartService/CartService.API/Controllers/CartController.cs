using CartService.Application.DTOs;
using CartService.Application.Services.Abstractions;
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CartService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartAppService _cartAppService;
        private readonly IIdempotencyAppService _idempotency; // Application-level orchestrator

        public CartController(ICartAppService cartAppService, IIdempotencyAppService idempotency)
        {
            _cartAppService = cartAppService;
            _idempotency = idempotency;
        }

        // ----- Helpers -----
        private static string ComputeHash(string method, string path, object? body)
        {
            var payload = body is null ? string.Empty : JsonSerializer.Serialize(body);
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{method}:{path}|{payload}"));
            return Convert.ToHexString(bytes); // header safe
        }

        private (string key, string? derived) ResolveIdempotencyKey(object? body = null)
        {
            var hdr = Request.Headers["x-idempotency-key"].FirstOrDefault()
                   ?? Request.Headers["Idempotency-Key"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(hdr))
                return (hdr!, null);

            var hash = ComputeHash(Request.Method, Request.Path.Value ?? string.Empty, body);
            return (hash, hash);
        }

        // ---------- Queries (no idempotency wrapping needed) ----------
        [HttpGet("{UserId}")]
        public async Task<IActionResult> GetUserCart(long UserId)
        {
            var cart = await _cartAppService.GetUserCart(UserId);
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
            => await _cartAppService.GetCartItems(CartId);

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

        [HttpGet("{cartId}")]
        public async Task<IActionResult> SavedForLater(long cartId, CancellationToken ct)
        {
            var data = await _cartAppService.GetSavedForLaterAsync(cartId, ct);
            return Ok(data);
        }

        // ---------- Commands (wrapped with idempotency) ----------

        [HttpPost("{UserId}")]
        public async Task<IActionResult> AddItem(long UserId, [FromBody] CartItem item, CancellationToken ct)
        {
            var (key, hash) = ResolveIdempotencyKey(item);
            var dto = await _idempotency.ExecuteAsync(
                key,
                UserId,
                _ => _cartAppService.AddItem(UserId, item),
                ttl: TimeSpan.FromHours(1),
                requestHash: hash,
                ct: ct);
            return Ok(dto);
        }

        [HttpGet("{CartId}")]
        public async Task<IActionResult> MakeInActive(int CartId, CancellationToken ct)
        {
            var (key, hash) = ResolveIdempotencyKey();
            var status = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _cartAppService.MakeInActive(CartId),
                ttl: TimeSpan.FromHours(1),
                requestHash: hash,
                ct: ct);
            return Ok(status);
        }

        [HttpDelete("{CartId}/{ItemId}")]
        public async Task<IActionResult> DeleteItem(int CartId, int ItemId, CancellationToken ct)
        {
            var (key, hash) = ResolveIdempotencyKey();
            var count = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _cartAppService.DeleteItem(CartId, ItemId),
                ttl: TimeSpan.FromHours(1),
                requestHash: hash,
                ct: ct);
            return Ok(count);
        }

        [HttpGet("{CartId}/{ItemId}/{Quantity}")]
        public async Task<IActionResult> UpdateQuantity(int CartId, int ItemId, int Quantity, CancellationToken ct)
        {
            var (key, hash) = ResolveIdempotencyKey();
            var count = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _cartAppService.UpdateQuantity(CartId, ItemId, Quantity),
                ttl: TimeSpan.FromHours(1),
                requestHash: hash,
                ct: ct);
            return Ok(count);
        }

        public record ApplyCouponRequest(long CartId, string Code, decimal Amount, string? Description);

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest body, CancellationToken ct)
        {
            var (key, hash) = ResolveIdempotencyKey(body);
            var totals = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                async _ =>
                {
                    await _cartAppService.ApplyCouponAsync(body.CartId, body.Code, body.Amount, body.Description, ct);
                    return await _cartAppService.GetTotalsAsync(body.CartId, ct);
                },
                ttl: TimeSpan.FromHours(1),
                requestHash: hash,
                ct: ct);
            return Ok(totals);
        }

        [HttpDelete("{cartId}/{code}")]
        public async Task<IActionResult> RemoveCoupon(long cartId, string code, CancellationToken ct)
        {
            var (key, hash) = ResolveIdempotencyKey();
            var totals = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                async _ =>
                {
                    await _cartAppService.RemoveCouponAsync(cartId, code, ct);
                    return await _cartAppService.GetTotalsAsync(cartId, ct);
                },
                ttl: TimeSpan.FromHours(1),
                requestHash: hash,
                ct: ct);
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
