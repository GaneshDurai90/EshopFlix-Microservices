using System;

namespace eShopFlix.Web.Models
{
    public sealed class ProductReviewModel
    {
        public int ReviewId { get; init; }
        public int ProductId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public byte Rating { get; init; }
        public string? Title { get; init; }
        public string Body { get; init; } = string.Empty;
        public bool IsPublished { get; init; }
        public DateTime CreatedDate { get; init; }
    }
}
