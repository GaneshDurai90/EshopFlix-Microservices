using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface IProductVariantRepository
    {
        Task<ProductVariant?> GetByIdAsync(int skuId, CancellationToken ct = default);
        Task<ProductVariant?> GetDefaultAsync(int productId, CancellationToken ct = default);
        Task<IReadOnlyList<ProductVariant>> GetByProductAsync(int productId, CancellationToken ct = default);
        Task<bool> ExistsSkuAsync(string sku, int? excludeSkuId = null, CancellationToken ct = default);
        Task AddAsync(ProductVariant variant, CancellationToken ct = default);
        Task UpdateAsync(ProductVariant variant, CancellationToken ct = default);
        Task DeleteAsync(ProductVariant variant, CancellationToken ct = default);
    }
}
