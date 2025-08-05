using Microsoft.Extensions.DependencyInjection;

namespace MESK.MediatR;

public static class ServiceRegistrar
{
    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        Action<MediatROptions> opts)
    {
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        
        var config = new MediatROptions();
        opts(config);

        foreach (var assembly in config.Assemblies)
        {
            var types = assembly
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract);

            var handlerTypes = types
                .SelectMany(t =>
                    t.GetInterfaces()
                        .Where(i => i.IsGenericType && (
                            i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)
                            || i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                        .Select(s => new { Interface = s, Implementation = t }));

            services.AddScoped<ISender, Sender>();

            foreach (var handlerType in handlerTypes)
            {
                services.AddScoped(handlerType.Interface, handlerType.Implementation);
            }
        }
        
        foreach (var pipeline in config.PipelineBehaviors)
        {
            var genericArg = pipeline.GetGenericArguments().Length;

            if (genericArg == 1)
            {
                services.AddScoped(typeof(IPipelineBehavior<>), pipeline);
            }
            else if (genericArg == 2)
            {
                services.AddScoped(typeof(IPipelineBehavior<,>), pipeline);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(genericArg));
            }
        }
        
        foreach (var assembly in config.Assemblies)
        {
            var types = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract);

            var handlerTypes = types
                .SelectMany(t => t
                    .GetInterfaces()
                    .Where(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
                    .Select(s => new { Interface = s, Implementation = t }));

            foreach (var handler in handlerTypes)
            {
                services.AddScoped(handler.Interface, handler.Implementation);
            }
        }
        
        return services;
    }
}