using CartService.Application.DTOs;
using CartService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Application.Services.Abstractions
{
    public interface ICartAppService
    {
        Task<CartDTO> GetUserCart(long UserId);
        Task<int> GetCartItemCount(long UserId);
        Task<IEnumerable<CartItemDTO>> GetCartItems(long CartId);
        Task<CartDTO> GetCart(int CartId);
        Task<CartDTO> AddItem(long UserId, CartItem item);
        Task<int> DeleteItem(int CartId, int ItemId);
        Task<bool> MakeInActive(int CartId);
        Task<int> UpdateQuantity(int CartId, int ItemId, int Quantity);
        // Item options
        Task<int> AddItemOptionAsync(int cartItemId, string name, string value, CancellationToken ct = default);
        Task<int> RemoveItemOptionAsync(int cartItemOptionId, CancellationToken ct = default);

        // Coupons
        Task ApplyCouponAsync(long cartId, string code, decimal amount, string? description = null, CancellationToken ct = default);
        Task RemoveCouponAsync(long cartId, string code, CancellationToken ct = default);

        // Adjustments
        Task<int> AddAdjustmentAsync(long cartId, int? cartItemId, string type, string description, decimal amount, CancellationToken ct = default);

        // Shipping
        Task SelectShippingAsync(long cartId, string carrier, string methodCode, string methodName,
                                 decimal cost, int? estimatedDays, string? addressSnapshotJson, CancellationToken ct = default);

        // Taxes & Totals
        Task RecalculateTotalsAsync(long cartId, CancellationToken ct = default);
        Task<CartTotal?> GetTotalsAsync(long cartId, CancellationToken ct = default);

        // Payments
        Task SetPaymentAsync(long cartId, string method, decimal amountAuthorized, string currencyCode, string status, CancellationToken ct = default);

        // Clear cart
        Task ClearAsync(long cartId, CancellationToken ct = default);

        Task<CartSnapshotDto> GetSnapshotAsync(long cartId, CancellationToken ct = default);


        // Reads (optional helpers)
        Task<IReadOnlyList<CartItemOption>> GetItemOptionsAsync(int cartItemId, CancellationToken ct = default);
        Task<IReadOnlyList<CartCoupon>> GetCouponsAsync(long cartId, CancellationToken ct = default);
        Task<IReadOnlyList<CartAdjustment>> GetAdjustmentsAsync(long cartId, CancellationToken ct = default);
        Task<IReadOnlyList<CartShipment>> GetShipmentsAsync(long cartId, CancellationToken ct = default);
        Task<IReadOnlyList<CartTaxis>> GetTaxesAsync(long cartId, CancellationToken ct = default);
        Task<IReadOnlyList<CartPayment>> GetPaymentsAsync(long cartId, CancellationToken ct = default);

        // Save for later
        Task<IReadOnlyList<SavedForLaterItem>> GetSavedForLaterAsync(long cartId, CancellationToken ct = default);
        Task SaveForLaterAsync(long cartId, int itemId, CancellationToken ct = default);
        Task MoveSavedToCartAsync(int savedItemId, CancellationToken ct = default);
    }
}
