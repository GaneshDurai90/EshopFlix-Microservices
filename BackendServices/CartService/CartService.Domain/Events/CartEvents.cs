using System;

namespace CartService.Domain.Events
{
    public interface IDomainEvent
    {
        long CartId { get; }
        int Version { get; }
        DateTime OccurredOnUtc { get; }
        string? CausedBy { get; }
    }

    public abstract record CartEventBase(
        long CartId,
        int Version,
        DateTime OccurredOnUtc,
        string? CausedBy
    ) : IDomainEvent;

    // v1 events (add more as you refactor additional commands)
    public sealed record CartCreatedV1(long CartId, long UserId, string CurrencyCode, string Channel, string Locale, string CountryCode, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record ItemAddedV1(long CartId, long UserId, int ItemId, decimal UnitPrice, int Quantity, string? Sku, string? ProductName, string? TaxCategory, string? ProductSnapshotJson, string? VariantJson, bool IsGift, int? ParentItemId, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record ItemQuantityUpdatedV1(long CartId, int ItemId, int DeltaQuantity, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record ItemRemovedV1(long CartId, int ItemId, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record CouponAppliedV1(long CartId, string Code, decimal Amount, string? Description, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record CouponRemovedV1(long CartId, string Code, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record ShippingSelectedV1(long CartId, string Carrier, string MethodCode, string MethodName, decimal Cost, int? EstimatedDays, string? AddressSnapshotJson, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record TotalsRecalculatedV1(long CartId, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record CartClearedV1(long CartId, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record ItemSavedForLaterV1(long CartId, int ItemId, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record SavedItemMovedToCartV1(long CartId, int SavedItemId, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record PaymentSetV1(long CartId, string Method, decimal AmountAuthorized, string CurrencyCode, string Status, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    public sealed record CartDeactivatedV1(long CartId, int Version, DateTime OccurredOnUtc, string? CausedBy)
        : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);

    // Add snapshot event to support periodic snapshots without new tables
    public sealed record CartSnapshotV1(
        long CartId,
        string SnapshotJson, // serialized read model snapshot (totals, items summary, etc.)
        int Version,
        DateTime OccurredOnUtc,
        string? CausedBy
    ) : CartEventBase(CartId, Version, OccurredOnUtc, CausedBy);
}
