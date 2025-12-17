using System.Collections.Generic;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Tags.Queries
{
    public sealed class GetAllTagsQuery : IQuery<IReadOnlyList<TagDto>>
    {
    }
}
