using CatalogService.Application.ProductRelationships.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductRelationships.Validators
{
    public sealed class UpdateProductRelationshipCommandValidator : AbstractValidator<UpdateProductRelationshipCommand>
    {
        public UpdateProductRelationshipCommandValidator()
        {
            RuleFor(x => x.ParentProductId).GreaterThan(0);
            RuleFor(x => x.RelatedProductId).GreaterThan(0);
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }
}
