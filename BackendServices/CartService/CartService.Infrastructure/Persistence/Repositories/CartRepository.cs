using CartService.Application.DTOs;
using CartService.Application.Repositories;
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence.Snapshots;
using Microsoft.EntityFrameworkCore;            
using Microsoft.Data.SqlClient;
using System.Globalization;
using CartService.Application.Exceptions;

namespace CartService.Infrastructure.Persistence.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly CartServiceDbContext _db;
        private ICartServiceDbContextProcedures _SP;
        public CartRepository(CartServiceDbContext db ,ICartServiceDbContextProcedures SP)
        {
            _db = db;
            _SP = SP;
        }

        private async Task RecalculateTotalsSafeAsync(long cartId, CancellationToken ct = default)
        {
            try
            {
                await _SP.SP_Cart_RecalculateTotalsAsync(cartId, cancellationToken: ct);
            }
            catch (SqlException ex) when (ex.Message.Contains("Could not find stored procedure", StringComparison.OrdinalIgnoreCase))
            {
                // Fallback to direct EXEC of the correct DB proc name
                var p = new SqlParameter("@CartId", cartId);
                await _db.Database.ExecuteSqlRawAsync("EXEC [dbo].[SP_Cart_RecalculateTotals] @CartId = @CartId", p, ct);
            }
        }

        /// <summary>
        /// Adds an item to the cart. If the cartId is not provided, it tries to find the user's active cart,
        /// and if not found it creates one. Uses SP_Cart_AddItem and recalculates totals.
        /// </summary>
        public async Task<Cart> AddItem(long UserId, CartItem item)
        {
            // 1) Resolve/ensure cart
            Cart cart;

            if (item.CartId > 0)
            {
                cart = await _db.Carts.FirstOrDefaultAsync(c => c.Id == item.CartId);
            }
            else
            {
           cart = await _db.Carts
                  .Include(c => c.CartItems)
                  .FirstOrDefaultAsync(x => x.UserId == UserId && x.IsActive);
            }

            if (cart == null)
            {
                // Derive defaults (culture-aware with safe fallbacks)
                string currencyCode = "USD";
                string locale = "en-US";
                string countryCode = "US";
                try
                {
                    var ci = CultureInfo.CurrentUICulture;
                    locale = string.IsNullOrWhiteSpace(ci.Name) ? locale : ci.Name;

                    // Try RegionInfo (may throw for invariant cultures)
                    var ri = new RegionInfo(ci.LCID);
                    countryCode = string.IsNullOrWhiteSpace(ri.TwoLetterISORegionName) ? countryCode : ri.TwoLetterISORegionName;

                    // Currency from RegionInfo if available
                    currencyCode = string.IsNullOrWhiteSpace(ri.ISOCurrencySymbol) ? currencyCode : ri.ISOCurrencySymbol;
                }
                catch
                {
                    // keep fallbacks
                }

                // Create a new cart via stored proc (keeps DB-first pattern)
                var createRes = await _SP.SP_Cart_CreateAsync(
                    userId: UserId,
                    currencyCode: currencyCode,
                    channel: "WEB",
                    anonymousId: null,
                    locale: locale,
                    countryCode: countryCode);

                var newCartId = createRes.FirstOrDefault()?.CartId ?? 0L;
                cart = await _db.Carts.FirstAsync(c => c.Id == newCartId);
            }

            // 2) If the same ItemId already exists in this cart, just bump qty
            var existing = await _db.CartItems
                                    .FirstOrDefaultAsync(x => x.CartId == cart.Id && x.ItemId == item.ItemId);

            await using var tx = await _db.Database.BeginTransactionAsync();

            if (existing != null)
            {
                // use proc to update qty (needs CartItemId)
                await _SP.SP_Cart_UpdateItemQuantityAsync(existing.Id, existing.Quantity + item.Quantity);
            }
            else
            {
                // 3) Insert via stored proc (let SP set defaults/derived fields)
                var addRes = await _SP.SP_Cart_AddItemAsync(
                    cartId: cart.Id,
                    itemId: item.ItemId,
                    sKU: item.Sku,
                    productName: item.ProductName,
                    unitPrice: item.UnitPrice,
                    quantity: item.Quantity,
                    taxCategory: item.TaxCategory,
                    productSnapshotJson: item.ProductSnapshotJson,
                    variantJson: item.VariantJson,
                    isGift: item.IsGift,
                    parentItemId: item.ParentItemId);
            }

            // 4) Recalculate totals (keeps CartTotals in sync)
            await RecalculateTotalsSafeAsync(cart.Id);

            await tx.CommitAsync();

            // 5) Return a fresh cart (with items) so caller sees latest state
            cart = await _db.Carts.Include(c => c.CartItems)
                                  .FirstAsync(c => c.Id == cart.Id);
            return cart;
        }

        /// <summary>
        /// Deletes an item from the cart by CartId and ItemId (uses SP_Cart_RemoveItem internally).
        /// </summary>
        public async Task<int> DeleteItem(long CartId, int ItemId)
        {
            var cartItemId = await _db.CartItems
                                      .Where(x => x.CartId == CartId && x.ItemId == ItemId)
                                      .Select(x => (int?)x.Id)
                                      .FirstOrDefaultAsync();

            if (cartItemId == null)
                throw AppException.NotFound("cart.item", $"Item {ItemId} not found in cart {CartId}");

            await _SP.SP_Cart_RemoveItemAsync(cartItemId.Value);
            await RecalculateTotalsSafeAsync(CartId);
            return 1;
        }

        /// <summary>
        /// Retrieves a cart by CartId, including its items, if it is active.
        /// </summary>
        public async Task<Cart> GetCart(long CartId)
        {
            return await _db.Carts
                            .Include(c => c.CartItems)
                            .FirstOrDefaultAsync(c => c.Id == CartId && c.IsActive);
        }

        /// <summary>
        /// Gets the total count of items in the user's active cart.
        /// </summary>
        public async Task<int> GetCartItemCount(long UserId)
        {
            var cart = await _db.Carts
                                .Include(c => c.CartItems)
                                .FirstOrDefaultAsync(c => c.UserId == UserId && c.IsActive);
            return cart?.CartItems.Sum(c => c.Quantity) ?? 0;
        }

        /// <summary>
        /// Retrieves all items in a cart by CartId.
        /// </summary>
        public async Task<IEnumerable<CartItem>> GetCartItems(long CartId)
        {
            return await _db.CartItems
                            .Where(c => c.CartId == CartId)
                            .ToListAsync();
        }

        /// <summary>
        /// Retrieves the user's active cart, including its items.
        /// </summary>
        public async Task<Cart> GetUserCart(long UserId)
        {
            return await _db.Carts
                            .Include(c => c.CartItems)
                            .FirstOrDefaultAsync(c => c.UserId == UserId && c.IsActive);
        }

        /// <summary>
        /// Marks a cart as inactive by setting its IsActive property to false.
        /// </summary>
        public async Task<bool> MakeInActive(long CartId)
        {
            var cart = await _db.Carts.FindAsync(CartId);
            if (cart != null)
            {
                cart.IsActive = false;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the quantity of a specific item (by CartId + ItemId). Uses SP_Cart_UpdateItemQuantity.
        /// </summary>
        public async Task<int> UpdateQuantity(long CartId, int ItemId, int Quantity)
        {
            var cartItem = await _db.CartItems
                                    .FirstOrDefaultAsync(x => x.CartId == CartId && x.ItemId == ItemId);
            if (cartItem == null)
                throw AppException.NotFound("cart.item", $"Item {ItemId} not found in cart {CartId}");

            await _SP.SP_Cart_UpdateItemQuantityAsync(cartItem.Id, cartItem.Quantity + Quantity);
            await RecalculateTotalsSafeAsync(CartId);
            return 1;
        }

        #region Item Options

        public async Task<int> AddItemOptionAsync(int cartItemId, string name, string value, CancellationToken ct = default)
        {
            // EF is fine here (no sproc available/needed)
            var opt = new CartItemOption
            {
                CartItemId = cartItemId,
                Name = name,
                Value = value
            };
            _db.CartItemOptions.Add(opt);
            return await _db.SaveChangesAsync(ct);
        }

        public async Task<int> RemoveItemOptionAsync(int cartItemOptionId, CancellationToken ct = default)
        {
            var opt = await _db.CartItemOptions.FindAsync(new object?[] { cartItemOptionId }, ct);
            if (opt == null) return 0;
            _db.CartItemOptions.Remove(opt);
            return await _db.SaveChangesAsync(ct);
        }

        public Task<IReadOnlyList<CartItemOption>> GetItemOptionsAsync(int cartItemId, CancellationToken ct = default)
            => _db.CartItemOptions.Where(x => x.CartItemId == cartItemId)
                                  .AsNoTracking()
                                  .ToListAsync(ct)
                                  .ContinueWith(t => (IReadOnlyList<CartItemOption>)t.Result, ct);

        #endregion

        #region Coupons

        public async Task ApplyCouponAsync(long cartId, string code, decimal amount, string? description = null, CancellationToken ct = default)
        {
            await _SP.SP_Cart_ApplyCouponAsync(cartId, code, description, amount, "PromotionService", cancellationToken: ct);
            await RecalculateTotalsSafeAsync(cartId, ct);
        }

        public async Task RemoveCouponAsync(long cartId, string code, CancellationToken ct = default)
        {
            await _SP.SP_Cart_RemoveCouponAsync(cartId, code, cancellationToken: ct);
            await RecalculateTotalsSafeAsync(cartId, ct);
        }

        public Task<IReadOnlyList<CartCoupon>> GetCouponsAsync(long cartId, CancellationToken ct = default)
            => _db.CartCoupons.Where(x => x.CartId == cartId)
                              .AsNoTracking()
                              .ToListAsync(ct)
                              .ContinueWith(t => (IReadOnlyList<CartCoupon>)t.Result, ct);

        #endregion

        #region Adjustments

        public async Task<int> AddAdjustmentAsync(long cartId, int? cartItemId, string type, string description, decimal amount, CancellationToken ct = default)
        {
            var res = await _SP.SP_Cart_AddAdjustmentAsync(cartId, cartItemId, type, description, amount, cancellationToken: ct);
            await RecalculateTotalsSafeAsync(cartId, ct);
            // if your sproc returns AdjustmentId, you can read res.First().AdjustmentId
            return 1;
        }

        public Task<IReadOnlyList<CartAdjustment>> GetAdjustmentsAsync(long cartId, CancellationToken ct = default)
            => _db.CartAdjustments.Where(x => x.CartId == cartId)
                                  .AsNoTracking()
                                  .ToListAsync(ct)
                                  .ContinueWith(t => (IReadOnlyList<CartAdjustment>)t.Result, ct);

        #endregion

        #region Shipping

        public async Task SelectShippingAsync(long cartId, string carrier, string methodCode, string methodName,
                                             decimal cost, int? estimatedDays, string? addressSnapshotJson, CancellationToken ct = default)
        {
            await _SP.SP_Cart_SelectShippingMethodAsync(cartId, carrier, methodCode, methodName, cost, estimatedDays, addressSnapshotJson, cancellationToken: ct);
            await RecalculateTotalsSafeAsync(cartId, ct);
        }

        public Task<IReadOnlyList<CartShipment>> GetShipmentsAsync(long cartId, CancellationToken ct = default)
            => _db.CartShipments.Where(x => x.CartId == cartId)
                                .AsNoTracking()
                                .ToListAsync(ct)
                                .ContinueWith(t => (IReadOnlyList<CartShipment>)t.Result, ct);

        #endregion

        #region Taxes & Totals

        public Task RecalculateTotalsAsync(long cartId, CancellationToken ct = default)
            => RecalculateTotalsSafeAsync(cartId, ct);

        public async Task<CartTotal?> GetTotalsAsync(long cartId, CancellationToken ct = default)
        {
            return await _db.CartTotals.AsNoTracking().FirstOrDefaultAsync(x => x.CartId == cartId, ct);
        }

        public Task<IReadOnlyList<CartTaxis>> GetTaxesAsync(long cartId, CancellationToken ct = default)
            => _db.CartTaxes.Where(x => x.CartId == cartId)
                            .AsNoTracking()
                            .ToListAsync(ct)
                            .ContinueWith(t => (IReadOnlyList<CartTaxis>)t.Result, ct);

        #endregion

        #region Payments

        public async Task SetPaymentAsync(long cartId, string method, decimal amountAuthorized, string currencyCode, string status, CancellationToken ct = default)
        {
            await _SP.SP_Cart_SetPaymentAsync(cartId, method, amountAuthorized, currencyCode, status, cancellationToken: ct);
            // typically payment doesn’t change totals, but if your totals include fees, uncomment:
            // await Procs.SP_Cart_RecalculateTotalsAsync(cartId, cancellationToken: ct);
        }

        public Task<IReadOnlyList<CartPayment>> GetPaymentsAsync(long cartId, CancellationToken ct = default)
            => _db.CartPayments.Where(x => x.CartId == cartId)
                               .AsNoTracking()
                               .ToListAsync(ct)
                               .ContinueWith(t => (IReadOnlyList<CartPayment>)t.Result, ct);

        #endregion

        #region Clear

        public async Task ClearAsync(long cartId, CancellationToken ct = default)
        {
            await _SP.SP_Cart_ClearAsync(cartId, cancellationToken: ct);
            await RecalculateTotalsSafeAsync(cartId, ct);
        }

        #endregion



        // -------- Optional but recommended: full snapshot using multi-result proc --------

        /// <summary>
        /// Full cart snapshot (Cart, Items, Options, Coupons, Adjustments, Shipments, Taxes, Payments, Totals)
        /// using the multi-result reader.
        /// </summary>
        public async Task<CartSnapshotDto> GetSnapshotAsync(long cartId, CancellationToken ct = default)
        {
            var snap = await CartSnapshotReader.ReadAsync(_db, cartId, 60, ct);
            return new CartSnapshotDto
            {
                Cart = snap.Cart,
                Items = snap.Items,
                ItemOptions = snap.ItemOptions,
                Coupons = snap.Coupons,
                Adjustments = snap.Adjustments,
                Shipments = snap.Shipments,
                Taxes = snap.Taxes,
                Payments = snap.Payments,
                Totals = snap.Totals
            };
        }

        #region Save For Later
        public async Task<IReadOnlyList<SavedForLaterItem>> GetSavedForLaterAsync(long cartId, CancellationToken ct = default)
            => await _db.SavedForLaterItems.Where(x => x.CartId == cartId)
                                           .AsNoTracking()
                                           .ToListAsync(ct);

        public async Task SaveForLaterAsync(long cartId, int itemId, CancellationToken ct = default)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(x => x.CartId == cartId && x.ItemId == itemId, ct);
            if (item == null)
                throw AppException.NotFound("cart.item", $"Item {itemId} not found in cart {cartId}");

            var sfl = new SavedForLaterItem
            {
                CartId = cartId,
                ItemId = item.ItemId,
                Sku = item.Sku,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                ProductSnapshotJson = item.ProductSnapshotJson,
                CreatedDate = DateTime.UtcNow
            };
            _db.SavedForLaterItems.Add(sfl);
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync(ct);
            await RecalculateTotalsSafeAsync(cartId, ct);
        }

        public async Task MoveSavedToCartAsync(int savedItemId, CancellationToken ct = default)
        {
            var sfl = await _db.SavedForLaterItems.FirstOrDefaultAsync(x => x.Id == savedItemId, ct);
            if (sfl == null)
                throw AppException.NotFound("saved.item", $"Saved item {savedItemId} not found");

            var existing = await _db.CartItems.FirstOrDefaultAsync(x => x.CartId == sfl.CartId && x.ItemId == sfl.ItemId, ct);
            if (existing != null)
            {
                existing.Quantity += sfl.Quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    CartId = sfl.CartId,
                    ItemId = sfl.ItemId,
                    Sku = sfl.Sku,
                    ProductName = sfl.ProductName,
                    UnitPrice = sfl.UnitPrice,
                    Quantity = sfl.Quantity,
                    ProductSnapshotJson = sfl.ProductSnapshotJson
                });
            }
            _db.SavedForLaterItems.Remove(sfl);
            await _db.SaveChangesAsync(ct);
            await RecalculateTotalsSafeAsync(sfl.CartId, ct);
        }
        #endregion
    }
}

