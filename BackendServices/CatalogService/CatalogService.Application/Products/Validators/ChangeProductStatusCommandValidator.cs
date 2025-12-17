using System;
using CatalogService.Application.Products.Commands;
using CatalogService.Domain.Enums;
using FluentValidation;

namespace CatalogService.Application.Products.Validators
{
    public sealed class ChangeProductStatusCommandValidator : AbstractValidator<ChangeProductStatusCommand>
    {
        public ChangeProductStatusCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Status)
                .Must(status => Enum.IsDefined(typeof(ProductStatus), status))
                .WithMessage("Invalid product status supplied.");
        }
    }
}
