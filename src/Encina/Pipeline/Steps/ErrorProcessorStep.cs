using LanguageExt;

namespace Encina;

/// <summary>
/// Adapts one <see cref="IRequestErrorProcessor{TRequest, TResponse}"/> into a pipeline step: it calls
/// <c>nextStep</c>, observes a <c>Left</c> and returns that same <c>Left</c> unchanged. A <c>Right</c> is not observed.
/// </summary>
internal sealed class ErrorProcessorStep<TRequest, TResponse>(IRequestErrorProcessor<TRequest, TResponse> processor)
    : IPipelineBehavior<TRequest, TResponse>, IPipelineStepIdentity
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Type StepType => processor.GetType();

    /// <inheritdoc />
    public PipelineStepKind Kind => PipelineStepKind.ErrorProcessor;

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
        if (result.IsRight)
        {
            return result;
        }

        await ObserveAsync(request, context, (EncinaError)result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async ValueTask ObserveAsync(TRequest request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)
    {
        try
        {
            await processor.Process(request, context, error, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The request already failed with 'error', which the caller still returns; cancelling the
            // observer must not replace it. Any other exception is a bug in the processor and propagates.
        }
    }
}
