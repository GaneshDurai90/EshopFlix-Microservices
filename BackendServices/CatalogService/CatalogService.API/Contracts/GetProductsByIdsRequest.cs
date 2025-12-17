using System;
using System.Collections.Generic;

namespace CatalogService.API.Contracts
{
    public sealed class GetProductsByIdsRequest
    {
        public IReadOnlyCollection<int> Ids { get; init; } = Array.Empty<int>();
    }
}
