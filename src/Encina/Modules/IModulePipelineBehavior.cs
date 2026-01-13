using LanguageExt;

namespace Encina.Modules;

/// <summary>
/// Intercepts handler execution for requests within a specific module.
/// </summary>
/// <typeparam name="TModule">The module type this behavior applies to.</typeparam>
/// <typeparam name="TRequest">Request type traversing the pipeline.</typeparam>
/// <typeparam name="TResponse">Response type returned by the final handler.</typeparam>
/// <remarks>
/// <para>
/// Module-scoped behaviors only execute for requests that are handled within the specified module.
/// This enables module-specific cross-cutting concerns like auditing, caching, or authorization
/// without affecting handlers in other modules.
/// </para>
/// <para>
/// Behaviors are chained in reverse registration order, similar to global <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
/// Each one decides whether to invoke the next step or short-circuit the flow with its own response.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderAuditBehavior&lt;TRequest, TResponse&gt;
///     : IModulePipelineBehavior&lt;OrderModule, TRequest, TResponse&gt;
///     where TRequest : IRequest&lt;TResponse&gt;
/// {
///     private readonly IAuditLog _auditLog;
///
///     public OrderAuditBehavior(IAuditLog auditLog)
///     {
///         _auditLog = auditLog;
///     }
///
///     public async ValueTask&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(
///         TRequest request,
///         IRequestContext context,
///         RequestHandlerCallback&lt;TResponse&gt; nextStep,
///         CancellationToken cancellationToken)
///     {
///         _auditLog.Log($"Order operation: {typeof(TRequest).Name}");
///         return await nextStep().ConfigureAwait(false);
///     }
/// }
///
/// // Registration
/// services.AddEncinaModuleBehavior&lt;OrderModule, OrderAuditBehavior&lt;,&gt;&gt;();
/// </code>
/// </example>
public interface IModulePipelineBehavior<TModule, TRequest, TResponse> // NOSONAR S2326: TModule provides module-scoped behavior routing
    where TModule : class, IModule
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Executes the module-specific behavior logic around the next pipeline element.
    /// </summary>
    /// <param name="request">Request being processed.</param>
    /// <param name="context">Ambient context with correlation ID, user info, module info, etc.</param>
    /// <param name="nextStep">Callback to the next behavior or handler.</param>
    /// <param name="cancellationToken">Token to cancel the flow.</param>
    /// <returns>Final result or the modified response from the behavior.</returns>
    /// <remarks>
    /// This method is only called when the request is being handled by a handler within the
    /// <typeparamref name="TModule"/>. The <paramref name="context"/> contains the module name
    /// via <see cref="IRequestContext.Metadata"/> with key <c>"ModuleName"</c>.
    /// </remarks>
    ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}
