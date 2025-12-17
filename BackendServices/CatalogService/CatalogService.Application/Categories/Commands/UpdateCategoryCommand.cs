using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Categories.Commands
{
    public sealed class UpdateCategoryCommand : ICommand<CategoryDto>
    {
        public int CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int? ParentCategoryId { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; } = true;
    }
}
