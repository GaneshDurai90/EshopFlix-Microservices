using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Promotions.Queries
{
    public sealed class GetPromotionByIdQuery : IQuery<PromotionDto?>
    {
        public int PromotionId { get; init; }
    }
}
