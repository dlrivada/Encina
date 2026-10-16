using LanguageExt;

namespace Encina;

/// <summary>
/// Executes typed logic after the transaction has committed and the request succeeded.
/// </summary>
/// <typeparam name="TRequest">Observed request type.</typeparam>
/// <typeparam name="TResponse">Response type emitted by the handler.</typeparam>
/// <remarks>
/// <para>
/// Its default stage is <see cref="PipelineStage.Transaction"/> with <see cref="PipelineStageAttribute.Order"/> -100:
/// outside the transaction behavior (order 0), so it runs after the commit returned <c>Right</c>. Without a
/// transaction behavior it runs after a successful inner pipeline. It does not run when the inner pipeline
/// returned <c>Left</c>. Startup validation of the placement arrives with a later phase of #2184.
/// </para>
/// <para>
/// A <c>Left</c> from a post-commit processor fails the request even though the commit already happened.
/// Durable side effects that must not be lost belong in the outbox, not in a post-commit hook.
/// </para>
/// <para>
/// <b>Not yet invoked:</b> the pipeline builder does not run post-commit processors until the order resolver and the
/// processor steps land in a later phase of the pipeline execution order work (#2184).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderPlacedPush&lt;TRequest, TResponse&gt;(IPushClient push)
///     : IRequestPostCommitProcessor&lt;TRequest, TResponse&gt;
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; Process(
///         TRequest request,
///         IRequestContext context,
///         TResponse response,
///         CancellationToken cancellationToken)
///     {
///         await push.NotifyAsync(context.CorrelationId, cancellationToken);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface IRequestPostCommitProcessor<in TRequest, TResponse>
{
    /// <summary>
    /// Executes the post-commit logic using the request and the successful response.
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
