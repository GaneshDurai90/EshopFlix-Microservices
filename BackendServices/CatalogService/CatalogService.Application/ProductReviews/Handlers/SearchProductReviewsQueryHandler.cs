using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductReviews.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductReviews.Handlers
{
    public sealed class SearchProductReviewsQueryHandler : IQueryHandler<SearchProductReviewsQuery, PagedResult<ProductReviewListItemDto>>
    {
        private readonly IProductReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public SearchProductReviewsQueryHandler(IProductReviewRepository reviewRepository, IMapper mapper)
        {
            _reviewRepository = reviewRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ProductReviewListItemDto>> Handle(SearchProductReviewsQuery query, CancellationToken ct)
        {
            var (items, total) = await _reviewRepository.SearchAsync(query.ProductId, query.IsPublished, query.Page, query.PageSize, ct);
            var mapped = _mapper.Map<IReadOnlyList<ProductReviewListItemDto>>(items);
            return new PagedResult<ProductReviewListItemDto>(mapped, total, query.Page, query.PageSize);
        }
    }
}
