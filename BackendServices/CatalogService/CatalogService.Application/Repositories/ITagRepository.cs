using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface ITagRepository
    {
        Task<Tag?> GetByIdAsync(int tagId, CancellationToken ct = default);
        Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null, CancellationToken ct = default);
        Task<(IReadOnlyList<Tag> Items, int TotalCount)> SearchAsync(string? term, int page, int pageSize, CancellationToken ct = default);
        Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(Tag tag, CancellationToken ct = default);
        Task UpdateAsync(Tag tag, CancellationToken ct = default);
        Task DeleteAsync(Tag tag, CancellationToken ct = default);
        Task AssignToProductAsync(int productId, IEnumerable<int> tagIds, CancellationToken ct = default);
    }
}
