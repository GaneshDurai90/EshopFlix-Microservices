using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.IntegrationEvents;
using CatalogService.Application.Messaging;
using CatalogService.Application.Products.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Products.Handlers
{
    public sealed class ChangeProductStatusCommandHandler : ICommandHandler<ChangeProductStatusCommand, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly IMapper _mapper;

        public ChangeProductStatusCommandHandler(
            IProductRepository productRepository,
            IIntegrationEventPublisher eventPublisher,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _eventPublisher = eventPublisher;
            _mapper = mapper;
        }

        public async Task<ProductDetailDto> Handle(ChangeProductStatusCommand command, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
            if (product == null)
            {
                throw AppException.NotFound("product", $"Product {command.ProductId} not found");
            }

            product.SetStatus(command.Status);
            await _productRepository.UpdateAsync(product, ct);

            await _eventPublisher.EnqueueAsync(
                nameof(ProductUpdatedIntegrationEvent),
                new ProductUpdatedIntegrationEvent(product.ProductId, product.Name, product.Slug, product.CategoryId),
                ct);

            return _mapper.Map<ProductDetailDto>(product);
        }
    }
}
