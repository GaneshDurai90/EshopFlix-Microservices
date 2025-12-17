namespace CatalogService.Application.IntegrationEvents
{
    public record CategoryCreatedIntegrationEvent(int CategoryId, string Name, string Slug, int? ParentCategoryId);
    public record CategoryUpdatedIntegrationEvent(int CategoryId, string Name, string Slug, int? ParentCategoryId);
    public record CategoryDeletedIntegrationEvent(int CategoryId, string Name, string Slug);
}
