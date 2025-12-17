namespace CatalogService.API.Contracts.Tags
{
    public sealed class UpdateTagRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
