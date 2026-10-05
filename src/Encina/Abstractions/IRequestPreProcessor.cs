namespace Encina;

/// <summary>
/// Executes logic before the main pipeline for a request.
/// </summary>
/// <typeparam name="TRequest">Request type being processed.</typeparam>
/// <remarks>
/// Runs before any behavior. Ideal for normalizing data, enriching context, or enforcing light
/// auditing policies.
/// </remarks>
/// <example>
/// <code>
/// public sealed class RequestStartLogger&lt;TRequest&gt;(ILogger&lt;RequestStartLogger&lt;TRequest&gt;&gt; logger)
///     : IRequestPreProcessor&lt;TRequest&gt;
/// {
///     public Task Process(TRequest request, IRequestContext context, CancellationToken cancellationToken)
///     {
///         logger.LogDebug(
///             "Starting {Request} (correlation: {CorrelationId})",
///             typeof(TRequest).Name,
///             context.CorrelationId);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestPreProcessor<in TRequest>
{
    /// <summary>
    /// Executes the pre-processing logic using the received request.
    /// </summary>
    /// <param name="request">Original request.</param>
    /// <param name="context">Request context that can be enriched with user info, tenant ID, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Pre-processors can enrich the context by calling <c>With*</c> methods.
    /// The enriched context flows to subsequent behaviors and handlers.
    /// </remarks>
    Task Process(TRequest request, IRequestContext context, CancellationToken cancellationToken);
}
