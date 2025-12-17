using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Products.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Products.Handlers
{
    public sealed class GetProductsByIdsQueryHandler : IQueryHandler<GetProductsByIdsQuery, IEnumerable<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public GetProductsByIdsQueryHandler(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductDTO>> Handle(GetProductsByIdsQuery query, CancellationToken ct)
        {
            if (query.ProductIds == null || query.ProductIds.Count == 0)
            {
                return Enumerable.Empty<ProductDTO>();
            }

            var products = await _productRepository.GetByIdsAsync(query.ProductIds, ct);
            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }
    }
}
