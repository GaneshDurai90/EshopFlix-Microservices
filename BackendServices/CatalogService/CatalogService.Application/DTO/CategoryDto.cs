namespace CatalogService.Application.DTO
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
