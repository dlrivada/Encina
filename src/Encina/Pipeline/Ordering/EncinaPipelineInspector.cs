using Microsoft.Extensions.DependencyInjection;

namespace Encina;

/// <summary>
/// Default <see cref="IEncinaPipelineInspector"/>: resolves the steps of a pipeline in its own scope, asks the
/// <see cref="PipelineOrderResolver"/> for the order (which validates it) and maps every step to a
/// <see cref="PipelineStepDescription"/>.
/// </summary>
/// <remarks>
/// The description is what the builders would run: the same candidates, the same resolver and the same cache. A pipeline
/// that breaks an order rule throws <see cref="InvalidOperationException"/> here exactly as it does on its first request.
/// </remarks>
internal sealed class EncinaPipelineInspector : IEncinaPipelineInspector
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PipelineOrderResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaPipelineInspector"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory of the scope the steps are resolved in.</param>
    /// <param name="resolver">The resolver that orders and validates the steps.</param>
    public EncinaPipelineInspector(IServiceScopeFactory scopeFactory, PipelineOrderResolver resolver)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public IReadOnlyList<PipelineStepDescription> Describe<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            var steps = PipelineBuilder<TRequest, TResponse>.ResolveSteps(scope.ServiceProvider);
            return DescribeSteps(typeof(IPipelineBehavior<TRequest, TResponse>), steps);
        }
        finally
        {
            DisposeScope(scope);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PipelineStepDescription> DescribeStream<TRequest, TItem>()
        where TRequest : IStreamRequest<TItem>
    {
        var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            var steps = scope.ServiceProvider.GetServices<IStreamPipelineBehavior<TRequest, TItem>>().ToArray();
            return DescribeSteps(typeof(IStreamPipelineBehavior<TRequest, TItem>), steps);
        }
        finally
        {
            DisposeScope(scope);
        }
    }

    // The contract is synchronous, but a step that implements only IAsyncDisposable cannot be disposed synchronously
    private static void DisposeScope(AsyncServiceScope scope)
        => scope.DisposeAsync().AsTask().GetAwaiter().GetResult();

    private PipelineStepDescription[] DescribeSteps(Type pipelineKey, IReadOnlyList<object> steps)
    {
        if (steps.Count == 0)
        {
            return [];
        }

        var order = _resolver.Resolve(pipelineKey, steps);
        var descriptions = new PipelineStepDescription[order.Length];
        for (var i = 0; i < order.Length; i++)
        {
            var candidate = PipelineStepCandidate.For(steps[order[i]], order[i]);
            var position = _resolver.ResolvePosition(candidate);
            descriptions[i] = new PipelineStepDescription(candidate.StepType, candidate.Kind, position.Stage, position.Order, position.Source);
        }

        return descriptions;
    }
}
