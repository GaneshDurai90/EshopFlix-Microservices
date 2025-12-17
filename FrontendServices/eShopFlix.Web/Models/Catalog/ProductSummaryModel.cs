using System;

namespace eShopFlix.Web.Models
{
    public sealed class ProductSummaryModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public byte Status { get; set; }
        public bool IsSearchable { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string? PrimaryImageUrl { get; set; }

        // Enriched fields (filled from GetByIds response)
        public decimal? DefaultPrice { get; set; }
        public string Currency { get; set; } = "INR";
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
