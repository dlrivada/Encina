using Encina.Messaging.Recoverability;
using Microsoft.Extensions.Logging;

namespace Encina.Hangfire;

/// <summary>
/// Adapter that executes IRequest{TResponse} as a Hangfire background job.
/// </summary>
/// <typeparam name="TRequest">The type of request to execute.</typeparam>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
/// <remarks>
/// <para>
/// Hangfire records a job as failed only when the job method throws, so the adapter turns every
/// <c>Left</c> result of the handler into an exception:
/// </para>
/// <list type="bullet">
/// <item><description>A cancellation error (any Encina <c>*.cancelled</c> code, such as
/// <see cref="EncinaErrorCodes.RequestCancelled"/> or <see cref="EncinaErrorCodes.HandlerCancelled"/>) while
/// the job's cancellation token is cancelled throws <see cref="OperationCanceledException"/>, so Hangfire
/// treats the job as interrupted (for example on server shutdown) rather than failed.</description></item>
/// <item><description>A failure classified as permanent by <see cref="IErrorClassifier"/> throws
/// <see cref="EncinaJobPermanentFailureException"/>.</description></item>
/// <item><description>Any other failure throws <see cref="EncinaJobFailedException"/>, which Hangfire
/// retries.</description></item>
/// </list>
/// <para>
/// The classifier is the registered <see cref="IErrorClassifier"/> or, when none is registered,
/// <see cref="DefaultErrorClassifier"/>. See <see cref="EncinaAutomaticRetry"/> to stop Hangfire from
/// retrying permanent failures.
/// </para>
/// </remarks>
public sealed class HangfireRequestJobAdapter<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEncina _encina;
    private readonly ILogger<HangfireRequestJobAdapter<TRequest, TResponse>> _logger;
    private readonly IErrorClassifier _errorClassifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorClassifier">
    /// The classifier that decides whether a failure is permanent or transient. When <c>null</c>,
    /// <see cref="DefaultErrorClassifier"/> is used.
    /// </param>
    public HangfireRequestJobAdapter(
        IEncina encina,
        ILogger<HangfireRequestJobAdapter<TRequest, TResponse>> logger,
        IErrorClassifier? errorClassifier = null)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
        _errorClassifier = errorClassifier ?? new DefaultErrorClassifier();
    }

    /// <summary>
    /// Executes the request through the Encina as a Hangfire job.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The handler's response. Hangfire stores it as the job result.</returns>
    /// <exception cref="OperationCanceledException">The job was cancelled through <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="EncinaJobPermanentFailureException">The handler failed with a permanent error.</exception>
    /// <exception cref="EncinaJobFailedException">The handler failed with a transient or unclassified error.</exception>
    public async Task<TResponse> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest).Name;
        LanguageExt.Either<EncinaError, TResponse> result;

        try
        {
            Log.ExecutingRequestJob(_logger, requestType);

            result = await _encina.Send(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.RequestJobException(_logger, ex, requestType);

            throw;
        }

        return result.Match(
            Right: response =>
            {
                Log.RequestJobCompleted(_logger, requestType);
                return response;
            },
            Left: error => throw ToException(error, requestType, cancellationToken));
    }

    private Exception ToException(EncinaError error, string requestType, CancellationToken cancellationToken)
    {
        if (JobFailure.IsJobCancellation(error, cancellationToken))
        {
            Log.RequestJobCancelled(_logger, requestType);
            return JobFailure.Cancelled(error, cancellationToken);
        }

        var classification = JobFailure.Classify(error, _errorClassifier);
        Log.RequestJobFailed(_logger, requestType, error.GetCode().IfNone("encina.unknown"), classification.ToString(), error.Message);
        return JobFailure.Failed(error, classification);
    }
}
