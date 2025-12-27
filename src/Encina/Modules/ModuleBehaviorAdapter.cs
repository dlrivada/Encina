using LanguageExt;

namespace Encina.Modules;

/// <summary>
/// Adapts a module-scoped behavior to the standard pipeline behavior interface.
/// </summary>
/// <typeparam name="TModule">The module type this adapter wraps.</typeparam>
/// <typeparam name="TRequest">Request type traversing the pipeline.</typeparam>
/// <typeparam name="TResponse">Response type returned by the final handler.</typeparam>
/// <remarks>
/// <para>
/// This adapter wraps an <see cref="IModulePipelineBehavior{TModule, TRequest, TResponse}"/>
/// and only executes it when the request is being processed by a handler that belongs
/// to the target module.
/// </para>
/// <para>
/// Module association is determined by the <see cref="IModuleHandlerRegistry"/>, which
/// maps handler types to their owning modules based on assembly association.
/// </para>
/// <para>
/// If the handler doesn't belong to the target module, the adapter skips
/// the wrapped behavior and passes directly to the next step in the pipeline.
/// </para>
/// </remarks>
internal sealed class ModuleBehaviorAdapter<TModule, TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TModule : class, IModule
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// The metadata key used to store the module name in <see cref="IRequestContext"/>.
    /// </summary>
    public const string ModuleNameKey = "Encina.ModuleName";

    private readonly IModulePipelineBehavior<TModule, TRequest, TResponse> _innerBehavior;
    private readonly IModuleHandlerRegistry _handlerRegistry;
    private readonly string _targetModuleName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleBehaviorAdapter{TModule, TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="innerBehavior">The module behavior to wrap.</param>
    /// <param name="module">The target module instance to get the name from.</param>
    /// <param name="handlerRegistry">The registry that maps handlers to modules.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public ModuleBehaviorAdapter(
        IModulePipelineBehavior<TModule, TRequest, TResponse> innerBehavior,
        TModule module,
        IModuleHandlerRegistry handlerRegistry)
    {
        ArgumentNullException.ThrowIfNull(innerBehavior);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(handlerRegistry);

        _innerBehavior = innerBehavior;
        _targetModuleName = module.Name;
        _handlerRegistry = handlerRegistry;
    }

    /// <summary>
    /// Executes the wrapped behavior only if the request is being processed by the target module.
    /// </summary>
    /// <param name="request">Request being processed.</param>
    /// <param name="context">Ambient context containing module information.</param>
    /// <param name="nextStep">Callback to the next behavior or handler.</param>
    /// <param name="cancellationToken">Token to cancel the flow.</param>
    /// <returns>
    /// The result from the wrapped behavior if the module matches,
    /// or directly from the next step if skipped.
    /// </returns>
    public ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check if the handler for this request belongs to the target module
        if (!IsTargetModule())
        {
            // Skip this behavior and proceed to the next step
            return nextStep();
        }

        // Execute the module-specific behavior
        return _innerBehavior.Handle(request, context, nextStep, cancellationToken);
    }

    /// <summary>
    /// Determines whether the request handler belongs to the target module.
    /// </summary>
    /// <remarks>
    /// Uses the handler registry to look up the module that owns the handler
    /// for the current request type. This is based on assembly association
    /// established during module registration.
    /// </remarks>
    private bool IsTargetModule()
    {
        // Get the handler service type for this request
        var handlerServiceType = typeof(IRequestHandler<TRequest, TResponse>);

        // Check if the handler belongs to the target module
        return _handlerRegistry.BelongsToModule(handlerServiceType, _targetModuleName);
    }
}
