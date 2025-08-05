namespace MESK.MediatR;

public sealed class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;
    
    public Sender(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    
    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        var interfaceType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType());
        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType());

        RequestHandlerDelegate handlerDelegate = () =>
        {
            var handler = _serviceProvider.GetService(pipelineType);
            var method = interfaceType.GetMethod("Handle")!;
            return (Task)method.Invoke(handler, new object[] { request, cancellationToken })!;
        };
        
        var behaviors = (IEnumerable<object>)(_serviceProvider.GetService(pipelineType) ?? Enumerable.Empty<object>());
        
        var pipeline = behaviors
            .Reverse()
            .Aggregate(handlerDelegate, (next, behavior) =>
            {
                return () =>
                {
                    var method = pipelineType.GetMethod("Handle")!;
                    return (Task)method.Invoke(behavior, new object[] { request, next, cancellationToken })!;
                };
            });
        
        await pipeline();
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var interfaceType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));

        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            var handler = _serviceProvider.GetService(pipelineType);
            var method = interfaceType.GetMethod("Handle")!;
            return (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        };
        
        var behaviors = (IEnumerable<object>)(_serviceProvider.GetService(pipelineType) ?? Enumerable.Empty<object>());
        
        var pipeline = behaviors
            .Reverse()
            .Aggregate(handlerDelegate, (next, behavior) =>
            {
                return () =>
                {
                    var method = pipelineType.GetMethod("Handle")!;
                    return (Task<TResponse>)method.Invoke(behavior, new object[] { request, next, cancellationToken })!;
                };
            });
        
        return await pipeline();
    }

    public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        var interfaceType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        
        var handlers = (IEnumerable<object>)_serviceProvider.GetService(interfaceType)!;
        
        var tasks = handlers
            .Select(handler =>
            {
                var method = interfaceType.GetMethod("Handle")!;
                return (Task)method.Invoke(handler, new object[] { notification, cancellationToken })!;
            }).ToArray();
        
        await Task.WhenAll(tasks);
    }
}