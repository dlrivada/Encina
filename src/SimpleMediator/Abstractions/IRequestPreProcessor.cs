namespace SimpleMediator;

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
/// public sealed class EnsureCorrelationId&lt;TRequest&gt; : IRequestPreProcessor&lt;TRequest&gt;
/// {
///     public Task Process(TRequest request, CancellationToken cancellationToken)
///     {
///         CorrelationContext.Ensure();
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
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Process(TRequest request, CancellationToken cancellationToken);
}
