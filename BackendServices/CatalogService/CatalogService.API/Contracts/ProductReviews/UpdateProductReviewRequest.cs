namespace CatalogService.API.Contracts.ProductReviews
{
    public sealed class UpdateProductReviewRequest
    {
        public byte Rating { get; set; }
        public string? Title { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
    }
}
