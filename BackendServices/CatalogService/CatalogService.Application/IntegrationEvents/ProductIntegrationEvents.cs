namespace CatalogService.Application.IntegrationEvents
{
    public record ProductCreatedIntegrationEvent(int ProductId, string Name, string Slug, int? CategoryId);
    public record ProductUpdatedIntegrationEvent(int ProductId, string Name, string Slug, int? CategoryId);
    public record ProductDeletedIntegrationEvent(int ProductId, string Name, string Slug);
}
