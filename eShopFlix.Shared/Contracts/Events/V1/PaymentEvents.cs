namespace Contracts.Events.V1
{
    public record PaymentAuthorizedV1(Guid PaymentId, Guid OrderId, decimal Amount, string Currency, string Status, DateTime OccurredAt);
    public record PaymentCapturedV1(Guid PaymentId, Guid OrderId, decimal Amount, string Currency, string Status, DateTime OccurredAt);
    public record PaymentFailedV1(Guid PaymentId, Guid OrderId, string Reason, DateTime OccurredAt);
}
