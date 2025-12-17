using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface IPromotionRepository
    {
        Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken ct = default);
        Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken ct = default);
        Task<(IReadOnlyList<Promotion> Items, int TotalCount)> SearchAsync(string? term, bool? isActive, int page, int pageSize, CancellationToken ct = default);
        Task AddAsync(Promotion promotion, IEnumerable<int> productIds, CancellationToken ct = default);
        Task UpdateAsync(Promotion promotion, IEnumerable<int> productIds, CancellationToken ct = default);
        Task DeleteAsync(Promotion promotion, CancellationToken ct = default);
    }
}
