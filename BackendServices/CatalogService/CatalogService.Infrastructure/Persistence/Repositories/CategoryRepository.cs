using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class CategoryRepository : ICategoryRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public CategoryRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Category category, CancellationToken ct = default)
        {
            await _dbContext.Categories.AddAsync(category, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsWithSlugAsync(string slug, int? excludeCategoryId = null, CancellationToken ct = default)
        {
            slug = slug ?? string.Empty;
            return await _dbContext.Categories.AnyAsync(
                c => c.Slug == slug && (!excludeCategoryId.HasValue || c.CategoryId != excludeCategoryId.Value),
                ct);
        }

        public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbContext.Categories
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Category>> GetChildrenAsync(int parentCategoryId, CancellationToken ct = default)
        {
            return await _dbContext.Categories
                .Where(c => c.ParentCategoryId == parentCategoryId)
                .ToListAsync(ct);
        }

        public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
            => _dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == id, ct);

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => _dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == slug, ct);

        public async Task UpdateAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
