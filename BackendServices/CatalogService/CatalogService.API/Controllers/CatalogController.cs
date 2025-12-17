using CatalogService.API.Contracts;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Products.Queries;
using CatalogService.Application.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly IProductAppService _productService;
        private readonly IValidator<GetProductsByIdsRequest> _getByIdsValidator;

        public CatalogController(
            IProductAppService productService,
            IValidator<GetProductsByIdsRequest> getByIdsValidator)
        {
            _productService = productService;
            _getByIdsValidator = getByIdsValidator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _productService.SearchAsync(new SearchProductsQuery
            {
                Page = 1,
                PageSize = 500
            }, ct);

            return Ok(result.Items ?? Enumerable.Empty<ProductListItemDto>());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            if (id <= 0)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["id"] = new[] { "Id must be greater than zero." }
                });
            }

            var product = await _productService.GetByIdAsync(id, ct);
            if (product is null)
            {
                throw AppException.NotFound("product", $"Product with ID {id} not found.");
            }

            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> GetByIds([FromBody] GetProductsByIdsRequest request, CancellationToken ct)
        {
            if (request is null)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["body"] = new[] { "Request payload is required." }
                });
            }

            var validation = _getByIdsValidator.Validate(request);
            if (!validation.IsValid)
            {
                throw AppException.Validation(ToErrorDictionary(validation));
            }

            var products = await _productService.GetByIdsAsync(request.Ids ?? Array.Empty<int>(), ct);

            return Ok(products ?? Enumerable.Empty<ProductDTO>());
        }

        private static IDictionary<string, string[]> ToErrorDictionary(FluentValidation.Results.ValidationResult validationResult)
            => validationResult.Errors
                .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray(),
                    StringComparer.OrdinalIgnoreCase);

    }
}
