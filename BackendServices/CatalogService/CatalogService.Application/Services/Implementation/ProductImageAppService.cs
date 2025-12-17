using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductImages.Commands;
using CatalogService.Application.ProductImages.Queries;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class ProductImageAppService : IProductImageAppService
    {
        private readonly IDispatcher _dispatcher;

        public ProductImageAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<ProductImageDto> CreateAsync(CreateProductImageCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteProductImageCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<ProductImageDto?> GetByIdAsync(int imageId, CancellationToken ct = default)
            => await _dispatcher.Query(new GetProductImageByIdQuery { ProductImageId = imageId }, ct);

        public async Task<IReadOnlyList<ProductImageDto>> GetByProductAsync(int productId, CancellationToken ct = default)
            => await _dispatcher.Query(new GetProductImagesQuery { ProductId = productId }, ct);

        public async Task<ProductImageDto> SetPrimaryAsync(SetPrimaryProductImageCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task<ProductImageDto> UpdateAsync(UpdateProductImageCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
