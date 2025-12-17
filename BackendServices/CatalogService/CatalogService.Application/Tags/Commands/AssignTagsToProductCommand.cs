using System.Collections.Generic;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Tags.Commands
{
    public sealed class AssignTagsToProductCommand : ICommand<ProductDetailDto>
    {
        public int ProductId { get; init; }
        public IReadOnlyCollection<int> TagIds { get; init; } = System.Array.Empty<int>();
    }
}
