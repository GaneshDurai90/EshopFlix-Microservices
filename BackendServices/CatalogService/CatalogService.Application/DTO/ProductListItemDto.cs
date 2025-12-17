using System;

namespace CatalogService.Application.DTO
{
    public class ProductListItemDto
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
    }
}
