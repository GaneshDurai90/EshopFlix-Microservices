using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface IProductImageRepository
    {
        Task<ProductImage?> GetByIdAsync(int imageId, CancellationToken ct = default);
        Task<IReadOnlyList<ProductImage>> GetByProductAsync(int productId, CancellationToken ct = default);
        Task AddAsync(ProductImage image, CancellationToken ct = default);
        Task UpdateAsync(ProductImage image, CancellationToken ct = default);
        Task DeleteAsync(ProductImage image, CancellationToken ct = default);
    }
}
