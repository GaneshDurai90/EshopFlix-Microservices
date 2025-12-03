using Contracts.DTOs;

namespace Contracts.Events.V1
{
    // Emitted when user finalizes cart and wants to checkout
    public record CartCheckedOutV1(
        Guid OrderId,
        long UserId,
        string Currency,
        decimal Total,
        IEnumerable<CartItemDto> Items,
        AddressDto? ShippingAddress,
        DateTime OccurredAt
    );

    // Emitted when a coupon is applied/removed successfully (optional)
    public record CartCouponAppliedV1(Guid CartId, long UserId, string Code, decimal DiscountAmount, DateTime OccurredAt);
    public record CartCouponRemovedV1(Guid CartId, long UserId, string Code, DateTime OccurredAt);
}
