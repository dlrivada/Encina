using LanguageExt;

namespace Encina;

/// <summary>
/// Executes typed logic after the handler succeeded for a given request.
/// </summary>
/// <typeparam name="TRequest">Observed request type.</typeparam>
/// <typeparam name="TResponse">Response type emitted by the handler.</typeparam>
/// <remarks>
/// <para>
/// A post-processor runs only when the inner pipeline returned <c>Right</c>. Use
/// <see cref="IRequestErrorProcessor{TRequest, TResponse}"/> to observe failures. A <c>Left</c> returned by a
/// post-processor fails the request: errors are never swallowed.
/// </para>
/// <para>
/// Its default stage is <see cref="PipelineStage.Application"/>; declare another with
/// <see cref="PipelineStageAttribute"/>. Anything that needs wrapping, timing or catching belongs in an
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class AuditLogPostProcessor&lt;TRequest, TResponse&gt;(IAuditLogger auditLogger)
///     : IRequestPostProcessor&lt;TRequest, TResponse&gt;
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; Process(
///         TRequest request,
///         IRequestContext context,
///         TResponse response,
///         CancellationToken cancellationToken)
///     {
///         await auditLogger.LogAsync(typeof(TRequest).Name, context.CorrelationId, cancellationToken);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface IRequestPostProcessor<in TRequest, TResponse>
{
    /// <summary>
    /// Executes the post-processing logic using the request and the successful response.
    /// </summary>
    /// <param name="request">Original request.</param>
    /// <param name="context">Request context with correlation ID, user info, etc.</param>
    /// <param name="response">Response returned by the inner pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>Right</c> to keep the response; <c>Left</c> to fail the request with that error.</returns>
    ValueTask<Either<EncinaError, Unit>> Process(
        TRequest request,
        IRequestContext context,
        TResponse response,
        CancellationToken cancellationToken);
}
