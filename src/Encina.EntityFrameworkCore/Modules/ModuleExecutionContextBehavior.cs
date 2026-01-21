using Encina.Modules;
using Encina.Modules.Isolation;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Modules;

/// <summary>
/// Pipeline behavior that sets the module execution context based on the handler's owning module.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior is responsible for establishing the ambient module context before
/// handler execution. This context is then used by <see cref="ModuleSchemaValidationInterceptor"/>
/// to validate SQL schema access.
/// </para>
/// <para>
/// <b>Execution Flow</b>:
/// <list type="number">
/// <item><description>Determine the handler service type from the request/response types</description></item>
/// <item><description>Look up the handler's owning module from <see cref="IModuleHandlerRegistry"/></description></item>
/// <item><description>Set the module in <see cref="IModuleExecutionContext"/></description></item>
/// <item><description>Execute the next step in the pipeline</description></item>
/// <item><description>Clear the module context in finally block (via IDisposable scope)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Handler Resolution</b>:
/// The handler service type is derived from the request and response generic type parameters.
/// If no module is found for the handler, the behavior passes through without setting
/// a module context.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Handler in OrderModule will have "Orders" module context set
/// public class CreateOrderHandler : IRequestHandler&lt;CreateOrderCommand, Order&gt;
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, Order&gt;&gt; Handle(...)
///     {
///         // During this execution, IModuleExecutionContext.CurrentModule == "Orders"
///         // Any SQL executed will be validated against Orders module's allowed schemas
///     }
/// }
/// </code>
/// </example>
public sealed class ModuleExecutionContextBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleHandlerRegistry _handlerRegistry;
    private readonly ILogger<ModuleExecutionContextBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleExecutionContextBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="moduleContext">The module execution context.</param>
    /// <param name="handlerRegistry">The module handler registry.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ModuleExecutionContextBehavior(
        IModuleExecutionContext moduleContext,
        IModuleHandlerRegistry handlerRegistry,
        ILogger<ModuleExecutionContextBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(moduleContext);
        ArgumentNullException.ThrowIfNull(handlerRegistry);
        ArgumentNullException.ThrowIfNull(logger);

        _moduleContext = moduleContext;
        _handlerRegistry = handlerRegistry;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Get the handler service type from the request/response types
        // This is the same approach used by ModuleBehaviorAdapter
        var handlerServiceType = typeof(IRequestHandler<TRequest, TResponse>);

        // Look up the module for this handler
        var moduleName = _handlerRegistry.GetModuleName(handlerServiceType);
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            Log.NoModuleFoundForHandler(_logger, handlerServiceType.Name, context.CorrelationId);
            return await nextStep();
        }

        // Set module context and execute using the disposable scope pattern
        Log.SettingModuleContext(_logger, moduleName, typeof(TRequest).Name, context.CorrelationId);

        using (_moduleContext.CreateScope(moduleName))
        {
            try
            {
                return await nextStep();
            }
            finally
            {
                Log.ClearingModuleContext(_logger, typeof(TRequest).Name, context.CorrelationId);
            }
        }
    }
}
