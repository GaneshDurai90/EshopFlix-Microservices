using System;
using System.Collections.Generic;

namespace eShopFlix.Web.Models
{
    public sealed class CatalogIndexViewModel
    {
        public IReadOnlyList<ProductCardViewModel> Products { get; init; } = Array.Empty<ProductCardViewModel>();
        public IReadOnlyList<PromotionSummaryModel> Promotions { get; init; } = Array.Empty<PromotionSummaryModel>();
        public string? SearchTerm { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }

        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);

        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;
    }

    public sealed class ProductCardViewModel
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? CategoryName { get; init; }
        public string? ImageUrl { get; init; }
        public decimal? Price { get; init; }
        public string Currency { get; init; } = "INR";
        public string? Description { get; init; }
        public byte Status { get; init; }
        public bool IsSearchable { get; init; }
        public DateTime CreatedDate { get; init; }
    }
}
