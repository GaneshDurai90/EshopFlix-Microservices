using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Categories.Queries;
using CatalogService.Application.DTO;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Categories.Handlers
{
    public sealed class GetCategoryTreeQueryHandler : IQueryHandler<GetCategoryTreeQuery, IEnumerable<CategoryNodeDto>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoryTreeQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<CategoryNodeDto>> Handle(GetCategoryTreeQuery query, CancellationToken ct)
        {
            var categories = await _categoryRepository.GetAllAsync(ct);
            var lookup = categories
                .Where(c => query.IncludeInactive || c.IsActive)
                .ToDictionary(c => c.CategoryId, c => new CategoryNodeDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Slug = c.Slug,
                    IsActive = c.IsActive,
                    SortOrder = c.SortOrder
                });

            foreach (var category in lookup.Values)
            {
                category.Children.Clear();
            }

            var roots = new List<CategoryNodeDto>();
            foreach (var entity in categories)
            {
                if (!lookup.TryGetValue(entity.CategoryId, out var node))
                    continue;

                if (entity.ParentCategoryId.HasValue && lookup.TryGetValue(entity.ParentCategoryId.Value, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            foreach (var node in lookup.Values)
            {
                node.Children = node.Children.OrderBy(c => c.SortOrder).ToList();
            }

            return roots.OrderBy(r => r.SortOrder).ToArray();
        }
    }
}
