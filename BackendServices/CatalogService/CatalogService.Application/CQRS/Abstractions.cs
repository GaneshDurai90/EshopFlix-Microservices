namespace CatalogService.Application.CQRS
{
    public interface ICommand<out TResponse> { }
    public interface IQuery<out TResponse> { }

    public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        Task<TResponse> Handle(TCommand command, CancellationToken ct);
    }

    public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        Task<TResponse> Handle(TQuery query, CancellationToken ct);
    }

    public interface IDispatcher
    {
        Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
        Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
    }
}
