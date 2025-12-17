using CatalogService.Application.CQRS;

namespace CatalogService.Application.ProductReviews.Commands
{
    public sealed class DeleteProductReviewCommand : ICommand<bool>
    {
        public int ReviewId { get; init; }
    }
}
