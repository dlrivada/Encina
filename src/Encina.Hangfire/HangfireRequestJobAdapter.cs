using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Hangfire;

/// <summary>
/// Adapter that executes IRequest{TResponse} as a Hangfire background job.
/// </summary>
/// <typeparam name="TRequest">The type of request to execute.</typeparam>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
public sealed class HangfireRequestJobAdapter<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEncina _Encina;
    private readonly ILogger<HangfireRequestJobAdapter<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="Encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    public HangfireRequestJobAdapter(
        IEncina Encina,
        ILogger<HangfireRequestJobAdapter<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(Encina);
        ArgumentNullException.ThrowIfNull(logger);

        _Encina = Encina;
        _logger = logger;
    }

    /// <summary>
    /// Executes the request through the Encina as a Hangfire job.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Either<EncinaError, TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            Log.ExecutingRequestJob(_logger, typeof(TRequest).Name);

            var result = await _Encina.Send(request, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: _ => Log.RequestJobCompleted(_logger, typeof(TRequest).Name),
                Left: error => Log.RequestJobFailed(_logger, typeof(TRequest).Name, error.Message));

            return result;
        }
        catch (Exception ex)
        {
            Log.RequestJobException(_logger, ex, typeof(TRequest).Name);

            throw;
        }
    }
}
