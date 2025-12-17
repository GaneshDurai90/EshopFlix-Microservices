using CatalogService.Application.ProductRelationships.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductRelationships.Validators
{
    public sealed class DeleteProductRelationshipCommandValidator : AbstractValidator<DeleteProductRelationshipCommand>
    {
        public DeleteProductRelationshipCommandValidator()
        {
            RuleFor(x => x.ParentProductId).GreaterThan(0);
            RuleFor(x => x.RelatedProductId).GreaterThan(0);
        }
    }
}
