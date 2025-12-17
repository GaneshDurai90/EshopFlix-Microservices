using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Categories.Commands;
using CatalogService.Application.Exceptions;
using CatalogService.Application.IntegrationEvents;
using CatalogService.Application.Messaging;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Categories.Handlers
{
    public sealed class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, bool>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IIntegrationEventPublisher _eventPublisher;

        public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IIntegrationEventPublisher eventPublisher)
        {
            _categoryRepository = categoryRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task<bool> Handle(DeleteCategoryCommand command, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId, ct);
            if (category == null)
            {
                throw AppException.NotFound("category", $"Category {command.CategoryId} not found");
            }

            var children = await _categoryRepository.GetChildrenAsync(category.CategoryId, ct);
            if (children.Any())
            {
                throw AppException.Business("category.has-children", "Cannot delete a category that has child categories.");
            }

            await _categoryRepository.DeleteAsync(category, ct);

            await _eventPublisher.EnqueueAsync(
                nameof(CategoryDeletedIntegrationEvent),
                new CategoryDeletedIntegrationEvent(category.CategoryId, category.Name, category.Slug),
                ct);

            return true;
        }
    }
}
