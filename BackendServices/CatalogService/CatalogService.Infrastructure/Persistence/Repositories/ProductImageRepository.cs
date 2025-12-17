using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class ProductImageRepository : IProductImageRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public ProductImageRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ProductImage image, CancellationToken ct = default)
        {
            await _dbContext.ProductImages.AddAsync(image, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(ProductImage image, CancellationToken ct = default)
        {
            _dbContext.ProductImages.Remove(image);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<ProductImage?> GetByIdAsync(int imageId, CancellationToken ct = default)
        {
            return await _dbContext.ProductImages.FirstOrDefaultAsync(i => i.ProductImageId == imageId, ct);
        }

        public async Task<IReadOnlyList<ProductImage>> GetByProductAsync(int productId, CancellationToken ct = default)
        {
            return await _dbContext.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.ProductImageId)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(ProductImage image, CancellationToken ct = default)
        {
            _dbContext.ProductImages.Update(image);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
