using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina;

/// <summary>
/// Configures discovery and additional encina components.
/// </summary>
/// <remarks>
/// Used by <see cref="ServiceCollectionExtensions.AddEncina(IServiceCollection, Action{EncinaConfiguration}, System.Reflection.Assembly[])"/>
/// to fine-tune automatic registrations.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "Pre-1.0: every stage-aware overload takes a required PipelineStage before the optional order, so it can never be confused with the stage-less overload and calls are never ambiguous.")]
public sealed class EncinaConfiguration
{
    private readonly HashSet<Assembly> _assemblies = [];
    private readonly List<Type> _pipelineBehaviorTypes = [];
    private readonly List<Type> _requestPreProcessorTypes = [];
    private readonly List<Type> _requestPostProcessorTypes = [];
    private readonly List<Type> _requestErrorProcessorTypes = [];
    private readonly List<Type> _requestPostCommitProcessorTypes = [];
    private readonly List<Type> _streamPipelineBehaviorTypes = [];
    private readonly List<PipelineStageDeclaration> _stageDeclarations = [];

    /// <summary>
    /// Lifetime applied to handlers registered through scanning.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; private set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets the notification dispatch configuration options.
    /// </summary>
    /// <remarks>
    /// Controls how notifications are dispatched to multiple handlers.
    /// By default, notifications are dispatched sequentially with fail-fast semantics.
    /// </remarks>
    public NotificationDispatchOptions NotificationDispatch { get; } = new();

    internal IReadOnlyCollection<Assembly> Assemblies => _assemblies;
    internal IReadOnlyList<Type> PipelineBehaviorTypes => _pipelineBehaviorTypes;
    internal IReadOnlyList<Type> RequestPreProcessorTypes => _requestPreProcessorTypes;
    internal IReadOnlyList<Type> RequestPostProcessorTypes => _requestPostProcessorTypes;
    internal IReadOnlyList<Type> RequestErrorProcessorTypes => _requestErrorProcessorTypes;
    internal IReadOnlyList<Type> RequestPostCommitProcessorTypes => _requestPostCommitProcessorTypes;
    internal IReadOnlyList<Type> StreamPipelineBehaviorTypes => _streamPipelineBehaviorTypes;
    internal IReadOnlyList<PipelineStageDeclaration> StageDeclarations => _stageDeclarations;

    /// <summary>
    /// Sets the lifetime used for handlers registered through scanning.
    /// </summary>
    public EncinaConfiguration WithHandlerLifetime(ServiceLifetime lifetime)
    {
        HandlerLifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Configures parallel notification dispatch with the specified strategy.
    /// </summary>
    /// <param name="strategy">The dispatch strategy to use. Default is <see cref="NotificationDispatchStrategy.Parallel"/>.</param>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent handlers. Default is -1 (uses processor count).</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config =>
    /// {
    ///     config.UseParallelNotificationDispatch(
    ///         NotificationDispatchStrategy.ParallelWhenAll,
    ///         maxDegreeOfParallelism: 4);
    /// });
    /// </code>
    /// </example>
    public EncinaConfiguration UseParallelNotificationDispatch(
        NotificationDispatchStrategy strategy = NotificationDispatchStrategy.Parallel,
        int maxDegreeOfParallelism = -1)
    {
        NotificationDispatch.Strategy = strategy;
        NotificationDispatch.MaxDegreeOfParallelism = maxDegreeOfParallelism;
        return this;
    }

    /// <summary>
    /// Adds an assembly to scan for handlers, behaviors, and processors.
    /// </summary>
    public EncinaConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds multiple assemblies to the scan list.
    /// </summary>
    public EncinaConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies is null)
        {
            return this;
        }

        foreach (var assembly in assemblies)
        {
            if (assembly is not null)
            {
                _assemblies.Add(assembly);
            }
        }

