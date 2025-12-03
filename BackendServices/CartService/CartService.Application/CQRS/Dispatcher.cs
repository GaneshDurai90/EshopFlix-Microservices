using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CartService.Application.CQRS
{
    public sealed class Dispatcher : IDispatcher
    {
        private readonly IServiceProvider _sp;

        public Dispatcher(IServiceProvider sp) => _sp = sp;

        public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        {
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
            dynamic handler = _sp.GetRequiredService(handlerType);
            return handler.Handle((dynamic)command, ct);
        }

        public Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
            dynamic handler = _sp.GetRequiredService(handlerType);
            return handler.Handle((dynamic)query, ct);
        }
    }
}
