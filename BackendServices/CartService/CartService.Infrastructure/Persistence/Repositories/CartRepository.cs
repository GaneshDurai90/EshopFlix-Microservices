using CartService.Application.DTOs;
using CartService.Application.Exceptions;
using CartService.Application.Repositories;
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence.Snapshots;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CartService.Infrastructure.Persistence.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly CartServiceDbContext _db;
        private ICartServiceDbContextProcedures _SP;

        public CartRepository(CartServiceDbContext db, ICartServiceDbContextProcedures SP)
        {
            _db = db;
            _SP = SP;
        }

        // Backstop invariants only (boundary validation is handled by FluentValidation at the API layer).
        private static void EnsurePositive(string name, int value)
        {
            if (value <= 0)
                throw AppException.Validation(new Dictionary<string, string[]> { [name] = new[] { $"{name} must be greater than 0." } });
        }

        // Keep FK mapping for better 404s on deletes/updates
        private static bool IsSqlForeignKeyViolation(Exception? ex)
        {
            while (ex != null)
            {
                if (ex is SqlException se && se.Number == 547) return true;
                ex = ex.InnerException;
            }
            return false;
        }

        private async Task<Cart> EnsureActiveCartAsync(long cartId, CancellationToken ct = default)
        {
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.Id == cartId && c.IsActive, ct);
            if (cart == null)
                throw AppException.NotFound("cart", $"Cart {cartId} not found or inactive.");
            return cart;
        }

        private async Task RecalculateTotalsSafeAsync(long cartId, CancellationToken ct = default)
        {
            try
            {
                await _SP.SP_Cart_RecalculateTotalsAsync(cartId, cancellationToken: ct);
            }
            catch (SqlException ex)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to recalculate cart totals.",
                    "urn:problem:cart:db.recalculate",
                    extensions: new Dictionary<string, object?>
                    {
                        ["sqlErrorNumber"] = ex.Number,
                        ["cartId"] = cartId
                    },
                    innerException: ex);
            }
        }

        // Adds an item to the cart, creating the cart if needed.
        // Note: basic input validation is handled at the API boundary via FluentValidation.
        public async Task<Cart> AddItem(long UserId, CartItem item)
        {
            // 1) Resolve/ensure cart (and ownership if cartId provided)
            Cart cart;

            if (item.CartId > 0)
            {
                cart = await EnsureActiveCartAsync(item.CartId);
                if (cart.UserId != UserId)
                    throw AppException.Business("cart.mismatch", $"Cart {item.CartId} does not belong to the user.");
            }
            else
            {
                cart = await _db.Carts
                                .Include(c => c.CartItems)
                                .FirstOrDefaultAsync(x => x.UserId == UserId && x.IsActive);

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
                        var ri = new RegionInfo(ci.LCID);
                        countryCode = string.IsNullOrWhiteSpace(ri.TwoLetterISORegionName) ? countryCode : ri.TwoLetterISORegionName;
                        currencyCode = string.IsNullOrWhiteSpace(ri.ISOCurrencySymbol) ? currencyCode : ri.ISOCurrencySymbol;
                    }
                    catch
                    {
                        // keep fallbacks
                    }

                    try
                    {
                        var createRes = await _SP.SP_Cart_CreateAsync(
                            userId: UserId,
                            currencyCode: currencyCode,
                            channel: "WEB",
                            anonymousId: null,
                            locale: locale,
                            countryCode: countryCode);

                        var newCartId = createRes.FirstOrDefault()?.CartId ?? 0L;
                        if (newCartId <= 0)
                        {
                            throw new AppException(
                                StatusCodes.Status500InternalServerError,
                                "Cart creation failed",
                                "Failed to create a new cart.",
                                "urn:problem:cart:create.failed",
                                extensions: new Dictionary<string, object?> { ["userId"] = UserId });
                        }

                        cart = await _db.Carts.FirstAsync(c => c.Id == newCartId);
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        throw new AppException(
                            StatusCodes.Status409Conflict,
                            "Concurrency conflict",
                            "The cart was modified by another process. Retry the operation.",
                            "urn:problem:cart:concurrency",
                            innerException: ex);
                    }
                    // Note: SP is idempotent; duplicates won’t surface as 2601/2627 anymore.
                    catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
                    {
                        throw new AppException(
                            StatusCodes.Status500InternalServerError,
                            "Database error",
                            "An error occurred while creating the cart.",
                            "urn:problem:cart:db.create",
                            innerException: ex);
                    }
                }
            }

            // 2) If the same ItemId already exists, bump qty; else add
            var existing = await _db.CartItems.FirstOrDefaultAsync(x => x.CartId == cart.Id && x.ItemId == item.ItemId);

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                if (existing != null)
                {
                    var newQty = existing.Quantity + item.Quantity;
                    EnsurePositive("quantity", newQty); // backstop invariant
                    await _SP.SP_Cart_UpdateItemQuantityAsync(existing.Id, newQty);
                }
                else
                {
                    // backstop invariant when adding new line
                    EnsurePositive(nameof(item.Quantity), item.Quantity);

                    await _SP.SP_Cart_AddItemAsync(
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

                await RecalculateTotalsSafeAsync(cart.Id);
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync();
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The cart was modified by another process. Retry the operation.",
                    "urn:problem:cart:concurrency",
                    innerException: ex);
            }
            // Note: SPs are idempotent; unique collisions won’t be thrown. Keep FK mapping for safety.
            catch (DbUpdateException ex) when (IsSqlForeignKeyViolation(ex))
            {
                await tx.RollbackAsync();
                throw AppException.NotFound("cart.item", "One of the referenced entities no longer exists.");
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                await tx.RollbackAsync();
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "An unexpected database error occurred while adding the item.",
                    "urn:problem:cart:db.additem",
                    innerException: ex);
            }

            // 3) Return a fresh cart (with items)
            cart = await _db.Carts.Include(c => c.CartItems).FirstAsync(c => c.Id == cart.Id);
            return cart;
        }

        // Deletes an item from the cart (uses SP_Cart_RemoveItem).
        public async Task<int> DeleteItem(long CartId, int ItemId)
        {
            await EnsureActiveCartAsync(CartId);

            var cartItemId = await _db.CartItems
                                      .Where(x => x.CartId == CartId && x.ItemId == ItemId)
                                      .Select(x => (int?)x.Id)
                                      .FirstOrDefaultAsync();

            if (cartItemId == null)
                throw AppException.NotFound("cart.item", $"Item {ItemId} not found in cart {CartId}");

            try
            {
                await _SP.SP_Cart_RemoveItemAsync(cartItemId.Value);
                await RecalculateTotalsSafeAsync(CartId);
                return 1;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The cart was modified by another process. Retry the operation.",
                    "urn:problem:cart:concurrency",
                    innerException: ex);
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "An unexpected database error occurred while deleting the item.",
                    "urn:problem:cart:db.deleteitem",
                    innerException: ex);
            }
        }

        // Retrieves a cart by CartId, including its items, if it is active.
        public async Task<Cart> GetCart(long CartId)
        {
            return await _db.Carts
                            .Include(c => c.CartItems)
                            .FirstOrDefaultAsync(c => c.Id == CartId && c.IsActive);
        }

        // Gets the total count of items in the user's active cart.
        public async Task<int> GetCartItemCount(long UserId)
        {
            var cartId = await _db.Carts
                                  .Where(c => c.UserId == UserId && c.IsActive)
                                  .Select(c => (long?)c.Id)
                                  .FirstOrDefaultAsync();

            if (!cartId.HasValue)
                return 0;

            // Always count actual items - this is the source of truth
            var itemCount = await _db.CartItems
                                     .Where(ci => ci.CartId == cartId.Value)
                                     .SumAsync(ci => (int?)ci.Quantity);

            return itemCount ?? 0;
        }

        // Retrieves all items in a cart by CartId.
        public async Task<IEnumerable<CartItem>> GetCartItems(long CartId)
        {
            return await _db.CartItems
                            .Where(c => c.CartId == CartId)
                            .ToListAsync();
        }

        // Retrieves the user's active cart, including its items.
        public async Task<Cart> GetUserCart(long UserId)
        {
            return await _db.Carts
                            .Include(c => c.CartItems)
                            .FirstOrDefaultAsync(c => c.UserId == UserId && c.IsActive);
        }

        // Marks a cart as inactive.
        public async Task<bool> MakeInActive(long CartId)
        {
            var cart = await _db.Carts.FindAsync(CartId);
            if (cart != null)
            {
                if (!cart.IsActive)
                    throw AppException.Business("cart.already.inactive", $"Cart {CartId} is already inactive.");

                cart.IsActive = false;
                try
                {
                    await _db.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    throw new AppException(
                        StatusCodes.Status409Conflict,
                        "Concurrency conflict",
                        "The cart was modified by another process. Retry the operation.",
                        "urn:problem:cart:concurrency",
                        innerException: ex);
                }
                catch (DbUpdateException ex)
                {
                    throw new AppException(
                        StatusCodes.Status500InternalServerError,
                        "Database error",
                        "Failed to set the cart inactive.",
                        "urn:problem:cart:db.inactive",
                        innerException: ex);
                }
            }
            return false;
        }

        // Updates the quantity of a specific item (by CartId + ItemId).
        public async Task<int> UpdateQuantity(long CartId, int ItemId, int Quantity)
        {
            await EnsureActiveCartAsync(CartId);

            var cartItem = await _db.CartItems
                                    .FirstOrDefaultAsync(x => x.CartId == CartId && x.ItemId == ItemId);
            if (cartItem == null)
                throw AppException.NotFound("cart.item", $"Item {ItemId} not found in cart {CartId}");

            var newQty = cartItem.Quantity + Quantity;
            EnsurePositive("quantity", newQty); // backstop invariant (no zero/negative inventory)

            try
            {
                await _SP.SP_Cart_UpdateItemQuantityAsync(cartItem.Id, newQty);
                await RecalculateTotalsSafeAsync(CartId);
                return 1;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The cart was modified by another process. Retry the operation.",
                    "urn:problem:cart:concurrency",
                    innerException: ex);
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "An unexpected database error occurred while updating the quantity.",
                    "urn:problem:cart:db.updateqty",
                    innerException: ex);
            }
        }

        #region Item Options (EF-backed)

        public async Task<int> AddItemOptionAsync(int cartItemId, string name, string value, CancellationToken ct = default)
        {
            // Boundary validation is handled by validators; enforce persistence invariants here.
            var exists = await _db.CartItems.AnyAsync(ci => ci.Id == cartItemId, ct);
            if (!exists)
                throw AppException.NotFound("cart.item", $"CartItem {cartItemId} not found");

            var opt = new CartItemOption { CartItemId = cartItemId, Name = name, Value = value };
            _db.CartItemOptions.Add(opt);

            try
            {
                return await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The item option was modified by another process. Retry the operation.",
                    "urn:problem:cart:concurrency",
                    innerException: ex);
            }
            // Inline unique detection (no helper) because this path is EF-backed
            catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException se && (se.Number == 2601 || se.Number == 2627))
            {
                throw AppException.Business("cart.item.option.duplicate", "An identical option already exists for this item.");
            }
            catch (DbUpdateException ex) when (IsSqlForeignKeyViolation(ex))
            {
                throw AppException.NotFound("cart.item", "The associated cart item no longer exists.");
            }
            catch (DbUpdateException ex)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to add the item option.",
                    "urn:problem:cart:db.addoption",
                    innerException: ex);
            }
        }

        public async Task<int> RemoveItemOptionAsync(int cartItemOptionId, CancellationToken ct = default)
        {
            var opt = await _db.CartItemOptions.FindAsync(new object?[] { cartItemOptionId }, ct);
            if (opt == null) return 0;

            _db.CartItemOptions.Remove(opt);

            try
            {
                return await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The item option was modified by another process. Retry the operation.",
                    "urn:problem:cart:concurrency",
                    innerException: ex);
            }
            catch (DbUpdateException ex)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to remove the item option.",
                    "urn:problem:cart:db.removeoption",
                    innerException: ex);
            }
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
            await EnsureActiveCartAsync(cartId, ct);

            try
            {
                await _SP.SP_Cart_ApplyCouponAsync(cartId, code, description, amount, "PromotionService", cancellationToken: ct);
                await RecalculateTotalsSafeAsync(cartId, ct);
            }
            // Note: SP is idempotent; duplicate coupon will not throw unique violations anymore.
            catch (DbUpdateException ex) when (IsSqlForeignKeyViolation(ex))
            {
                throw AppException.NotFound("cart", $"Cart {cartId} not found or inactive.");
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    $"Failed to apply coupon '{code}'.",
                    "urn:problem:cart:db.applycoupon",
                    innerException: ex);
            }
        }

        public async Task RemoveCouponAsync(long cartId, string code, CancellationToken ct = default)
        {
            await EnsureActiveCartAsync(cartId, ct);

            try
            {
                await _SP.SP_Cart_RemoveCouponAsync(cartId, code, cancellationToken: ct);
                await RecalculateTotalsSafeAsync(cartId, ct);
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    $"Failed to remove coupon '{code}'.",
                    "urn:problem:cart:db.removecoupon",
                    innerException: ex);
            }
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
            await EnsureActiveCartAsync(cartId, ct);

            if (cartItemId.HasValue)
            {
                var itemExists = await _db.CartItems.AnyAsync(i => i.Id == cartItemId.Value && i.CartId == cartId, ct);
                if (!itemExists)
                    throw AppException.NotFound("cart.item", $"CartItem {cartItemId.Value} not found in cart {cartId}");
            }

            try
            {
                await _SP.SP_Cart_AddAdjustmentAsync(cartId, cartItemId, type, description, amount, cancellationToken: ct);
                await RecalculateTotalsSafeAsync(cartId, ct);
                return 1;
            }
            catch (DbUpdateException ex) when (IsSqlForeignKeyViolation(ex))
            {
                throw AppException.NotFound("cart", $"Cart {cartId} not found.");
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to add adjustment.",
                    "urn:problem:cart:db.addadjustment",
                    innerException: ex);
            }
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
            await EnsureActiveCartAsync(cartId, ct);

            try
            {
                await _SP.SP_Cart_SelectShippingMethodAsync(cartId, carrier, methodCode, methodName, cost, estimatedDays, addressSnapshotJson, cancellationToken: ct);
                await RecalculateTotalsSafeAsync(cartId, ct);
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to select shipping method.",
                    "urn:problem:cart:db.shipping",
                    innerException: ex);
            }
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
            await EnsureActiveCartAsync(cartId, ct);

            try
            {
                await _SP.SP_Cart_SetPaymentAsync(cartId, method, amountAuthorized, currencyCode, status, cancellationToken: ct);
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to set payment.",
                    "urn:problem:cart:db.setpayment",
                    innerException: ex);
            }
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
            await EnsureActiveCartAsync(cartId, ct);

            try
            {
                await _SP.SP_Cart_ClearAsync(cartId, cancellationToken: ct);
                await RecalculateTotalsSafeAsync(cartId, ct);
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to clear the cart.",
                    "urn:problem:cart:db.clear",
                    innerException: ex);
            }
        }

        #endregion

        // Full cart snapshot (Cart, Items, Options, Coupons, Adjustments, Shipments, Taxes, Payments, Totals)
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
            => await _db.SavedForLaterItems.Where(x => x.CartId == cartId).AsNoTracking().ToListAsync(ct);

        public async Task SaveForLaterAsync(long cartId, int itemId, CancellationToken ct = default)
        {
            await EnsureActiveCartAsync(cartId, ct);

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

            try
            {
                await _db.SaveChangesAsync(ct);
                await RecalculateTotalsSafeAsync(cartId, ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The cart was modified by another process. Retry the operation.",
                    "urn:problem:cart:concurrency",
                    innerException: ex);
            }
            catch (DbUpdateException ex)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to move item to Save For Later.",
                    "urn:problem:cart:db.saveforlater",
                    innerException: ex);
            }
        }

        public async Task MoveSavedToCartAsync(int savedItemId, CancellationToken ct = default)
        {
            var sfl = await _db.SavedForLaterItems.FirstOrDefaultAsync(x => x.Id == savedItemId, ct);
            if (sfl == null)
                throw AppException.NotFound("saved.item", $"Saved item {savedItemId} not found");

            await EnsureActiveCartAsync(sfl.CartId, ct);

            var existing = await _db.CartItems.FirstOrDefaultAsync(x => x.CartId == sfl.CartId && x.ItemId == sfl.ItemId, ct);
            if (existing != null)
            {
                var newQty = existing.Quantity + sfl.Quantity;
                EnsurePositive("quantity", newQty);
                existing.Quantity = newQty;
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

            try
            {
                await _db.SaveChangesAsync(ct);
                await RecalculateTotalsSafeAsync(sfl.CartId, ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new AppException(
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The cart was modified by another process. Retry the operation.",
                    "urn:problem:cart:concurrency",
                    innerException: ex);
            }
            catch (DbUpdateException ex)
            {
                throw new AppException(
                    StatusCodes.Status500InternalServerError,
                    "Database error",
                    "Failed to move saved item to cart.",
                    "urn:problem:cart:db.movesaved",
                    innerException: ex);
            }
        }

        #endregion
    }
}

