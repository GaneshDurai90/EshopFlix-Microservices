using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class TagRepository : ITagRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public TagRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Tag tag, CancellationToken ct = default)
        {
            await _dbContext.Tags.AddAsync(tag, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task AssignToProductAsync(int productId, IEnumerable<int> tagIds, CancellationToken ct = default)
        {
            var normalized = tagIds?.Distinct().ToArray() ?? System.Array.Empty<int>();

            var current = await _dbContext.Set<Dictionary<string, object>>("ProductTags")
                .Where(pt => EF.Property<int>(pt, "ProductId") == productId)
                .Select(pt => EF.Property<int>(pt, "TagId"))
                .ToListAsync(ct);

            var toRemove = current.Except(normalized).ToArray();
            var toAdd = normalized.Except(current).ToArray();

            if (toRemove.Length > 0)
            {
                var rows = _dbContext.Set<Dictionary<string, object>>("ProductTags")
                    .Where(pt => EF.Property<int>(pt, "ProductId") == productId && toRemove.Contains(EF.Property<int>(pt, "TagId")));
                _dbContext.RemoveRange(rows);
            }

            foreach (var tagId in toAdd)
            {
                var entry = new Dictionary<string, object>
                {
                    ["ProductId"] = productId,
                    ["TagId"] = tagId
                };
                _dbContext.Add(entry);
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Tag tag, CancellationToken ct = default)
        {
            _dbContext.Tags.Remove(tag);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbContext.Tags
                .OrderBy(t => t.Name)
                .ToListAsync(ct);
        }

        public Task<Tag?> GetByIdAsync(int tagId, CancellationToken ct = default)
        {
            return _dbContext.Tags.FirstOrDefaultAsync(t => t.TagId == tagId, ct);
        }

        public Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            slug = slug ?? string.Empty;
            return _dbContext.Tags.FirstOrDefaultAsync(t => t.Slug == slug, ct);
        }

        public async Task<(IReadOnlyList<Tag> Items, int TotalCount)> SearchAsync(string? term, int page, int pageSize, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var query = _dbContext.Tags.Include(t => t.Products).AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowered = term.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(lowered) || t.Slug.ToLower().Contains(lowered));
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task UpdateAsync(Tag tag, CancellationToken ct = default)
        {
            _dbContext.Tags.Update(tag);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null, CancellationToken ct = default)
        {
            slug = slug ?? string.Empty;
            return await _dbContext.Tags.AnyAsync(t => t.Slug == slug && (!excludeId.HasValue || t.TagId != excludeId.Value), ct);
        }
    }
}
