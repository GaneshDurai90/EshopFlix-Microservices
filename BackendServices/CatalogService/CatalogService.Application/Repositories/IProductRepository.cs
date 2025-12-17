using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;

namespace CatalogService.Application.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
        Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
            string? term,
            int? categoryId,
            ProductStatus? status,
            int page,
            int pageSize,
            CancellationToken ct = default);
        Task<bool> ExistsWithSlugAsync(string slug, int? excludeProductId = null, CancellationToken ct = default);
        Task AddAsync(Product product, CancellationToken ct = default);
        Task UpdateAsync(Product product, CancellationToken ct = default);
        Task DeleteAsync(Product product, CancellationToken ct = default);
    }
}
