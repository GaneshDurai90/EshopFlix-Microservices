using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductReviews.Commands
{
    public sealed class CreateProductReviewCommand : ICommand<ProductReviewDto>
    {
        public int ProductId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public byte Rating { get; init; }
        public string? Title { get; init; }
        public string Body { get; init; } = string.Empty;
    }
}
