using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductImages.Commands;
using CatalogService.Application.ProductImages.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IProductImageAppService
    {
        Task<ProductImageDto> CreateAsync(CreateProductImageCommand command, CancellationToken ct = default);
        Task<ProductImageDto> UpdateAsync(UpdateProductImageCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteProductImageCommand command, CancellationToken ct = default);
        Task<ProductImageDto> SetPrimaryAsync(SetPrimaryProductImageCommand command, CancellationToken ct = default);
        Task<IReadOnlyList<ProductImageDto>> GetByProductAsync(int productId, CancellationToken ct = default);
        Task<ProductImageDto?> GetByIdAsync(int imageId, CancellationToken ct = default);
    }
}
