using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Promotions.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Promotions.Handlers
{
    public sealed class UpdatePromotionCommandHandler : ICommandHandler<UpdatePromotionCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IMapper _mapper;

        public UpdatePromotionCommandHandler(IPromotionRepository promotionRepository, IMapper mapper)
        {
            _promotionRepository = promotionRepository;
            _mapper = mapper;
        }

        public async Task<PromotionDto> Handle(UpdatePromotionCommand command, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(command.PromotionId, ct);
            if (promotion == null)
            {
                throw AppException.NotFound("promotion", $"Promotion {command.PromotionId} not found");
            }

            var exists = await _promotionRepository.ExistsByCodeAsync(command.Code.Trim(), command.PromotionId, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["code"] = new[] { "Promotion code already exists." }
                });
            }

            promotion.Code = command.Code.Trim();
            promotion.Name = command.Name.Trim();
            promotion.Description = command.Description;
            promotion.DiscountType = command.DiscountType;
            promotion.DiscountValue = command.DiscountValue;
            promotion.AppliesToAllProducts = command.AppliesToAllProducts;
            promotion.StartDate = command.StartDate;
            promotion.EndDate = command.EndDate;
            promotion.IsActive = command.IsActive;

            await _promotionRepository.UpdateAsync(promotion, command.ProductIds ?? System.Array.Empty<int>(), ct);
            var dto = _mapper.Map<PromotionDto>(promotion);
            dto.ProductIds = command.ProductIds ?? System.Array.Empty<int>();
            return dto;
        }
    }
}
