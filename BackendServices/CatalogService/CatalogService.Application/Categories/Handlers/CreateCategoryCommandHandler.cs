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
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Categories.Handlers
{
    public sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IIntegrationEventPublisher _eventPublisher;

        public CreateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            IMapper mapper,
            IIntegrationEventPublisher eventPublisher)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
        }

        public async Task<CategoryDto> Handle(CreateCategoryCommand command, CancellationToken ct)
        {
            var normalizedSlug = NormalizeSlug(command.Slug);
            var exists = await _categoryRepository.ExistsWithSlugAsync(normalizedSlug, null, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["slug"] = new[] { "Slug is already in use." }
                });
            }

            var category = new Category
            {
                Name = command.Name.Trim(),
                Slug = normalizedSlug,
                Description = command.Description,
                ParentCategoryId = command.ParentCategoryId,
                SortOrder = command.SortOrder,
                IsActive = command.IsActive,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            await _categoryRepository.AddAsync(category, ct);

            var dto = _mapper.Map<CategoryDto>(category);
            await _eventPublisher.EnqueueAsync(
                nameof(CategoryCreatedIntegrationEvent),
                new CategoryCreatedIntegrationEvent(category.CategoryId, category.Name, category.Slug, category.ParentCategoryId),
                ct);

            return dto;
        }

        private static string NormalizeSlug(string slug)
            => slug?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
