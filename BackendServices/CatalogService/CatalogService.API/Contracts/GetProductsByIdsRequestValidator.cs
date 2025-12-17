using FluentValidation;
using System.Linq;

namespace CatalogService.API.Contracts
{
    public sealed class GetProductsByIdsRequestValidator : AbstractValidator<GetProductsByIdsRequest>
    {
        private const int MaxProductsPerCall = 500;

        public GetProductsByIdsRequestValidator()
        {
            RuleFor(x => x.Ids)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Ids collection is required.")
                .Must(ids => ids.Any()).WithMessage("Provide at least one product id.")
                .Must(ids => ids.All(id => id > 0)).WithMessage("Product ids must be positive integers.")
                .Must(ids => ids.Count <= MaxProductsPerCall)
                .WithMessage($"You can request up to {MaxProductsPerCall} products per call.");
        }
    }
}
