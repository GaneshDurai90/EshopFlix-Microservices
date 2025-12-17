using CatalogService.Application.ProductRelationships.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductRelationships.Validators
{
    public sealed class AddProductRelationshipCommandValidator : AbstractValidator<AddProductRelationshipCommand>
    {
        public AddProductRelationshipCommandValidator()
        {
            RuleFor(x => x.ParentProductId).GreaterThan(0);
            RuleFor(x => x.RelatedProductId).GreaterThan(0);
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }
}
