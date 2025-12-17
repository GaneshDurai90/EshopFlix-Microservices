using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductVariants.Commands;
using CatalogService.Application.ProductVariants.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IProductVariantAppService
    {
        Task<ProductVariantDto> CreateAsync(CreateProductVariantCommand command, CancellationToken ct = default);
        Task<ProductVariantDto> UpdateAsync(UpdateProductVariantCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteProductVariantCommand command, CancellationToken ct = default);
        Task<ProductVariantDto> SetDefaultAsync(SetDefaultProductVariantCommand command, CancellationToken ct = default);
        Task<ProductVariantDto?> GetByIdAsync(int skuId, CancellationToken ct = default);
        Task<IReadOnlyList<ProductVariantListItemDto>> GetByProductAsync(int productId, CancellationToken ct = default);
    }
}
