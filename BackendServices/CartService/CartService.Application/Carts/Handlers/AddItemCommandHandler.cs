
using AutoMapper;
using CartService.Application.CQRS;
using CartService.Application.DTOs;
using CartService.Application.HttpClients;
using CartService.Application.Repositories;
using CartService.Domain.Events;
using CartService.Domain.Entities;
using CartService.Application.EventSourcing; // changed: now references application abstraction
using CartService.Application.Carts.Commands;

namespace CartService.Application.Carts.Handlers
{
    public sealed class AddItemCommandHandler : ICommandHandler<AddItemCommand, CartDTO>
    {
        private readonly ICartRepository _repo;
        private readonly IEventStore _eventStore;
        private readonly CatalogServiceClient _catalog;
        private readonly IMapper _mapper;

        public AddItemCommandHandler(
            ICartRepository repo,
            IEventStore eventStore,
            CatalogServiceClient catalog,
            IMapper mapper)
        {
            _repo = repo;
            _eventStore = eventStore;
            _catalog = catalog;
            _mapper = mapper;
        }

        public async Task<CartDTO> Handle(AddItemCommand command, CancellationToken ct)
        {
            // Execute write via existing repository (keeps behavior + invariants).
            var cart = await _repo.AddItem(command.UserId, command.Item);

            // Append event(s) for audit/replay
            var evt = new ItemAddedV1(
                CartId: cart.Id,
                UserId: command.UserId,
                ItemId: command.Item.ItemId,
                UnitPrice: command.Item.UnitPrice,
                Quantity: command.Item.Quantity,
                Sku: command.Item.Sku,
                ProductName: command.Item.ProductName,
                TaxCategory: command.Item.TaxCategory,
                ProductSnapshotJson: command.Item.ProductSnapshotJson,
                VariantJson: command.Item.VariantJson,
                IsGift: command.Item.IsGift,
                ParentItemId: command.Item.ParentItemId,
                Version: 0,
                OccurredOnUtc: DateTime.UtcNow,
                CausedBy: "CartService");

            await _eventStore.AppendAsync(cart.Id, new[] { evt }, "CartService", ct);

            // Read model: prefer snapshot + enrich (mirrors your AppService behavior)
            var dto = await BuildDtoFromSnapshotAsync(cart.Id, ct);
            return dto!;
        }

        // helper mirrors BuildDtoFromSnapshotAsync from CartAppService
        private async Task<CartDTO?> BuildDtoFromSnapshotAsync(long cartId, CancellationToken ct)
        {
            // Use repository API to get full snapshot then enrich via Catalog
            var snap = await _repo.GetSnapshotAsync(cartId, ct);
            var cart = snap.Cart.FirstOrDefault();
            if (cart is null) return null;

            var dto = _mapper.Map<CartDTO>(cart);
            dto.CartItems = _mapper.Map<List<CartItemDTO>>(snap.Items);

            if (dto.CartItems?.Count > 0)
            {
                var ids = snap.Items.Select(i => i.ItemId).Distinct().ToArray();
                var products = await _catalog.GetByIdsAsync(ids, ct);

                foreach (var line in dto.CartItems)
                {
                    var p = products.FirstOrDefault(x => x.ProductId == line.ItemId);
                    if (p != null)
                    {
                        line.Name = p.Name;
                        line.ImageUrl = p.ImageUrl;
                    }
                }
            }

            var totals = snap.Totals.FirstOrDefault();
            if (totals != null)
            {
                dto.Total = totals.Subtotal;
                dto.Tax = totals.TaxTotal;
                dto.GrandTotal = totals.GrandTotal;
            }
            else
            {
                dto.Total = snap.Items.Sum(i => i.UnitPrice * i.Quantity);
                dto.Tax = snap.Taxes.Sum(t => t.Amount);
                var shipping = snap.Shipments.Where(s => s.IsSelected).Sum(s => s.Cost);
                var discount = snap.Coupons.Sum(c => c.DiscountAmount) +
                               snap.Adjustments.Where(a => a.Amount < 0).Sum(a => a.Amount);
                dto.GrandTotal = dto.Total + dto.Tax + shipping + discount;
            }

            return dto;
        }
    }
}
