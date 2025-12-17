using System;
using System.Collections.Generic;

namespace eShopFlix.Web.Models
{
    public sealed class PagedResponseModel<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }

        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);

        public bool HasMore => Page < TotalPages;
    }
}
