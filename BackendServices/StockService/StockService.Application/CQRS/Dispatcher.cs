using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StockService.Application.CQRS;

/// <summary>
/// CQRS Dispatcher implementation that routes commands and queries to their handlers.
/// </summary>
public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Dispatcher> _logger;

    public Dispatcher(IServiceProvider serviceProvider, ILogger<Dispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        var commandType = command.GetType();
        _logger.LogDebug("Dispatching command: {CommandType}", commandType.Name);

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));
        
        try
        {
            dynamic handler = _serviceProvider.GetRequiredService(handlerType);
            var response = await handler.HandleAsync((dynamic)command, ct);
            
            _logger.LogDebug("Command {CommandType} handled successfully", commandType.Name);
            return response;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No service"))
        {
            _logger.LogError(ex, "No handler registered for command: {CommandType}", commandType.Name);
            throw new InvalidOperationException($"No handler registered for command: {commandType.Name}", ex);
        }
    }

    public async Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        var queryType = query.GetType();
        _logger.LogDebug("Dispatching query: {QueryType}", queryType.Name);

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));
        
        try
        {
            dynamic handler = _serviceProvider.GetRequiredService(handlerType);
            var response = await handler.HandleAsync((dynamic)query, ct);
            
            _logger.LogDebug("Query {QueryType} handled successfully", queryType.Name);
            return response;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No service"))
        {
            _logger.LogError(ex, "No handler registered for query: {QueryType}", queryType.Name);
            throw new InvalidOperationException($"No handler registered for query: {queryType.Name}", ex);
        }
    }
}
