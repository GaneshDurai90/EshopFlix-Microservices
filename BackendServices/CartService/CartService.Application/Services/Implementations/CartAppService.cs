using CartService.Application.Exceptions;
using AutoMapper;
using CartService.Application.CQRS;
using CartService.Application.Carts.Commands;
using CartService.Application.DTOs;
using CartService.Application.HttpClients;
using CartService.Application.Repositories;
using CartService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using CartService.Application.Services.Abstractions;

namespace CartService.Application.Services.Implementations
{
    public class CartAppService : ICartAppService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly CatalogServiceClient _catalogServiceClient;
        private readonly IDispatcher _dispatcher;

        public CartAppService(
            ICartRepository cartRepository,
            IMapper mapper,
            IConfiguration configuration,
            CatalogServiceClient catalogServiceClient,
            IDispatcher dispatcher)
        {
            _cartRepository = cartRepository;
            _mapper = mapper;
            _configuration = configuration;
            _catalogServiceClient = catalogServiceClient;
            _dispatcher = dispatcher;
        }

        // ---------- Private helpers (unchanged) ----------

        private async Task<CartDTO> PopulateCartDetailsAsync(Cart cart, CancellationToken ct)
        {
            var cartModel = _mapper.Map<CartDTO>(cart);

            if (cartModel?.CartItems == null || cartModel.CartItems.Count == 0)
                return cartModel;

            // fetch product info once
            var productIds = cart.CartItems.Select(x => x.ItemId).Distinct().ToArray();
            var products = await _catalogServiceClient.GetByIdsAsync(productIds, ct);

            foreach (var x in cartModel.CartItems)
            {
                var p = products.FirstOrDefault(p => p.ProductId == x.ItemId);
                if (p != null)
                {
                    x.Name = p.Name;
                    x.ImageUrl = p.ImageUrl;
                }
                cartModel.Total += x.UnitPrice * x.Quantity;
            }

            // tax from config if totals aren't available
            var taxPct = decimal.TryParse(_configuration["Tax"], out var t) ? t : 0m;
            cartModel.Tax = Math.Round(cartModel.Total * (taxPct / 100m), 2);
            cartModel.GrandTotal = cartModel.Total + cartModel.Tax;

            return cartModel;
        }

        private async Task<CartDTO> BuildDtoFromSnapshotAsync(long cartId, CancellationToken ct)
        {
            var snap = await _cartRepository.GetSnapshotAsync(cartId, ct);

            // Map core + items
            var cart = snap.Cart.FirstOrDefault();
            if (cart == null) return null;

            var dto = _mapper.Map<CartDTO>(cart);
            dto.CartItems = _mapper.Map<List<CartItemDTO>>(snap.Items);

            // Enrich items from Catalog
            if (dto.CartItems?.Count > 0)
            {
                var ids = snap.Items.Select(i => i.ItemId).Distinct().ToArray();
                var products = await _catalogServiceClient.GetByIdsAsync(ids, ct);

                foreach (var line in dto.CartItems)
                {
                    var p = products.FirstOrDefault(x => x.ProductId == line.ItemId);
                    if (p != null)
                    {
                        line.Name = p.Name;
                        line.ImageUrl = p.ImageUrl;
                    }
                }
            }

            // Totals: if CartTotals exists, use that; else compute
            var totals = snap.Totals.FirstOrDefault();
            if (totals != null)
            {
                dto.Total = totals.Subtotal;
                dto.Tax = totals.TaxTotal;
                dto.GrandTotal = totals.GrandTotal;
            }
            else
            {
                // simple fallback
                dto.Total = snap.Items.Sum(i => i.UnitPrice * i.Quantity);
                dto.Tax = snap.Taxes.Sum(t => t.Amount);
                var shipping = snap.Shipments.Where(s => s.IsSelected).Sum(s => s.Cost);
                var discount = snap.Coupons.Sum(c => c.DiscountAmount) +
                               snap.Adjustments.Where(a => a.Amount < 0).Sum(a => a.Amount);
                dto.GrandTotal = dto.Total + dto.Tax + shipping + discount;
            }

            // You can also expose coupons/shipments/etc on your DTO if it has fields
            return dto;
        }

        // ---------- Commands via Dispatcher ----------

        public async Task<CartDTO> AddItem(long userId, CartItem item)
        {
            var errors = new Dictionary<string, string[]>();
            if (item.Quantity <= 0) errors[nameof(item.Quantity)] = new[] { "Quantity must be greater than zero." };
            if (item.UnitPrice < 0) errors[nameof(item.UnitPrice)] = new[] { "UnitPrice cannot be negative." };
            if (errors.Count > 0) throw AppException.Validation(errors);

            return await _dispatcher.Send(new AddItemCommand(userId, item));
        }

        public Task<int> DeleteItem(int cartId, int itemId)
            => _dispatcher.Send(new DeleteItemCommand(cartId, itemId));

        public Task<bool> MakeInActive(int cartId)
            => _dispatcher.Send(new MakeInActiveCommand(cartId));

        public Task<int> UpdateQuantity(int cartId, int itemId, int quantity)
            => _dispatcher.Send(new UpdateQuantityCommand(cartId, itemId, quantity));

