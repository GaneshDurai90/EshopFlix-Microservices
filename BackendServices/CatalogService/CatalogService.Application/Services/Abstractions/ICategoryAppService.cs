using CatalogService.Application.Categories.Commands;
using CatalogService.Application.Categories.Queries;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Services.Abstractions
{
    public interface ICategoryAppService
    {
        Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<CategoryNodeDto>> GetTreeAsync(bool includeInactive, CancellationToken ct = default);
        Task<CategoryDto> CreateAsync(CreateCategoryCommand command, CancellationToken ct = default);
        Task<CategoryDto> UpdateAsync(UpdateCategoryCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteCategoryCommand command, CancellationToken ct = default);
    }
}
