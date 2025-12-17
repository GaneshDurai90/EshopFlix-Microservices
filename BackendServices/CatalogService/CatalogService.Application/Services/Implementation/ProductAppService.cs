using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Products.Commands;
using CatalogService.Application.Products.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class ProductAppService : IProductAppService
    {
        private readonly IDispatcher _dispatcher;

        public ProductAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<ProductDetailDto> ChangeStatusAsync(ChangeProductStatusCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task<ProductDetailDto> CreateAsync(CreateProductCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteProductCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<ProductDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _dispatcher.Query(new GetProductByIdQuery { ProductId = id }, ct);

        public async Task<IEnumerable<ProductDTO>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
            => await _dispatcher.Query(new GetProductsByIdsQuery { ProductIds = ids?.ToArray() ?? Array.Empty<int>() }, ct);

        public async Task<PagedResult<ProductListItemDto>> SearchAsync(SearchProductsQuery query, CancellationToken ct = default)
            => await _dispatcher.Query(query, ct);

        public async Task<ProductDetailDto> UpdateAsync(UpdateProductCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
