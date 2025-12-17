using CatalogService.Application.Tags.Queries;
using FluentValidation;

namespace CatalogService.Application.Tags.Validators
{
    public sealed class SearchTagsQueryValidator : AbstractValidator<SearchTagsQuery>
    {
        public SearchTagsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(200);
        }
    }
}
