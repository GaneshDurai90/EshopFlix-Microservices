namespace CatalogService.API.Contracts.Manufacturers
{
    public sealed class CreateManufacturerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
    }
}
