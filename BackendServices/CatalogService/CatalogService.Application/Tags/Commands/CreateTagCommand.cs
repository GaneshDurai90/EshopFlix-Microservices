using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Tags.Commands
{
    public sealed class CreateTagCommand : ICommand<TagDto>
    {
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
    }
}