        return this;
    }

    /// <summary>
    /// Adds the assembly that contains the specified type.
    /// </summary>
    public EncinaConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Adds a generic behavior to the pipeline.
    /// </summary>
    public EncinaConfiguration AddPipelineBehavior<TBehavior>()
        where TBehavior : class
        => AddPipelineBehavior(typeof(TBehavior));

    /// <summary>
    /// Adds a generic behavior to the pipeline and places it in a stage, overriding any
    /// <see cref="PipelineStageAttribute"/> on the type.
    /// </summary>
    /// <typeparam name="TBehavior">Behavior type.</typeparam>
    /// <param name="stage">Stage in which the behavior runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddPipelineBehavior&lt;TenantScopeBehavior&gt;(PipelineStage.Authorization, order: 10));
    /// </code>
    /// </example>
    public EncinaConfiguration AddPipelineBehavior<TBehavior>(PipelineStage stage, int order = 0)
        where TBehavior : class
        => AddPipelineBehavior(typeof(TBehavior), stage, order);

    /// <summary>
    /// Adds a specific behavior to the pipeline.
    /// </summary>
    public EncinaConfiguration AddPipelineBehavior(Type pipelineBehaviorType)
        => AddStep(_pipelineBehaviorTypes, pipelineBehaviorType, typeof(IPipelineBehavior<,>), nameof(pipelineBehaviorType));

    /// <summary>
    /// Adds a specific behavior to the pipeline and places it in a stage, overriding any
    /// <see cref="PipelineStageAttribute"/> on the type.
    /// </summary>
    /// <param name="pipelineBehaviorType">Behavior type; an open generic type covers every closed pipeline.</param>
    /// <param name="stage">Stage in which the behavior runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddPipelineBehavior(typeof(TenantScopeBehavior&lt;,&gt;), PipelineStage.Authorization, 10));
    /// </code>
    /// </example>
    public EncinaConfiguration AddPipelineBehavior(Type pipelineBehaviorType, PipelineStage stage, int order = 0)
        => AddStaged(_pipelineBehaviorTypes, pipelineBehaviorType, typeof(IPipelineBehavior<,>), nameof(pipelineBehaviorType), stage, order);

    /// <summary>
    /// Adds a generic pre-processor to the pipeline.
    /// </summary>
    public EncinaConfiguration AddRequestPreProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestPreProcessor(typeof(TProcessor));

    /// <summary>
    /// Adds a generic pre-processor to the pipeline and places it in a stage.
    /// </summary>
    /// <typeparam name="TProcessor">Processor type.</typeparam>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestPreProcessor&lt;NormalizeInput&gt;(PipelineStage.InputPreparation));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestPreProcessor<TProcessor>(PipelineStage stage, int order = 0)
        where TProcessor : class
        => AddRequestPreProcessor(typeof(TProcessor), stage, order);

    /// <summary>
    /// Adds a specific pre-processor to the pipeline.
    /// </summary>
    public EncinaConfiguration AddRequestPreProcessor(Type processorType)
        => AddStep(_requestPreProcessorTypes, processorType, typeof(IRequestPreProcessor<>), nameof(processorType));

    /// <summary>
    /// Adds a specific pre-processor to the pipeline and places it in a stage.
    /// </summary>
    /// <param name="processorType">Processor type.</param>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestPreProcessor(typeof(NormalizeInput), PipelineStage.InputPreparation));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestPreProcessor(Type processorType, PipelineStage stage, int order = 0)
        => AddStaged(_requestPreProcessorTypes, processorType, typeof(IRequestPreProcessor<>), nameof(processorType), stage, order);

    /// <summary>
    /// Adds a generic post-processor to the pipeline.
    /// </summary>
    public EncinaConfiguration AddRequestPostProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestPostProcessor(typeof(TProcessor));

    /// <summary>
    /// Adds a generic post-processor to the pipeline and places it in a stage.
    /// </summary>
    /// <typeparam name="TProcessor">Processor type.</typeparam>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestPostProcessor&lt;AuditResult&gt;(PipelineStage.Audit, order: 50));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestPostProcessor<TProcessor>(PipelineStage stage, int order = 0)
        where TProcessor : class
        => AddRequestPostProcessor(typeof(TProcessor), stage, order);

    /// <summary>
    /// Adds a specific post-processor to the pipeline.
    /// </summary>
    public EncinaConfiguration AddRequestPostProcessor(Type processorType)
        => AddStep(_requestPostProcessorTypes, processorType, typeof(IRequestPostProcessor<,>), nameof(processorType));

    /// <summary>
    /// Adds a specific post-processor to the pipeline and places it in a stage.
    /// </summary>
    /// <param name="processorType">Processor type.</param>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestPostProcessor(typeof(AuditResult), PipelineStage.Audit, 50));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestPostProcessor(Type processorType, PipelineStage stage, int order = 0)
        => AddStaged(_requestPostProcessorTypes, processorType, typeof(IRequestPostProcessor<,>), nameof(processorType), stage, order);

    /// <summary>
    /// Adds a generic error processor to the pipeline; it runs in its default stage.
    /// </summary>
    public EncinaConfiguration AddRequestErrorProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestErrorProcessor(typeof(TProcessor));

    /// <summary>
    /// Adds a generic error processor to the pipeline and places it in a stage.
    /// </summary>
    /// <typeparam name="TProcessor">Processor type.</typeparam>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestErrorProcessor&lt;ReportFailure&gt;(PipelineStage.Observability));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestErrorProcessor<TProcessor>(PipelineStage stage, int order = 0)
        where TProcessor : class
        => AddRequestErrorProcessor(typeof(TProcessor), stage, order);

    /// <summary>
    /// Adds a specific error processor to the pipeline; it runs in its default stage.
    /// </summary>
    /// <param name="processorType">Processor type implementing <see cref="IRequestErrorProcessor{TRequest, TResponse}"/>.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public EncinaConfiguration AddRequestErrorProcessor(Type processorType)
        => AddStep(_requestErrorProcessorTypes, processorType, typeof(IRequestErrorProcessor<,>), nameof(processorType));

    /// <summary>
    /// Adds a specific error processor to the pipeline and places it in a stage.
    /// </summary>
    /// <param name="processorType">Processor type implementing <see cref="IRequestErrorProcessor{TRequest, TResponse}"/>.</param>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestErrorProcessor(typeof(ReportFailure), PipelineStage.Observability));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestErrorProcessor(Type processorType, PipelineStage stage, int order = 0)
        => AddStaged(_requestErrorProcessorTypes, processorType, typeof(IRequestErrorProcessor<,>), nameof(processorType), stage, order);

    /// <summary>
    /// Adds a generic post-commit processor to the pipeline; it runs in its default stage.
    /// </summary>
    public EncinaConfiguration AddRequestPostCommitProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestPostCommitProcessor(typeof(TProcessor));

    /// <summary>
    /// Adds a generic post-commit processor to the pipeline and places it in a stage.
    /// </summary>
    /// <typeparam name="TProcessor">Processor type.</typeparam>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestPostCommitProcessor&lt;PublishAfterCommit&gt;(PipelineStage.Transaction, order: -200));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestPostCommitProcessor<TProcessor>(PipelineStage stage, int order = 0)
        where TProcessor : class
        => AddRequestPostCommitProcessor(typeof(TProcessor), stage, order);

    /// <summary>
    /// Adds a specific post-commit processor to the pipeline; it runs in its default stage.
    /// </summary>
    /// <param name="processorType">Processor type implementing <see cref="IRequestPostCommitProcessor{TRequest, TResponse}"/>.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public EncinaConfiguration AddRequestPostCommitProcessor(Type processorType)
        => AddStep(_requestPostCommitProcessorTypes, processorType, typeof(IRequestPostCommitProcessor<,>), nameof(processorType));

    /// <summary>
    /// Adds a specific post-commit processor to the pipeline and places it in a stage.
    /// </summary>
    /// <param name="processorType">Processor type implementing <see cref="IRequestPostCommitProcessor{TRequest, TResponse}"/>.</param>
    /// <param name="stage">Stage in which the processor runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddRequestPostCommitProcessor(typeof(PublishAfterCommit), PipelineStage.Transaction, -200));
    /// </code>
    /// </example>
    public EncinaConfiguration AddRequestPostCommitProcessor(Type processorType, PipelineStage stage, int order = 0)
        => AddStaged(_requestPostCommitProcessorTypes, processorType, typeof(IRequestPostCommitProcessor<,>), nameof(processorType), stage, order);

    /// <summary>
    /// Adds a generic stream behavior to the stream pipeline.
    /// </summary>
    public EncinaConfiguration AddStreamPipelineBehavior<TBehavior>()
        where TBehavior : class
        => AddStreamPipelineBehavior(typeof(TBehavior));

    /// <summary>
    /// Adds a generic stream behavior to the stream pipeline and places it in a stage.
    /// </summary>
    /// <typeparam name="TBehavior">Stream behavior type.</typeparam>
    /// <param name="stage">Stage in which the behavior runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddStreamPipelineBehavior&lt;StreamTimingBehavior&gt;(PipelineStage.Observability));
    /// </code>
    /// </example>
    public EncinaConfiguration AddStreamPipelineBehavior<TBehavior>(PipelineStage stage, int order = 0)
        where TBehavior : class
        => AddStreamPipelineBehavior(typeof(TBehavior), stage, order);

    /// <summary>
    /// Adds a specific stream behavior to the stream pipeline.
    /// </summary>
    /// <param name="streamBehaviorType">Behavior type implementing <see cref="IStreamPipelineBehavior{TRequest, TItem}"/>.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public EncinaConfiguration AddStreamPipelineBehavior(Type streamBehaviorType)
        => AddStep(_streamPipelineBehaviorTypes, streamBehaviorType, typeof(IStreamPipelineBehavior<,>), nameof(streamBehaviorType));

    /// <summary>
    /// Adds a specific stream behavior to the stream pipeline and places it in a stage.
    /// </summary>
    /// <param name="streamBehaviorType">Behavior type implementing <see cref="IStreamPipelineBehavior{TRequest, TItem}"/>.</param>
    /// <param name="stage">Stage in which the behavior runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina(config => config.AddStreamPipelineBehavior(typeof(StreamTimingBehavior&lt;,&gt;), PipelineStage.Observability));
    /// </code>
    /// </example>
    public EncinaConfiguration AddStreamPipelineBehavior(Type streamBehaviorType, PipelineStage stage, int order = 0)
        => AddStaged(_streamPipelineBehaviorTypes, streamBehaviorType, typeof(IStreamPipelineBehavior<,>), nameof(streamBehaviorType), stage, order);

    internal void RegisterConfiguredPipelineBehaviors(IServiceCollection services)
        => RegisterAll(services, _pipelineBehaviorTypes, typeof(IPipelineBehavior<,>));

    internal void RegisterConfiguredRequestPreProcessors(IServiceCollection services)
        => RegisterAll(services, _requestPreProcessorTypes, typeof(IRequestPreProcessor<>));

    internal void RegisterConfiguredRequestPostProcessors(IServiceCollection services)
        => RegisterAll(services, _requestPostProcessorTypes, typeof(IRequestPostProcessor<,>));

    internal void RegisterConfiguredRequestErrorProcessors(IServiceCollection services)
        => RegisterAll(services, _requestErrorProcessorTypes, typeof(IRequestErrorProcessor<,>));

    internal void RegisterConfiguredRequestPostCommitProcessors(IServiceCollection services)
        => RegisterAll(services, _requestPostCommitProcessorTypes, typeof(IRequestPostCommitProcessor<,>));

    internal void RegisterConfiguredStreamPipelineBehaviors(IServiceCollection services)
        => RegisterAll(services, _streamPipelineBehaviorTypes, typeof(IStreamPipelineBehavior<,>));

    /// <summary>
    /// Copies the registration-time stage declarations into the registry the order resolver reads.
    /// </summary>
    internal void ApplyStageDeclarations(PipelineStageRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        foreach (var declaration in _stageDeclarations)
        {
            registry.Set(declaration.StepType, declaration.Stage, declaration.Order);
        }
    }

    private EncinaConfiguration AddStaged(
        List<Type> target,
        Type stepType,
        Type genericInterface,
        string parameterName,
        PipelineStage stage,
        int order)
    {
        AddStep(target, stepType, genericInterface, parameterName);
        _stageDeclarations.Add(new PipelineStageDeclaration(stepType, stage, order));
        return this;
    }

    private EncinaConfiguration AddStep(List<Type> target, Type stepType, Type genericInterface, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(stepType, parameterName);

        if (!stepType.IsClass || stepType.IsAbstract)
        {
            throw new ArgumentException($"{stepType.Name} must be a concrete class type.", parameterName);
        }

        if (!genericInterface.IsAssignableFromGeneric(stepType))
        {
            var interfaceName = genericInterface.Name[..genericInterface.Name.IndexOf('`')];
            var arity = new string(',', genericInterface.GetGenericArguments().Length - 1);
            throw new ArgumentException($"{stepType.Name} does not implement {interfaceName}<{arity}>.", parameterName);
        }

        if (!target.Contains(stepType))
        {
            target.Add(stepType);
        }

        return this;
    }

    private static void RegisterAll(IServiceCollection services, List<Type> types, Type genericInterface)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var type in types)
        {
            var serviceType = ResolveServiceType(type, genericInterface);
            services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, type));
        }
    }

    private static Type ResolveServiceType(Type implementationType, Type genericInterface)
    {
        var interfaceType = implementationType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);

        if (implementationType.IsGenericTypeDefinition || interfaceType is null || interfaceType.ContainsGenericParameters)
        {
            return genericInterface;
        }

        return interfaceType;
    }
}

/// <summary>
/// One registration-time placement of a pipeline step type, copied into the <see cref="PipelineStageRegistry"/>.
/// </summary>
/// <param name="StepType">Behavior or processor type.</param>
/// <param name="Stage">Declared stage.</param>
/// <param name="Order">Declared order inside the stage.</param>
internal readonly record struct PipelineStageDeclaration(Type StepType, PipelineStage Stage, int Order);

internal static class TypeExtensions
{
    /// <summary>
    /// Determines whether a generic interface is assignable when considering its arguments.
    /// </summary>
    public static bool IsAssignableFromGeneric(this Type genericInterface, Type candidate)
    {
        ArgumentNullException.ThrowIfNull(genericInterface);

        if (candidate is null)
        {
            return false;
        }

        if (!genericInterface.IsInterface || !genericInterface.IsGenericType)
        {
            return genericInterface.IsAssignableFrom(candidate);
        }

        return candidate
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
    }
}
