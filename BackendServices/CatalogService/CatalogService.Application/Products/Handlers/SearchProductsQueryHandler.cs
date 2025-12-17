using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Products.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Products.Handlers
{
    public sealed class SearchProductsQueryHandler : IQueryHandler<SearchProductsQuery, PagedResult<ProductListItemDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public SearchProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ProductListItemDto>> Handle(SearchProductsQuery query, CancellationToken ct)
        {
            var (items, total) = await _productRepository.SearchAsync(query.Term, query.CategoryId, query.Status, query.Page, query.PageSize, ct);
            var mapped = _mapper.Map<IReadOnlyList<ProductListItemDto>>(items);
            return new PagedResult<ProductListItemDto>(mapped, total, query.Page, query.PageSize);
        }
    }
}
