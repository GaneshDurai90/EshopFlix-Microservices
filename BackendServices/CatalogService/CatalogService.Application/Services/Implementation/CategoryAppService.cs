using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Categories.Commands;
using CatalogService.Application.Categories.Queries;
using CatalogService.Application.DTO;
using CatalogService.Application.Services.Abstractions;

namespace CatalogService.Application.Services.Implementation
{
    public sealed class CategoryAppService : ICategoryAppService
    {
        private readonly IDispatcher _dispatcher;

        public CategoryAppService(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);

        public async Task DeleteAsync(DeleteCategoryCommand command, CancellationToken ct = default)
        {
            await _dispatcher.Send(command, ct);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _dispatcher.Query(new GetCategoryByIdQuery { CategoryId = id }, ct);

        public async Task<IEnumerable<CategoryNodeDto>> GetTreeAsync(bool includeInactive, CancellationToken ct = default)
            => await _dispatcher.Query(new GetCategoryTreeQuery { IncludeInactive = includeInactive }, ct);

        public async Task<CategoryDto> UpdateAsync(UpdateCategoryCommand command, CancellationToken ct = default)
            => await _dispatcher.Send(command, ct);
    }
}
