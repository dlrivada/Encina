using LanguageExt;

namespace Encina;

/// <summary>
/// Adapts one <see cref="IRequestPreProcessor{TRequest}"/> into a pipeline step: a <c>Left</c> aborts the request
/// before <c>nextStep</c> is called.
/// </summary>
internal sealed class PreProcessorStep<TRequest, TResponse>(IRequestPreProcessor<TRequest> processor)
    : IPipelineBehavior<TRequest, TResponse>, IPipelineStepIdentity
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Type StepType => processor.GetType();

    /// <inheritdoc />
    public PipelineStepKind Kind => PipelineStepKind.PreProcessor;

    /// <inheritdoc />
    public Type? ModuleType => null;

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
        var outcome = await RunAsync(request, context, cancellationToken).ConfigureAwait(false);
        return outcome.IsLeft
            ? Either<EncinaError, TResponse>.Left((EncinaError)outcome)
            : await nextStep().ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, Unit>> RunAsync(TRequest request, IRequestContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await processor.Process(request, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Pre-processor {processor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var metadata = new Dictionary<string, object?>
            {
                ["preProcessor"] = processor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "preprocessor"
            };
            return Either<EncinaError, Unit>.Left(EncinaErrors.Create(EncinaErrorCodes.PreProcessorCancelled, message, ex, metadata));
        }
        // Pure ROP: any other exception is a bug in the processor and propagates (fail-fast)
    }
}
