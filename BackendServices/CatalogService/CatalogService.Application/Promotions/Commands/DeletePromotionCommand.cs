using CatalogService.Application.CQRS;

namespace CatalogService.Application.Promotions.Commands
{
    public sealed class DeletePromotionCommand : ICommand<bool>
    {
        public int PromotionId { get; init; }
    }
}
