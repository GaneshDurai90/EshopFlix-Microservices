using Contracts.DTOs;


namespace Contracts.Events.V1
{
    public record OrderPlacedV1(
        Guid OrderId,
        long UserId,
        IEnumerable<OrderLineDto> Lines,
        decimal TotalAmount,
        string Currency,
        AddressDto ShippingAddress,
        DateTime OccurredAt
    );

    public record OrderConfirmedV1(Guid OrderId, DateTime OccurredAt);
    public record OrderCancelledV1(Guid OrderId, string Reason, DateTime OccurredAt);
}
