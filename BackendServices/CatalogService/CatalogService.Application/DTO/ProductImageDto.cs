using System;

namespace CatalogService.Application.DTO
{
    public sealed class ProductImageDto
    {
        public int ProductImageId { get; set; }
        public int ProductId { get; set; }
        public int? SkuId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
