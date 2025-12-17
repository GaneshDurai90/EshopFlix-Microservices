using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductRelationships.Commands;
using CatalogService.Application.ProductRelationships.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class ProductRelationshipAppService : IProductRelationshipAppService
    {
        private readonly IDispatcher _dispatcher;

        public ProductRelationshipAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<ProductRelationshipDto> AddAsync(AddProductRelationshipCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteProductRelationshipCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<IReadOnlyList<ProductRelationshipDto>> GetAsync(GetProductRelationshipsQuery query, CancellationToken ct = default)
            => await _dispatcher.Query(query, ct);

        public async Task<ProductRelationshipDto> UpdateAsync(UpdateProductRelationshipCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
