using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Tags.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Tags.Handlers
{
    public sealed class AssignTagsToProductCommandHandler : ICommandHandler<AssignTagsToProductCommand, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public AssignTagsToProductCommandHandler(
            IProductRepository productRepository,
            ITagRepository tagRepository,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<ProductDetailDto> Handle(AssignTagsToProductCommand command, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
            if (product == null)
            {
                throw AppException.NotFound("product", $"Product {command.ProductId} not found");
            }

            var tagIds = command.TagIds?.Distinct().ToArray() ?? System.Array.Empty<int>();
            await _tagRepository.AssignToProductAsync(command.ProductId, tagIds, ct);

            product = await _productRepository.GetByIdAsync(command.ProductId, ct) ?? product;
            return _mapper.Map<ProductDetailDto>(product);
        }
    }
}
