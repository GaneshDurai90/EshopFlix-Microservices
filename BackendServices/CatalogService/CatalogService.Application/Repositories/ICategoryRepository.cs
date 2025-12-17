using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Category>> GetChildrenAsync(int parentCategoryId, CancellationToken ct = default);
        Task<bool> ExistsWithSlugAsync(string slug, int? excludeCategoryId = null, CancellationToken ct = default);
        Task AddAsync(Category category, CancellationToken ct = default);
        Task UpdateAsync(Category category, CancellationToken ct = default);
        Task DeleteAsync(Category category, CancellationToken ct = default);
    }
}
