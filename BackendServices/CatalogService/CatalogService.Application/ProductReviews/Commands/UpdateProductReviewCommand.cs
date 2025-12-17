using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductReviews.Commands
{
    public sealed class UpdateProductReviewCommand : ICommand<ProductReviewDto>
    {
        public int ReviewId { get; init; }
        public byte Rating { get; init; }
        public string? Title { get; init; }
        public string Body { get; init; } = string.Empty;
        public bool IsPublished { get; init; }
    }
}
