using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface IProductReviewRepository
    {
        Task<ProductReview?> GetByIdAsync(int reviewId, CancellationToken ct = default);
        Task<(IReadOnlyList<ProductReview> Items, int TotalCount)> SearchAsync(int productId, bool? isPublished, int page, int pageSize, CancellationToken ct = default);
        Task AddAsync(ProductReview review, CancellationToken ct = default);
        Task UpdateAsync(ProductReview review, CancellationToken ct = default);
        Task DeleteAsync(ProductReview review, CancellationToken ct = default);
    }
}
