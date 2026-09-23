using Encina.Messaging.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Encina.Quartz;

/// <summary>
/// Quartz job that executes a Encina request.
/// </summary>
/// <typeparam name="TRequest">The type of request to execute.</typeparam>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
/// <remarks>
/// <para>
/// A <c>Left</c> result from the handler fails the job:
/// </para>
/// <list type="bullet">
/// <item><description>A cancellation error (any Encina <c>*.cancelled</c> code, such as
/// <see cref="EncinaErrorCodes.RequestCancelled"/> or <see cref="EncinaErrorCodes.HandlerCancelled"/>) while
/// the job's cancellation token is cancelled throws <see cref="OperationCanceledException"/>.</description></item>
/// <item><description>Any other failure throws a <see cref="JobExecutionException"/> with
/// <see cref="JobExecutionException.RefireImmediately"/> set to <c>false</c>, whose message contains only
/// the error code and whose <see cref="Exception.Data"/> carries the error code and the
/// <see cref="IErrorClassifier"/> classification (see <see cref="EncinaJobFailureData"/>).</description></item>
/// </list>
/// </remarks>
[DisallowConcurrentExecution]
public sealed class QuartzRequestJob<TRequest, TResponse> : IJob
    where TRequest : IRequest<TResponse>
{
    private readonly IEncina _encina;
    private readonly ILogger<QuartzRequestJob<TRequest, TResponse>> _logger;
    private readonly IErrorClassifier _errorClassifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuartzRequestJob{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorClassifier">
    /// The classifier that decides whether a failure is permanent or transient. When <c>null</c>,
    /// <see cref="DefaultErrorClassifier"/> is used.
    /// </param>
    public QuartzRequestJob(
        IEncina encina,
        ILogger<QuartzRequestJob<TRequest, TResponse>> logger,
        IErrorClassifier? errorClassifier = null)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
        _errorClassifier = errorClassifier ?? new DefaultErrorClassifier();
    }

    /// <summary>
    /// Executes the Quartz job by sending the request through the Encina.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">The job was cancelled through the context's cancellation token.</exception>
    /// <exception cref="JobExecutionException">The request is missing, the handler failed, or the handler threw.</exception>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.JobDetail.JobDataMap.TryGetValue(QuartzConstants.RequestKey, out var requestObj) ||
            requestObj is not TRequest request)
        {
            Log.RequestNotFoundInJobDataMap(_logger, context.JobDetail.Key);

            throw new JobExecutionException($"Request of type {typeof(TRequest).Name} not found in JobDataMap");
        }

        var requestType = typeof(TRequest).Name;
        Either<EncinaError, TResponse> result;

        try
        {
            Log.ExecutingRequestJob(_logger, context.JobDetail.Key, requestType);

            result = await _encina.Send(request, context.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.RequestJobException(_logger, ex, context.JobDetail.Key, requestType);

            throw new JobExecutionException(ex);
        }

        result.Match(
            Right: response =>
            {
                Log.RequestJobCompleted(_logger, context.JobDetail.Key, requestType);

                // Store result in JobDataMap for retrieval. Not persisted by Quartz's default
                // RAMJobStore, but applications using AdoJobStore or a custom listener may
                // persist it outside Encina's retention/erasure controls (tracked in #1258).
                context.Result = response;
            },
            Left: error => throw ToException(error, context, requestType));
    }

    private Exception ToException(EncinaError error, IJobExecutionContext context, string requestType)
    {
        if (JobFailure.IsJobCancellation(error, context.CancellationToken))
        {
            Log.RequestJobCancelled(_logger, context.JobDetail.Key, requestType);
            return JobFailure.Cancelled(error, context.CancellationToken);
        }

        var classification = JobFailure.Classify(error, _errorClassifier);
        Log.RequestJobFailed(_logger, context.JobDetail.Key, requestType, error.GetCode().IfNone("encina.unknown"), classification.ToString());
        return JobFailure.Failed(error, classification);
    }
}
