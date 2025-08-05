namespace MESK.MediatR;

public delegate Task RequestHandlerDelegate();

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

public interface IPipelineBehavior<in TRequest>
    where TRequest : IRequest
{
    Task Handle(
        TRequest request, 
        RequestHandlerDelegate next, 
        CancellationToken cancellationToken = default!);
}

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken = default!);
}