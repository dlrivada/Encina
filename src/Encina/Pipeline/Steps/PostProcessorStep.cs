using LanguageExt;

namespace Encina;

/// <summary>
/// Adapts one <see cref="IRequestPostProcessor{TRequest, TResponse}"/> into a pipeline step: it calls
/// <c>nextStep</c>, runs the processor only on <c>Right</c>, and a processor <c>Left</c> fails the request.
/// </summary>
internal sealed class PostProcessorStep<TRequest, TResponse>(IRequestPostProcessor<TRequest, TResponse> processor)
    : IPipelineBehavior<TRequest, TResponse>, IPipelineStepIdentity
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Type StepType => processor.GetType();

    /// <inheritdoc />
    public PipelineStepKind Kind => PipelineStepKind.PostProcessor;

    /// <inheritdoc />
    public PipelineStage? DeclaredStage => PipelineStageLookup.Find(StepType)?.Stage;

    /// <inheritdoc />
    public int? DeclaredOrder => PipelineStageLookup.Find(StepType)?.Order;

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var result = await nextStep().ConfigureAwait(false);
        if (result.IsLeft)
        {
            return result;
        }

        var outcome = await RunAsync(request, context, (TResponse)result, cancellationToken).ConfigureAwait(false);
        return outcome.IsLeft ? Either<EncinaError, TResponse>.Left((EncinaError)outcome) : result;
    }

    private async ValueTask<Either<EncinaError, Unit>> RunAsync(TRequest request, IRequestContext context, TResponse response, CancellationToken cancellationToken)
    {
        try
        {
            return await processor.Process(request, context, response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Post-processor {processor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var metadata = new Dictionary<string, object?>
            {
                ["postProcessor"] = processor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "postprocessor"
            };
            return Either<EncinaError, Unit>.Left(EncinaErrors.Create(EncinaErrorCodes.PostProcessorCancelled, message, ex, metadata));
        }
        // Pure ROP: any other exception is a bug in the processor and propagates (fail-fast)
    }
}
