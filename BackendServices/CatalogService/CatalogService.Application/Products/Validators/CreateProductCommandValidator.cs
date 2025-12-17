using System;
using CatalogService.Application.Products.Commands;
using CatalogService.Domain.Enums;
using FluentValidation;

namespace CatalogService.Application.Products.Validators
{
    public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(250);

            RuleFor(x => x.Slug)
                .NotEmpty()
                .Matches("^[a-z0-9\\-]+$")
                .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");

            RuleFor(x => x.Status)
                .Must(status => Enum.IsDefined(typeof(ProductStatus), status))
                .WithMessage("Invalid product status supplied.");
        }
    }
}
