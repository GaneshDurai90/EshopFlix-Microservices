using Contracts.DTOs;

namespace Contracts.Events.V1
{
    // Emitted after successful reservation of inventory
    public record InventoryReservedV1(
        Guid ReservationId,
        Guid OrderId,                // correlate to Order / CartCheckedOut
        IEnumerable<CartItemDto> Lines,
        DateTime OccurredAt
    );

    // Emitted when inventory reservation is released (cancellation)
    public record InventoryReleasedV1(Guid ReservationId, Guid OrderId, DateTime OccurredAt);

    // Emitted when inventory is actually committed (order finalized)
    public record InventoryCommittedV1(Guid ReservationId, Guid OrderId, DateTime OccurredAt);
}
