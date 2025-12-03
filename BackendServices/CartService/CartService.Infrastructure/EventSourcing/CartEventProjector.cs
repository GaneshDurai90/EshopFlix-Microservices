using CartService.Domain.Events;
using CartService.Infrastructure.Persistence;

namespace CartService.Infrastructure.EventSourcing
{
    public sealed class CartEventProjector
    {
        private readonly ICartServiceDbContextProcedures _sp;

        public CartEventProjector(ICartServiceDbContextProcedures sp) => _sp = sp;

        public async Task ApplyAsync(IDomainEvent evt, CancellationToken ct = default)
        {
            switch (evt)
            {
                case ItemAddedV1 e:
                    // No-op here; AddItem SP already applied by command flow.
                    // For replay-from-scratch you would call SP_Cart_AddItemAsync and recalc totals.
                    await _sp.SP_Cart_AddItemAsync(e.CartId, e.ItemId, e.Sku, e.ProductName, e.UnitPrice, e.Quantity, e.TaxCategory, e.ProductSnapshotJson, e.VariantJson, e.IsGift, e.ParentItemId, cancellationToken: ct);
                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case ItemQuantityUpdatedV1 e:
                    // Need CartItemId; projector could derive by querying DB. Kept minimal for brevity.

                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case ItemRemovedV1 e:
                    // Need CartItemId; similar note as above.
                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case CouponAppliedV1 e:
                    await _sp.SP_Cart_ApplyCouponAsync(e.CartId, e.Code, e.Description, e.Amount, "Replay", cancellationToken: ct);
                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case CouponRemovedV1 e:
                    await _sp.SP_Cart_RemoveCouponAsync(e.CartId, e.Code, cancellationToken: ct);
                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case ShippingSelectedV1 e:
                    await _sp.SP_Cart_SelectShippingMethodAsync(e.CartId, e.Carrier, e.MethodCode, e.MethodName, e.Cost, e.EstimatedDays, e.AddressSnapshotJson, cancellationToken: ct);
                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case TotalsRecalculatedV1 e:
                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case CartClearedV1 e:
                    await _sp.SP_Cart_ClearAsync(e.CartId, cancellationToken: ct);
                    await _sp.SP_Cart_RecalculateTotalsAsync(e.CartId, cancellationToken: ct);
                    break;

                case PaymentSetV1 e:
                    await _sp.SP_Cart_SetPaymentAsync(e.CartId, e.Method, e.AmountAuthorized, e.CurrencyCode, e.Status, cancellationToken: ct);
                    break;

                case CartDeactivatedV1 e:
                    await _sp.SP_Cart_UnlockAsync(e.CartId, cancellationToken: ct); // example operation
                    break;

                case CartSnapshotV1:
                    // Snapshots are read-optimized markers; projection no-op
                    break;
            }
        }
    }
}