        // Item options keep direct repository for now (not part of current migration list)
        public Task<int> AddItemOptionAsync(int cartItemId, string name, string value, CancellationToken ct = default)
            => _cartRepository.AddItemOptionAsync(cartItemId, name, value, ct);

        public Task<int> RemoveItemOptionAsync(int cartItemOptionId, CancellationToken ct = default)
            => _cartRepository.RemoveItemOptionAsync(cartItemOptionId, ct);

        // Coupons
        public async Task ApplyCouponAsync(long cartId, string code, decimal amount, string? description = null, CancellationToken ct = default)
            => await _dispatcher.Send(new ApplyCouponCommand(cartId, code, amount, description), ct);

        public async Task RemoveCouponAsync(long cartId, string code, CancellationToken ct = default)
            => await _dispatcher.Send(new RemoveCouponCommand(cartId, code), ct);

        // Adjustments keep direct repository for now (not part of current migration list)
        public Task<int> AddAdjustmentAsync(long cartId, int? cartItemId, string type, string description, decimal amount, CancellationToken ct = default)
            => _cartRepository.AddAdjustmentAsync(cartId, cartItemId, type, description, amount, ct);

        // Shipping
        public async Task SelectShippingAsync(long cartId, string carrier, string methodCode, string methodName,
                                              decimal cost, int? estimatedDays, string? addressSnapshotJson, CancellationToken ct = default)
            => await _dispatcher.Send(new SelectShippingCommand(cartId, carrier, methodCode, methodName, cost, estimatedDays, addressSnapshotJson), ct);

        // Taxes & Totals
        public async Task RecalculateTotalsAsync(long cartId, CancellationToken ct = default)
            => await _dispatcher.Send(new RecalculateTotalsCommand(cartId), ct);

        public Task<CartTotal?> GetTotalsAsync(long cartId, CancellationToken ct = default)
            => _cartRepository.GetTotalsAsync(cartId, ct);

        // Payments
        public async Task SetPaymentAsync(long cartId, string method, decimal amountAuthorized, string currencyCode, string status, CancellationToken ct = default)
            => await _dispatcher.Send(new SetPaymentCommand(cartId, method, amountAuthorized, currencyCode, status), ct);

        // Clear cart
        public async Task ClearAsync(long cartId, CancellationToken ct = default)
            => await _dispatcher.Send(new ClearCartCommand(cartId), ct);

        public Task<CartSnapshotDto> GetSnapshotAsync(long cartId, CancellationToken ct)
            => _cartRepository.GetSnapshotAsync(cartId, ct);

        // Reads (keep as is)
        public async Task<CartDTO> GetCart(int cartId)
        {
            var dto = await BuildDtoFromSnapshotAsync(cartId, CancellationToken.None);
            if (dto == null)
                throw AppException.NotFound("cart", $"Cart {cartId} not found or inactive");
            return dto;
        }

        public Task<int> GetCartItemCount(long userId)
            => userId > 0 ? _cartRepository.GetCartItemCount(userId) : Task.FromResult(0);

        public async Task<IEnumerable<CartItemDTO>> GetCartItems(long cartId)
        {
            var data = await _cartRepository.GetCartItems(cartId);
            return _mapper.Map<IEnumerable<CartItemDTO>>(data ?? Enumerable.Empty<CartItem>());
        }

        public async Task<CartDTO> GetUserCart(long userId)
        {
            var cart = await _cartRepository.GetUserCart(userId);
            if (cart == null) return null;

            return await BuildDtoFromSnapshotAsync(cart.Id, CancellationToken.None);
        }

        public Task<IReadOnlyList<CartItemOption>> GetItemOptionsAsync(int cartItemId, CancellationToken ct = default)
            => _cartRepository.GetItemOptionsAsync(cartItemId, ct);

        public Task<IReadOnlyList<CartCoupon>> GetCouponsAsync(long cartId, CancellationToken ct = default)
            => _cartRepository.GetCouponsAsync(cartId, ct);

        public Task<IReadOnlyList<CartAdjustment>> GetAdjustmentsAsync(long cartId, CancellationToken ct = default)
            => _cartRepository.GetAdjustmentsAsync(cartId, ct);

        public Task<IReadOnlyList<CartShipment>> GetShipmentsAsync(long cartId, CancellationToken ct = default)
            => _cartRepository.GetShipmentsAsync(cartId, ct);

        public Task<IReadOnlyList<CartTaxis>> GetTaxesAsync(long cartId, CancellationToken ct = default)
            => _cartRepository.GetTaxesAsync(cartId, ct);

        public Task<IReadOnlyList<CartPayment>> GetPaymentsAsync(long cartId, CancellationToken ct = default)
            => _cartRepository.GetPaymentsAsync(cartId, ct);

        // Save for later
        public async Task<IReadOnlyList<SavedForLaterItem>> GetSavedForLaterAsync(long cartId, CancellationToken ct = default)
            => await _cartRepository.GetSavedForLaterAsync(cartId, ct);

        public async Task SaveForLaterAsync(long cartId, int itemId, CancellationToken ct = default)
            => await _dispatcher.Send(new SaveForLaterCommand(cartId, itemId), ct);

        public async Task MoveSavedToCartAsync(int savedItemId, CancellationToken ct = default)
            => await _dispatcher.Send(new MoveSavedToCartCommand(savedItemId), ct);
    }
}
