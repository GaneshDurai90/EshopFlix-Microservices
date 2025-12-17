using System.Collections.Generic;

namespace CatalogService.API.Contracts.Tags
{
    public sealed class AssignTagsRequest
    {
        public IReadOnlyCollection<int> TagIds { get; set; } = System.Array.Empty<int>();
    }
}
