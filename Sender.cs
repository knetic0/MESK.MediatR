using Microsoft.Extensions.DependencyInjection;

namespace MESK.MediatR;

public sealed class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;
    
    public Sender(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    
    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        using var scoped = _serviceProvider.CreateScope();
        var sp = scoped.ServiceProvider;
        
        var interfaceType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType());
        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType());

        RequestHandlerDelegate handlerDelegate = () =>
        {
            var handler = sp.GetRequiredService(pipelineType);
            var method = interfaceType.GetMethod("Handle")!;
            return (Task)method.Invoke(handler, new object[] { request, cancellationToken })!;
        };

        var behaviors = (IEnumerable<object>)sp.GetServices(pipelineType);
        
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
        using var scoped = _serviceProvider.CreateScope();
        var sp = scoped.ServiceProvider;
        
        var interfaceType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var pipelineType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));

        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            var handler = sp.GetRequiredService(pipelineType);
            var method = interfaceType.GetMethod("Handle")!;
            return (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        };
        
        var behaviors = (IEnumerable<object>)sp.GetServices(pipelineType);
        
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
        using var scoped = _serviceProvider.CreateScope();
        var sp = scoped.ServiceProvider;
        
        var interfaceType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        
        var handlers = (IEnumerable<object>)sp.GetService(interfaceType)!;
        
        var tasks = handlers
            .Select(handler =>
            {
                var method = interfaceType.GetMethod("Handle")!;
                return (Task)method.Invoke(handler, new object[] { notification, cancellationToken })!;
            }).ToArray();
        
        await Task.WhenAll(tasks);
    }
}