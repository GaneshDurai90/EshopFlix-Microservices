using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.Categories.Commands;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.IntegrationEvents;
using CatalogService.Application.Messaging;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Categories.Handlers
{
    public sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, CategoryDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IIntegrationEventPublisher _eventPublisher;

        public UpdateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            IMapper mapper,
            IIntegrationEventPublisher eventPublisher)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
        }

        public async Task<CategoryDto> Handle(UpdateCategoryCommand command, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId, ct);
            if (category == null)
            {
                throw AppException.NotFound("category", $"Category {command.CategoryId} not found");
            }

            var normalizedSlug = NormalizeSlug(command.Slug);
            var exists = await _categoryRepository.ExistsWithSlugAsync(normalizedSlug, command.CategoryId, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["slug"] = new[] { "Slug is already in use." }
                });
            }

            category.UpdateDetails(
                command.Name.Trim(),
                normalizedSlug,
                command.Description,
                command.ParentCategoryId,
                command.SortOrder,
                command.IsActive);

            await _categoryRepository.UpdateAsync(category, ct);

            var dto = _mapper.Map<CategoryDto>(category);
            await _eventPublisher.EnqueueAsync(
                nameof(CategoryUpdatedIntegrationEvent),
                new CategoryUpdatedIntegrationEvent(category.CategoryId, category.Name, category.Slug, category.ParentCategoryId),
                ct);

            return dto;
        }

        private static string NormalizeSlug(string slug)
            => slug?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
