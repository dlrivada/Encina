namespace Encina;

/// <summary>
/// Observes a failed request: runs only when the inner pipeline returned <c>Left</c>.
/// </summary>
/// <typeparam name="TRequest">Observed request type.</typeparam>
/// <typeparam name="TResponse">Response type of the request.</typeparam>
/// <remarks>
/// <para>
/// An error processor observes the error and cannot replace it: the original <c>Left</c> keeps flowing outwards.
/// Replacing or recovering from an error is catching, which belongs in an
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
/// </para>
/// <para>
/// Its default stage is <see cref="PipelineStage.Application"/> with <see cref="PipelineStageAttribute.Order"/> -100;
/// declare another with <see cref="PipelineStageAttribute"/>.
/// </para>
/// <para>
/// <b>Not yet invoked:</b> the pipeline builder does not run error processors until the order resolver and the
/// processor steps land in a later phase of the pipeline execution order work (#2184).
/// </para>
/// <para>
/// Do not log <see cref="EncinaError.Message"/>; record only the error code.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class FailureCounter&lt;TRequest, TResponse&gt;(IFailureMetrics metrics)
///     : IRequestErrorProcessor&lt;TRequest, TResponse&gt;
/// {
///     public ValueTask Process(
///         TRequest request,
///         IRequestContext context,
///         EncinaError failure,
///         CancellationToken cancellationToken)
///     {
///         metrics.Increment(typeof(TRequest).Name, failure.GetCode().IfNone("encina.unknown"));
///         return ValueTask.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestErrorProcessor<in TRequest, TResponse>
{
    /// <summary>
    /// Observes the error returned by the inner pipeline.
    /// </summary>
    /// <param name="request">Original request.</param>
    /// <param name="context">Request context with correlation ID, user info, etc.</param>
    /// <param name="failure">Error returned by the inner pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the observation is done.</returns>
    ValueTask Process(
        TRequest request,
        IRequestContext context,
        EncinaError failure,
        CancellationToken cancellationToken);
}
