using LanguageExt;

namespace Encina;

/// <summary>
/// Executes typed logic before the inner pipeline steps and the handler for a request.
/// </summary>
/// <typeparam name="TRequest">Request type being processed.</typeparam>
/// <remarks>
/// <para>
/// A pre-processor is typed sugar over a pipeline behavior: it has no <c>next</c> to forget or call twice.
/// Its default stage is <see cref="PipelineStage.Application"/> (inside every security, compliance and
/// transaction stage); declare another with <see cref="PipelineStageAttribute"/>.
/// </para>
/// <para>
/// A <c>Left</c> result aborts the request: the inner steps and the handler do not run and the error is returned.
/// Anything that needs wrapping, timing or catching belongs in an <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class RequestStartLogger&lt;TRequest&gt;(ILogger&lt;RequestStartLogger&lt;TRequest&gt;&gt; logger)
///     : IRequestPreProcessor&lt;TRequest&gt;
/// {
///     public ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; Process(
///         TRequest request, IRequestContext context, CancellationToken cancellationToken)
///     {
///         logger.LogDebug(
///             "Starting {Request} (correlation: {CorrelationId})",
///             typeof(TRequest).Name,
///             context.CorrelationId);
///         return ValueTask.FromResult&lt;Either&lt;EncinaError, Unit&gt;&gt;(Unit.Default);
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
    /// <returns><c>Right</c> to continue; <c>Left</c> to abort the request with that error.</returns>
    ValueTask<Either<EncinaError, Unit>> Process(TRequest request, IRequestContext context, CancellationToken cancellationToken);
}
