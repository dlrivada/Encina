namespace Encina;

/// <summary>
/// Describes the resolved step order of a pipeline without executing it.
/// </summary>
/// <remarks>
/// Steps are returned outermost first, in the order they run on the way in. <c>AddEncina</c> registers the default
/// implementation. A pipeline that breaks an order rule throws <see cref="InvalidOperationException"/>, as it does on its
/// first request. Describing a pipeline instantiates its steps in a scope, validates the order and caches it, so the
/// first call closes the stage overrides: a later <c>AddEncina</c> call that declares a stage on the same service
/// collection fails.
/// </remarks>
/// <example>
/// <code>
/// public static void PrintPipeline(IEncinaPipelineInspector inspector)
/// {
///     foreach (var step in inspector.Describe&lt;PlaceOrder, OrderId&gt;())
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
    /// <returns>The steps in execution order.</returns>
    IReadOnlyList<PipelineStepDescription> Describe<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>;

    /// <summary>
    /// Describes the pipeline of a stream request, outermost step first.
    /// </summary>
    /// <typeparam name="TRequest">Stream request type.</typeparam>
    /// <typeparam name="TItem">Item type yielded by the stream.</typeparam>
    /// <returns>The steps in execution order.</returns>
    IReadOnlyList<PipelineStepDescription> DescribeStream<TRequest, TItem>()
        where TRequest : IStreamRequest<TItem>;
}
