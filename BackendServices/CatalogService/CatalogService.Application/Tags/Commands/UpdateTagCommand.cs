using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Tags.Commands
{
    public sealed class UpdateTagCommand : ICommand<TagDto>
    {
        public int TagId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
    }
}
