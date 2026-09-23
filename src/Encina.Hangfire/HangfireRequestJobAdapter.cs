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
    /// Executes the request through Encina as a Hangfire job, without persisting the
    /// handler's response in Hangfire storage.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This is the default entry point used by <c>EnqueueRequest</c>, <c>ScheduleRequestWithDelay</c>
    /// and <c>ScheduleRequestAt</c>. Hangfire serializes and stores whatever a job method returns
    /// alongside the job in its own storage (outside Encina's retention, erasure and encryption
    /// controls), so the response is deliberately discarded here. Use
    /// <see cref="ExecuteAndReturnResultAsync"/> to opt in to persisting the response when the
    /// application explicitly needs it and the response is known not to carry personal or
    /// sensitive data (#1173).
    /// </remarks>
    /// <exception cref="OperationCanceledException">The job was cancelled through <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="EncinaJobPermanentFailureException">The handler failed with a permanent error.</exception>
    /// <exception cref="EncinaJobFailedException">The handler failed with a transient or unclassified error.</exception>
    public async Task ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        await ExecuteCoreAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the request through Encina as a Hangfire job and returns the response, which
    /// Hangfire will persist alongside the job in its own storage.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The handler's response.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Opt-in only.</strong> Hangfire stores whatever this method returns in its job
    /// storage, outside Encina's retention, erasure and encryption controls. Only enqueue a job
    /// through this method when <typeparamref name="TResponse"/> is known not to carry personal
    /// or health data. For requests whose response may contain personal data, use
    /// <see cref="ExecuteAsync"/> instead (the default used by <c>EnqueueRequest</c>) and read
    /// the outcome through Encina's own stores (outbox, audit trail) rather than Hangfire's job
    /// result.
    /// </para>
    /// <para>
    /// Failures are reported exactly like <see cref="ExecuteAsync"/>: an
    /// <see cref="OperationCanceledException"/> for a cancelled job, <see cref="EncinaJobPermanentFailureException"/>
    /// for a permanent failure, and <see cref="EncinaJobFailedException"/> otherwise. The exception
    /// message never carries <see cref="EncinaError.Message"/>, so only the response type on the
    /// success path needs to be checked for personal data.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">The job was cancelled through <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="EncinaJobPermanentFailureException">The handler failed with a permanent error.</exception>
    /// <exception cref="EncinaJobFailedException">The handler failed with a transient or unclassified error.</exception>
    public async Task<TResponse> ExecuteAndReturnResultAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteCoreAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteCoreAsync(
        TRequest request,
        CancellationToken cancellationToken)
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
        Log.RequestJobFailed(_logger, requestType, error.GetCode().IfNone("encina.unknown"));
        return JobFailure.Failed(error, classification);
    }
}
