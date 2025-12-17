using System;

namespace CatalogService.Application.DTO
{
    public sealed class ProductReviewListItemDto
    {
        public int ReviewId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public byte Rating { get; set; }
        public string? Title { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
