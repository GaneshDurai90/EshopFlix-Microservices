using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.Categories.Queries;
using CatalogService.Application.DTO;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Categories.Handlers
{
    public sealed class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, CategoryDto?>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<CategoryDto?> Handle(GetCategoryByIdQuery query, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(query.CategoryId, ct);
            return category is null ? null : _mapper.Map<CategoryDto>(category);
        }
    }
}
