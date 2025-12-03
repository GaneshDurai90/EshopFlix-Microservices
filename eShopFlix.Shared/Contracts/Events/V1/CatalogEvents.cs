using Contracts.DTOs;
namespace Contracts.Events.V1
{
    // Published when product or price changes in Catalog service
    public record ProductUpdatedV1(
        int ProductId,
        string SKU,
        string Name,
        decimal Price,
        bool IsActive,
        ProductSnapshotDto Snapshot,
        DateTime OccurredAt
    );

    public record ProductDeletedV1(int ProductId, string SKU, DateTime OccurredAt);
}
