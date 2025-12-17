namespace CatalogService.Application.DTO
{
    public sealed class ManufacturerListItemDto
    {
        public int ManufacturerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
    }
}
