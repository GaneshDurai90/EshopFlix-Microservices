using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Products.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Products.Handlers
{
    public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDetailDto?>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<ProductDetailDto?> Handle(GetProductByIdQuery query, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(query.ProductId, ct);
            return product is null ? null : _mapper.Map<ProductDetailDto>(product);
        }
    }
}
