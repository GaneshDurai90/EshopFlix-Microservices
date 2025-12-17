using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class ProductVariantRepository : IProductVariantRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public ProductVariantRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ProductVariant variant, CancellationToken ct = default)
        {
            await _dbContext.ProductVariants.AddAsync(variant, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(ProductVariant variant, CancellationToken ct = default)
        {
            _dbContext.ProductVariants.Remove(variant);
            await _dbContext.SaveChangesAsync(ct);
        }

        public Task<bool> ExistsSkuAsync(string sku, int? excludeSkuId = null, CancellationToken ct = default)
        {
            sku = sku ?? string.Empty;
            return _dbContext.ProductVariants.AnyAsync(v => v.Sku == sku && (!excludeSkuId.HasValue || v.SkuId != excludeSkuId.Value), ct);
        }

        public async Task<ProductVariant?> GetByIdAsync(int skuId, CancellationToken ct = default)
        {
            return await _dbContext.ProductVariants.FirstOrDefaultAsync(v => v.SkuId == skuId, ct);
        }

        public async Task<IReadOnlyList<ProductVariant>> GetByProductAsync(int productId, CancellationToken ct = default)
        {
            return await _dbContext.ProductVariants
                .Where(v => v.ProductId == productId)
                .OrderByDescending(v => v.IsDefault)
                .ThenBy(v => v.Sku)
                .ToListAsync(ct);
        }

        public async Task<ProductVariant?> GetDefaultAsync(int productId, CancellationToken ct = default)
        {
            return await _dbContext.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == productId && v.IsDefault, ct);
        }

        public async Task UpdateAsync(ProductVariant variant, CancellationToken ct = default)
        {
            _dbContext.ProductVariants.Update(variant);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
