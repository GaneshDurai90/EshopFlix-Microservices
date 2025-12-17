using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductVariants.Commands;
using CatalogService.Application.ProductVariants.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class ProductVariantAppService : IProductVariantAppService
    {
        private readonly IDispatcher _dispatcher;

        public ProductVariantAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<ProductVariantDto> CreateAsync(CreateProductVariantCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteProductVariantCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<ProductVariantDto?> GetByIdAsync(int skuId, CancellationToken ct = default)
            => await _dispatcher.Query(new GetProductVariantByIdQuery { SkuId = skuId }, ct);

        public async Task<IReadOnlyList<ProductVariantListItemDto>> GetByProductAsync(int productId, CancellationToken ct = default)
            => await _dispatcher.Query(new GetProductVariantsByProductQuery { ProductId = productId }, ct);

        public async Task<ProductVariantDto> SetDefaultAsync(SetDefaultProductVariantCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task<ProductVariantDto> UpdateAsync(UpdateProductVariantCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
