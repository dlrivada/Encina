namespace Encina;

/// <summary>
/// Describes the resolved step order of a pipeline without executing it.
/// </summary>
/// <remarks>
/// Steps are returned outermost first, in the order they run on the way in. <c>AddEncina</c> registers the default
/// implementation. A pipeline that breaks an order rule throws <see cref="InvalidOperationException"/>, as it does on its
/// first request. Describing a pipeline instantiates its steps in a scope, validates the order and caches it, so the
/// first call closes the stage overrides: a later <c>AddEncina</c> call that declares a stage on the same service
/// collection fails. The scope is disposed asynchronously, so steps that implement only <c>IAsyncDisposable</c> work.
/// </remarks>
/// <example>
/// <code>
/// public static async Task PrintPipelineAsync(IEncinaPipelineInspector inspector)
/// {
///     foreach (var step in await inspector.DescribeAsync&lt;PlaceOrder, OrderId&gt;())
///     {
///         Console.WriteLine($"{step.Stage} {step.Order} {step.StepType.Name}");
///     }
/// }
/// </code>
/// </example>
public interface IEncinaPipelineInspector
{
    /// <summary>
    /// Describes the pipeline of a request, outermost step first.
    /// </summary>
    /// <typeparam name="TRequest">Request type.</typeparam>
    /// <typeparam name="TResponse">Response type.</typeparam>
    /// <param name="cancellationToken">Token that cancels the description before the steps are resolved.</param>
    /// <returns>The steps in execution order.</returns>
    ValueTask<IReadOnlyList<PipelineStepDescription>> DescribeAsync<TRequest, TResponse>(CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;

    /// <summary>
    /// Describes the pipeline of a stream request, outermost step first.
    /// </summary>
    /// <typeparam name="TRequest">Stream request type.</typeparam>
    /// <typeparam name="TItem">Item type yielded by the stream.</typeparam>
    /// <param name="cancellationToken">Token that cancels the description before the steps are resolved.</param>
    /// <returns>The steps in execution order.</returns>
    ValueTask<IReadOnlyList<PipelineStepDescription>> DescribeStreamAsync<TRequest, TItem>(CancellationToken cancellationToken = default)
        where TRequest : IStreamRequest<TItem>;
}
