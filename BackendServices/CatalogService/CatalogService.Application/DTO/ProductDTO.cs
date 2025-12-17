using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogService.Application.DTO
{
    /// <summary>
    /// Lightweight product DTO used by external consumers (e.g., Cart and Web UI).
    /// Mirrors key fields from the Product aggregate without exposing navigation properties.
    /// </summary>
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int? CategoryId { get; set; }
        public string PrimaryImageUrl { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public byte Status { get; set; }
        public bool IsSearchable { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
