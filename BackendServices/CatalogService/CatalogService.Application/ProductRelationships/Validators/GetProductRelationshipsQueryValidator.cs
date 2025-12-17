using CatalogService.Application.ProductRelationships.Queries;
using FluentValidation;

namespace CatalogService.Application.ProductRelationships.Validators
{
    public sealed class GetProductRelationshipsQueryValidator : AbstractValidator<GetProductRelationshipsQuery>
    {
        public GetProductRelationshipsQueryValidator()
        {
            RuleFor(x => x.ParentProductId).GreaterThan(0);
        }
    }
}
