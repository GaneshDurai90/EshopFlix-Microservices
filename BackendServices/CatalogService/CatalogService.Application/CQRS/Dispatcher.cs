using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogService.Application.CQRS
{
    public sealed class Dispatcher : IDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public Dispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        {
            await ValidateAsync(command, ct);
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
            dynamic handler = _serviceProvider.GetRequiredService(handlerType);
            return await handler.Handle((dynamic)command, ct);
        }

        public async Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        {
            await ValidateAsync(query, ct);
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
            dynamic handler = _serviceProvider.GetRequiredService(handlerType);
            return await handler.Handle((dynamic)query, ct);
        }

        private async Task ValidateAsync<TRequest>(TRequest request, CancellationToken ct)
        {
            var validators = _serviceProvider.GetServices<IValidator<TRequest>>();
            if (validators == null || !validators.Any())
            {
                return;
            }

            var context = new ValidationContext<TRequest>(request);
            var failures = new List<ValidationFailure>();

            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(context, ct);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            if (failures.Count > 0)
            {
                var errorDictionary = failures
                    .GroupBy(f => f.PropertyName ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Where(f => !string.IsNullOrWhiteSpace(f.ErrorMessage))
                              .Select(f => f.ErrorMessage)
                              .Distinct()
                              .ToArray());

                throw AppException.Validation(errorDictionary);
            }
        }
    }
}
