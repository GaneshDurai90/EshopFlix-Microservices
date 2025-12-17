using System.Collections.Generic;

namespace CatalogService.Application.DTO
{
    public class CategoryNodeDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public List<CategoryNodeDto> Children { get; set; } = new();
    }
}
