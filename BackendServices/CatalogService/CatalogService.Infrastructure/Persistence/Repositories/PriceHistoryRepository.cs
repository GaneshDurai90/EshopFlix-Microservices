using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using PriceHistoryEntity = CatalogService.Domain.Entities.PriceHistory;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class PriceHistoryRepository : IPriceHistoryRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public PriceHistoryRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(IReadOnlyList<PriceHistoryEntity> Items, int TotalCount)> GetHistoryAsync(int? productId, int? skuId, int page, int pageSize, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 50 : pageSize;

            var query = _dbContext.PriceHistories.AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(p => p.ProductId == productId);
            }

            if (skuId.HasValue)
            {
                query = query.Where(p => p.SkuId == skuId);
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.ChangedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task RecordAsync(PriceHistoryEntity entry, CancellationToken ct = default)
        {
            await _dbContext.PriceHistories.AddAsync(entry, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
