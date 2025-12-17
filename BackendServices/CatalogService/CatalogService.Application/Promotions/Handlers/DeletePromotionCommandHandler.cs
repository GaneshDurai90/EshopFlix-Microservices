using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Promotions.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Promotions.Handlers
{
    public sealed class DeletePromotionCommandHandler : ICommandHandler<DeletePromotionCommand, bool>
    {
        private readonly IPromotionRepository _promotionRepository;

        public DeletePromotionCommandHandler(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task<bool> Handle(DeletePromotionCommand command, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(command.PromotionId, ct);
            if (promotion == null)
            {
                throw AppException.NotFound("promotion", $"Promotion {command.PromotionId} not found");
            }

            await _promotionRepository.DeleteAsync(promotion, ct);
            return true;
        }
    }
}
