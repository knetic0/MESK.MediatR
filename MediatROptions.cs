using System.Reflection;

namespace MESK.MediatR;

public sealed class MediatROptions
{
    internal List<Assembly> Assemblies { get; set; } = new();
    internal List<Type> PipelineBehaviors { get; set; } = new();
    
    public void RegisterServicesFromAssembly(Assembly assembly)
        => Assemblies.Add(assembly);
    
    public void RegisterServicesFromAssemblies(params Assembly[] assemblies)
        => Assemblies.AddRange(assemblies);
    
    public void RegisterPipelineBehavior(Type behaviorType)
        => PipelineBehaviors.Add(behaviorType);
}