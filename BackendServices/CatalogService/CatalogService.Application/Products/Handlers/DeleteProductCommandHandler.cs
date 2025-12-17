using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Exceptions;
using CatalogService.Application.IntegrationEvents;
using CatalogService.Application.Messaging;
using CatalogService.Application.Products.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Products.Handlers
{
    public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, bool>
    {
        private readonly IProductRepository _productRepository;
        private readonly IIntegrationEventPublisher _eventPublisher;

        public DeleteProductCommandHandler(
            IProductRepository productRepository,
            IIntegrationEventPublisher eventPublisher)
        {
            _productRepository = productRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task<bool> Handle(DeleteProductCommand command, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
            if (product == null)
            {
                throw AppException.NotFound("product", $"Product {command.ProductId} not found");
            }

            await _productRepository.DeleteAsync(product, ct);

            await _eventPublisher.EnqueueAsync(
                nameof(ProductDeletedIntegrationEvent),
                new ProductDeletedIntegrationEvent(product.ProductId, product.Name, product.Slug),
                ct);

            return true;
        }
    }
}
