using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record SelectShippingCommand(
        long CartId, string Carrier, string MethodCode, string MethodName, decimal Cost, int? EstimatedDays, string? AddressSnapshotJson) : ICommand<bool>;
}