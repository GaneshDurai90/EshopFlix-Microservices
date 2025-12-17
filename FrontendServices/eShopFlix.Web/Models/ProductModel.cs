using System;

namespace eShopFlix.Web.Models
{
    /// <summary>
    /// Mirrors the CatalogService product payload so gateway responses deserialize without data loss.
    /// </summary>
    public class ProductModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public int? BrandId { get; set; }
        public int? ManufacturerId { get; set; }
        public int? CategoryId { get; set; }
        public int? DefaultSkuId { get; set; }
        public decimal UnitPrice { get; set; }
        public byte Status { get; set; }
        public bool IsSearchable { get; set; }
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
