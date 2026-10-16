using LanguageExt;

namespace Encina;

/// <summary>
/// Adapts one <see cref="IRequestPostCommitProcessor{TRequest, TResponse}"/> into a pipeline step. It behaves as a
/// post-processor step whose default position is the <see cref="PipelineStage.Transaction"/> stage with
/// <c>Order = -100</c>, outside the transaction behavior: it runs only when the inner pipeline, including the commit,
/// returned <c>Right</c>, and never after a rollback.
/// </summary>
internal sealed class PostCommitProcessorStep<TRequest, TResponse>(IRequestPostCommitProcessor<TRequest, TResponse> processor)
    : IPipelineBehavior<TRequest, TResponse>, IPipelineStepIdentity
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Type StepType => processor.GetType();

    /// <inheritdoc />
    public PipelineStepKind Kind => PipelineStepKind.PostCommitProcessor;

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
            var message = $"Post-commit processor {processor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            var metadata = new Dictionary<string, object?>
            {
                ["postCommitProcessor"] = processor.GetType().FullName,
                ["request"] = typeof(TRequest).FullName,
                ["stage"] = "postcommitprocessor"
            };
            return Either<EncinaError, Unit>.Left(EncinaErrors.Create(EncinaErrorCodes.PostCommitProcessorCancelled, message, ex, metadata));
        }
        // Pure ROP: any other exception is a bug in the processor and propagates (fail-fast)
    }
}
