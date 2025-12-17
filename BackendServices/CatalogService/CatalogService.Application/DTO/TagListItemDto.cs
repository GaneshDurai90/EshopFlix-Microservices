namespace CatalogService.Application.DTO
{
    public sealed class TagListItemDto
    {
        public int TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}
