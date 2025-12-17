using System;
using System.Collections.Generic;
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
using CatalogService.Domain.Enums;

namespace CatalogService.Application.Products.Handlers
{
    public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IIntegrationEventPublisher _eventPublisher;

        public UpdateProductCommandHandler(
            IProductRepository productRepository,
            IMapper mapper,
            IIntegrationEventPublisher eventPublisher)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
        }

        public async Task<ProductDetailDto> Handle(UpdateProductCommand command, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
            if (product == null)
            {
                throw AppException.NotFound("product", $"Product {command.ProductId} not found");
            }

            var normalizedSlug = NormalizeSlug(command.Slug);
            var slugExists = await _productRepository.ExistsWithSlugAsync(normalizedSlug, command.ProductId, ct);
            if (slugExists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["slug"] = new[] { "Slug is already in use." }
                });
            }

            var status = Enum.IsDefined(typeof(ProductStatus), (int)command.Status)
                ? (ProductStatus)command.Status
                : product.GetStatus();

            product.UpdateBasics(
                command.Name.Trim(),
                normalizedSlug,
                command.ShortDescription,
                command.LongDescription,
                command.BrandId,
                command.ManufacturerId,
                command.CategoryId,
                command.IsSearchable,
                command.Weight,
                command.Dimensions,
                command.PrimaryImageUrl,
                command.SeoTitle,
                command.SeoDescription,
                command.SeoKeywords);

            product.SetStatus(status);

            await _productRepository.UpdateAsync(product, ct);

            var dto = _mapper.Map<ProductDetailDto>(product);
            await _eventPublisher.EnqueueAsync(
                nameof(ProductUpdatedIntegrationEvent),
                new ProductUpdatedIntegrationEvent(product.ProductId, product.Name, product.Slug, product.CategoryId),
                ct);

            return dto;
        }

        private static string NormalizeSlug(string slug)
            => slug?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
