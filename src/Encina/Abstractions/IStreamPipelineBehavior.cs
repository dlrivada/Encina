using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LanguageExt;

namespace Encina;

/// <summary>
/// Intercepts streaming handler execution to apply cross-cutting logic on each item.
/// </summary>
/// <typeparam name="TRequest">Stream request type traversing the pipeline.</typeparam>
/// <typeparam name="TItem">Type of each item yielded by the stream.</typeparam>
/// <remarks>
/// <para>
/// Stream behaviors wrap the async enumerable returned by handlers or downstream behaviors,
/// allowing inspection, transformation, or enrichment of each yielded item.
/// </para>
/// <para>
/// Unlike <see cref="IPipelineBehavior{TRequest, TResponse}"/>, stream behaviors operate on
/// sequences rather than single values. Common use cases include logging item counts, applying
/// rate limiting, enriching items with additional data, or filtering based on runtime conditions.
/// </para>
/// <para>
/// Behaviors are chained in reverse registration order. Each one can transform the stream
/// from the next step before yielding items to the caller.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class StreamLoggingBehavior&lt;TRequest, TItem&gt; : IStreamPipelineBehavior&lt;TRequest, TItem&gt;
///     where TRequest : IStreamRequest&lt;TItem&gt;
/// {
///     public async IAsyncEnumerable&lt;Either&lt;MediatorError, TItem&gt;&gt; Handle(
///         TRequest request,
///         IRequestContext context,
///         StreamHandlerCallback&lt;TItem&gt; nextStep,
///         [EnumeratorCancellation] CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("Stream started for {Request} (correlation: {CorrelationId})",
///             typeof(TRequest).Name, context.CorrelationId);
///
///         var count = 0;
///         var errorCount = 0;
///
///         await foreach (var item in nextStep().WithCancellation(cancellationToken))
///         {
///             item.Match(
///                 Left: _ => errorCount++,
///                 Right: _ => count++);
///
///             yield return item;
///         }
///
///         _logger.LogInformation("Stream completed for {Request}: {Count} items, {ErrorCount} errors",
///             typeof(TRequest).Name, count, errorCount);
///     }
/// }
/// </code>
/// </example>
public interface IStreamPipelineBehavior<TRequest, TItem>
    where TRequest : IStreamRequest<TItem>
{
    /// <summary>
    /// Executes the behavior logic around the next pipeline element in the stream.
    /// </summary>
    /// <param name="request">Stream request being processed.</param>
    /// <param name="context">Ambient context with correlation ID, user info, tenant info, etc.</param>
    /// <param name="nextStep">Callback to the next behavior or handler stream.</param>
    /// <param name="cancellationToken">
    /// Token to cancel stream iteration.
    /// </param>
    /// <returns>
    /// Async enumerable that yields items from the next step, potentially transformed or enriched.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The <c>nextStep</c> callback returns the stream from the next behavior or handler.
    /// You must enumerate this stream (using <c>await foreach</c>) and yield its items to the caller.
    /// </para>
    /// <para>
    /// Behaviors can:
    /// <list type="bullet">
    /// <item>Transform items: <c>yield return item.Map(transform)</c></item>
    /// <item>Filter items: only yield some items based on conditions</item>
    /// <item>Enrich items: add metadata or context to each item</item>
    /// <item>Log/monitor: track item counts, errors, or performance</item>
    /// <item>Rate limit: introduce delays between items</item>
    /// </list>
    /// </para>
    /// <para>
    /// Always use <c>.WithCancellation(cancellationToken)</c> when enumerating <c>nextStep()</c>
    /// to ensure proper cancellation propagation.
    /// </para>
    /// <para>
    /// When implementing this interface, decorate the cancellation token parameter with
    /// <c>[EnumeratorCancellation]</c> attribute to ensure proper cancellation when iteration stops early.
    /// </para>
    /// </remarks>
    IAsyncEnumerable<Either<MediatorError, TItem>> Handle(
        TRequest request,
        IRequestContext context,
        StreamHandlerCallback<TItem> nextStep,
        CancellationToken cancellationToken);
}
