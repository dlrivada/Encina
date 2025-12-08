using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Records duration and outcome metrics for mediator commands.
/// </summary>
/// <typeparam name="TCommand">Command type being observed.</typeparam>
/// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
/// <remarks>
/// Uses <see cref="IMediatorMetrics"/> to expose success/failure counters and a duration
/// histogram. The failure detector identifies functional errors without exceptions.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IMediatorMetrics, MeterMediatorMetrics&gt;();
/// services.AddSimpleMediator(cfg => cfg.AddPipelineBehavior(typeof(CommandMetricsPipelineBehavior&lt;,&gt;)), assemblies);
/// </code>
/// </example>
public sealed class CommandMetricsPipelineBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IMediatorMetrics _metrics;
    private readonly IFunctionalFailureDetector _failureDetector;

    /// <summary>
    /// Builds the behavior using the metrics service and failure detector.
    /// </summary>
    public CommandMetricsPipelineBehavior(IMediatorMetrics metrics, IFunctionalFailureDetector failureDetector)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;
    }

    /// <inheritdoc />
    public async Task<Either<Error, TResponse>> Handle(TCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var requestName = typeof(TCommand).Name;
        const string requestKind = "command";

        if (request is null)
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, "null_request");
            var message = $"{GetType().Name} received a null request.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.null_request", message));
        }

        if (next is null)
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, "null_next");
            var message = $"{GetType().Name} received a null delegate.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.null_next", message));
        }

        var startedAt = Stopwatch.GetTimestamp();
        Either<Error, TResponse> outcome;

        try
        {
            outcome = await next().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            _metrics.TrackFailure(requestKind, requestName, elapsed, "cancelled");
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.cancelled", $"Behavior {GetType().Name} cancelled the {typeof(TCommand).Name} request.", ex));
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var reason = ex.GetType().Name;
            _metrics.TrackFailure(requestKind, requestName, elapsed, reason);
            var error = MediatorErrors.FromException("mediator.behavior.exception", ex, $"Error running {GetType().Name} for {typeof(TCommand).Name}.");
            return Left<Error, TResponse>(error);
        }

        var totalElapsed = Stopwatch.GetElapsedTime(startedAt);

        outcome.Match(
            Right: response =>
            {
                if (_failureDetector.TryExtractFailure(response, out var failureReason, out _))
                {
                    _metrics.TrackFailure(requestKind, requestName, totalElapsed, failureReason);
                }
                else
                {
                    _metrics.TrackSuccess(requestKind, requestName, totalElapsed);
                }

                return Unit.Default;
            },
            Left: error =>
            {
                var effectiveError = error;
                _metrics.TrackFailure(requestKind, requestName, totalElapsed, effectiveError.GetMediatorCode());
                return Unit.Default;
            });

        return outcome;
    }
}
