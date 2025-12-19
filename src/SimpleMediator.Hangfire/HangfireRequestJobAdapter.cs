using LanguageExt;
using Microsoft.Extensions.Logging;

namespace SimpleMediator.Hangfire;

/// <summary>
/// Adapter that executes IRequest{TResponse} as a Hangfire background job.
/// </summary>
/// <typeparam name="TRequest">The type of request to execute.</typeparam>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
public sealed class HangfireRequestJobAdapter<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMediator _mediator;
    private readonly ILogger<HangfireRequestJobAdapter<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public HangfireRequestJobAdapter(
        IMediator mediator,
        ILogger<HangfireRequestJobAdapter<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);

        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Executes the request through the mediator as a Hangfire job.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Either<MediatorError, TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogInformation(
                "Executing Hangfire job for request {RequestType}",
                typeof(TRequest).Name);

            var result = await _mediator.Send(request, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: _ => _logger.LogInformation(
                    "Hangfire job completed successfully for request {RequestType}",
                    typeof(TRequest).Name),
                Left: error => _logger.LogError(
                    "Hangfire job failed for request {RequestType}: {ErrorMessage}",
                    typeof(TRequest).Name,
                    error.Message));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception in Hangfire job for request {RequestType}",
                typeof(TRequest).Name);

            throw;
        }
    }
}
