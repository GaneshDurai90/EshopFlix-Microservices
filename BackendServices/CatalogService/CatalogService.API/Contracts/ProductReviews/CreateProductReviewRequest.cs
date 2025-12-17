namespace CatalogService.API.Contracts.ProductReviews
{
    public sealed class CreateProductReviewRequest
    {
        public string UserId { get; set; } = string.Empty;
        public byte Rating { get; set; }
        public string? Title { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
