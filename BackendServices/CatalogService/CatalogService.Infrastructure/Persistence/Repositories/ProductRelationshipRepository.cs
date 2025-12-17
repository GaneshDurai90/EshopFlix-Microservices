using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class ProductRelationshipRepository : IProductRelationshipRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public ProductRelationshipRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ProductRelationship relationship, CancellationToken ct = default)
        {
            await _dbContext.ProductRelationships.AddAsync(relationship, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int parentProductId, int relatedProductId, CancellationToken ct = default)
        {
            var entity = await _dbContext.ProductRelationships.FirstOrDefaultAsync(r => r.ParentProductId == parentProductId && r.RelatedProductId == relatedProductId, ct);
            if (entity != null)
            {
                _dbContext.ProductRelationships.Remove(entity);
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        public async Task<IReadOnlyList<ProductRelationship>> GetByParentAsync(int parentProductId, byte? relationshipType, CancellationToken ct = default)
        {
            var query = _dbContext.ProductRelationships.Where(r => r.ParentProductId == parentProductId);
            if (relationshipType.HasValue)
            {
                query = query.Where(r => r.RelationshipType == relationshipType.Value);
            }

            return await query
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.RelatedProductId)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(ProductRelationship relationship, CancellationToken ct = default)
        {
            _dbContext.ProductRelationships.Update(relationship);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
