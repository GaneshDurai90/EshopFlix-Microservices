namespace StockService.Application.CQRS;

/// <summary>
/// Marker interface for commands that return a response.
/// Commands represent intent to change state.
/// </summary>
public interface ICommand<out TResponse> { }

/// <summary>
/// Marker interface for queries that return a response.
/// Queries represent read operations and should not modify state.
/// </summary>
public interface IQuery<out TResponse> { }

/// <summary>
/// Handler for a specific command type.
/// </summary>
public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken ct);
}

/// <summary>
/// Handler for a specific query type.
/// </summary>
public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken ct);
}

/// <summary>
/// Dispatcher for routing commands and queries to their handlers.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Sends a command to its handler and returns the response.
    /// </summary>
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);

    /// <summary>
    /// Executes a query and returns the response.
    /// </summary>
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
}
