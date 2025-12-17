using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class ManufacturerRepository : IManufacturerRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public ManufacturerRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Manufacturer manufacturer, CancellationToken ct = default)
        {
            await _dbContext.Manufacturers.AddAsync(manufacturer, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Manufacturer manufacturer, CancellationToken ct = default)
        {
            _dbContext.Manufacturers.Remove(manufacturer);
            await _dbContext.SaveChangesAsync(ct);
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
        {
            return _dbContext.Manufacturers.AnyAsync(m => m.Name == name && (!excludeId.HasValue || m.ManufacturerId != excludeId.Value), ct);
        }

        public Task<Manufacturer?> GetByIdAsync(int manufacturerId, CancellationToken ct = default)
        {
            return _dbContext.Manufacturers.FirstOrDefaultAsync(m => m.ManufacturerId == manufacturerId, ct);
        }

        public async Task<(IReadOnlyList<Manufacturer> Items, int TotalCount)> SearchAsync(string? term, int page, int pageSize, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var query = _dbContext.Manufacturers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowered = term.ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(lowered));
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task UpdateAsync(Manufacturer manufacturer, CancellationToken ct = default)
        {
            _dbContext.Manufacturers.Update(manufacturer);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
