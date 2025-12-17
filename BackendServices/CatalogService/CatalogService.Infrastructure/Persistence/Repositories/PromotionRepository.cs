using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class PromotionRepository : IPromotionRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public PromotionRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Promotion promotion, IEnumerable<int> productIds, CancellationToken ct = default)
        {
            await _dbContext.Promotions.AddAsync(promotion, ct);
            await _dbContext.SaveChangesAsync(ct);
            await UpdateProductLinksAsync(promotion.PromotionId, productIds, ct);
        }

        public async Task DeleteAsync(Promotion promotion, CancellationToken ct = default)
        {
            _dbContext.Promotions.Remove(promotion);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken ct = default)
        {
            code = code ?? string.Empty;
            return await _dbContext.Promotions.AnyAsync(p => p.Code == code && (!excludeId.HasValue || p.PromotionId != excludeId.Value), ct);
        }

        public async Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken ct = default)
        {
            return await _dbContext.Promotions
                .Include(p => p.Products)
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId, ct);
        }

        public async Task<(IReadOnlyList<Promotion> Items, int TotalCount)> SearchAsync(string? term, bool? isActive, int page, int pageSize, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var query = _dbContext.Promotions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowered = term.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowered) || p.Code.ToLower().Contains(lowered));
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task UpdateAsync(Promotion promotion, IEnumerable<int> productIds, CancellationToken ct = default)
        {
            _dbContext.Promotions.Update(promotion);
            await _dbContext.SaveChangesAsync(ct);
            await UpdateProductLinksAsync(promotion.PromotionId, productIds, ct);
        }

        private async Task UpdateProductLinksAsync(int promotionId, IEnumerable<int> productIds, CancellationToken ct)
        {
            if (productIds == null)
            {
                return;
            }

            var normalized = productIds.Distinct().ToArray();
            var current = await _dbContext.Set<Dictionary<string, object>>("ProductPromotions")
                .Where(pp => EF.Property<int>(pp, "PromotionId") == promotionId)
                .Select(pp => EF.Property<int>(pp, "ProductId"))
                .ToListAsync(ct);

            var toRemove = current.Except(normalized).ToArray();
            var toAdd = normalized.Except(current).ToArray();

            if (toRemove.Length > 0)
            {
                var rows = _dbContext.Set<Dictionary<string, object>>("ProductPromotions")
                    .Where(pp => EF.Property<int>(pp, "PromotionId") == promotionId && toRemove.Contains(EF.Property<int>(pp, "ProductId")));
                _dbContext.RemoveRange(rows);
            }

            foreach (var productId in toAdd)
            {
                var entry = new Dictionary<string, object>
                {
                    ["ProductId"] = productId,
                    ["PromotionId"] = promotionId
                };
                _dbContext.Add(entry);
            }

            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
