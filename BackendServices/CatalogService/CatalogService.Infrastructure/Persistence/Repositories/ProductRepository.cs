using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class ProductRepository : IProductRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public ProductRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Product product, CancellationToken ct = default)
        {
            await _dbContext.Products.AddAsync(product, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Product product, CancellationToken ct = default)
        {
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync(ct);
        }

        public Task<bool> ExistsWithSlugAsync(string slug, int? excludeProductId = null, CancellationToken ct = default)
        {
            slug = slug ?? string.Empty;
            return _dbContext.Products.AnyAsync(
                p => p.Slug == slug && (!excludeProductId.HasValue || p.ProductId != excludeProductId.Value),
                ct);
        }

        public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id, ct);
        }

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            slug = slug ?? string.Empty;
            return _dbContext.Products.FirstOrDefaultAsync(p => p.Slug == slug, ct);
        }

        public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            var idArray = ids?.Distinct().ToArray() ?? Array.Empty<int>();
            if (idArray.Length == 0)
            {
                return Array.Empty<Product>();
            }

            return await _dbContext.Products
                .Where(p => idArray.Contains(p.ProductId))
                .ToListAsync(ct);
        }

        public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
            string? term,
            int? categoryId,
            ProductStatus? status,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var query = _dbContext.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowered = term.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowered) || p.Slug.ToLower().Contains(lowered));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (status.HasValue)
            {
                var statusByte = (byte)status.Value;
                query = query.Where(p => p.Status == statusByte);
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task UpdateAsync(Product product, CancellationToken ct = default)
        {
            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
