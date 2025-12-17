using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence.Repositories
{
    public sealed class ProductReviewRepository : IProductReviewRepository
    {
        private readonly CatalogServiceDbContext _dbContext;

        public ProductReviewRepository(CatalogServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ProductReview review, CancellationToken ct = default)
        {
            await _dbContext.ProductReviews.AddAsync(review, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(ProductReview review, CancellationToken ct = default)
        {
            _dbContext.ProductReviews.Remove(review);
            await _dbContext.SaveChangesAsync(ct);
        }

        public Task<ProductReview?> GetByIdAsync(int reviewId, CancellationToken ct = default)
        {
            return _dbContext.ProductReviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId, ct);
        }

        public async Task<(IReadOnlyList<ProductReview> Items, int TotalCount)> SearchAsync(int productId, bool? isPublished, int page, int pageSize, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            var query = _dbContext.ProductReviews.Where(r => r.ProductId == productId);

            if (isPublished.HasValue)
            {
                query = query.Where(r => r.IsPublished == isPublished.Value);
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(r => r.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task UpdateAsync(ProductReview review, CancellationToken ct = default)
        {
            _dbContext.ProductReviews.Update(review);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
