namespace CatalogService.Application.DTO
{
    public sealed class ManufacturerDto
    {
        public int ManufacturerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
    }
}
