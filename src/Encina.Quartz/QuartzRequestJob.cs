using Microsoft.Extensions.Logging;
using Quartz;

namespace Encina.Quartz;

/// <summary>
/// Quartz job that executes a Encina request.
/// </summary>
/// <typeparam name="TRequest">The type of request to execute.</typeparam>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
[DisallowConcurrentExecution]
public sealed class QuartzRequestJob<TRequest, TResponse> : IJob
    where TRequest : IRequest<TResponse>
{
    private readonly IEncina _encina;
    private readonly ILogger<QuartzRequestJob<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuartzRequestJob{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    public QuartzRequestJob(
        IEncina encina,
        ILogger<QuartzRequestJob<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
    }

    /// <summary>
    /// Executes the Quartz job by sending the request through the Encina.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestObj = context.JobDetail.JobDataMap.Get(QuartzConstants.RequestKey);

        if (requestObj is not TRequest request)
        {
            Log.RequestNotFoundInJobDataMap(_logger, context.JobDetail.Key);

            throw new JobExecutionException($"Request of type {typeof(TRequest).Name} not found in JobDataMap");
        }

        try
        {
            Log.ExecutingRequestJob(_logger, context.JobDetail.Key, typeof(TRequest).Name);

            var result = await _encina.Send(request, context.CancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: response =>
                {
                    Log.RequestJobCompleted(_logger, context.JobDetail.Key, typeof(TRequest).Name);

                    // Store result in JobDataMap for retrieval
                    context.Result = response;
                },
                Left: error =>
                {
                    Log.RequestJobFailed(_logger, context.JobDetail.Key, typeof(TRequest).Name, error.Message);

                    // Throw to trigger Quartz retry mechanism
                    throw new JobExecutionException(error.Message);
                });
        }
        catch (Exception ex) when (ex is not JobExecutionException)
        {
            Log.RequestJobException(_logger, ex, context.JobDetail.Key, typeof(TRequest).Name);

            throw new JobExecutionException(ex);
        }
    }
}
