using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Promotions.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Promotions.Handlers
{
    public sealed class CreatePromotionCommandHandler : ICommandHandler<CreatePromotionCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IMapper _mapper;

        public CreatePromotionCommandHandler(IPromotionRepository promotionRepository, IMapper mapper)
        {
            _promotionRepository = promotionRepository;
            _mapper = mapper;
        }

        public async Task<PromotionDto> Handle(CreatePromotionCommand command, CancellationToken ct)
        {
            var exists = await _promotionRepository.ExistsByCodeAsync(command.Code.Trim(), null, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["code"] = new[] { "Promotion code already exists." }
                });
            }

            var promotion = new Promotion
            {
                Code = command.Code.Trim(),
                Name = command.Name.Trim(),
                Description = command.Description,
                DiscountType = command.DiscountType,
                DiscountValue = command.DiscountValue,
                AppliesToAllProducts = command.AppliesToAllProducts,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                IsActive = command.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            await _promotionRepository.AddAsync(promotion, command.ProductIds ?? Array.Empty<int>(), ct);
            var dto = _mapper.Map<PromotionDto>(promotion);
            dto.ProductIds = command.ProductIds ?? Array.Empty<int>();
            return dto;
        }
    }
}
