using System;

namespace CatalogService.Application.DTO
{
    public sealed class ProductReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public byte Rating { get; set; }
        public string? Title { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
