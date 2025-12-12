using System.Diagnostics;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Records duration and outcome metrics for mediator queries.
/// </summary>
/// <typeparam name="TQuery">Query type being observed.</typeparam>
/// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
/// <remarks>
/// Complements <see cref="CommandMetricsPipelineBehavior{TCommand,TResponse}"/> by exposing
/// visibility into reads, including functional failures.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IMediatorMetrics, MeterMediatorMetrics&gt;();
/// services.AddSimpleMediator(cfg => cfg.AddPipelineBehavior(typeof(QueryMetricsPipelineBehavior&lt;,&gt;)), assemblies);
/// </code>
/// </example>
/// <remarks>
/// Builds the behavior with the required services.
/// </remarks>
public sealed class QueryMetricsPipelineBehavior<TQuery, TResponse>(IMediatorMetrics metrics, IFunctionalFailureDetector failureDetector) : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IMediatorMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly IFunctionalFailureDetector _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, TResponse>> Handle(TQuery request, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
    {
        var requestName = typeof(TQuery).Name;
        const string requestKind = "query";

        if (!MediatorBehaviorGuards.TryValidateRequest(GetType(), request, out var failure))
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, failure.GetMediatorCode());
            return Left<MediatorError, TResponse>(failure);
        }

        if (!MediatorBehaviorGuards.TryValidateNextStep(GetType(), nextStep, out failure))
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, failure.GetMediatorCode());
            return Left<MediatorError, TResponse>(failure);
        }

        var startedAt = Stopwatch.GetTimestamp();
        Either<MediatorError, TResponse> outcome;

        try
        {
            outcome = await nextStep().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            _metrics.TrackFailure(requestKind, requestName, elapsed, "cancelled");
            return Left<MediatorError, TResponse>(MediatorErrors.Create(MediatorErrorCodes.BehaviorCancelled, $"Behavior {GetType().Name} cancelled the {typeof(TQuery).Name} request.", ex));
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var reason = ex.GetType().Name;
            _metrics.TrackFailure(requestKind, requestName, elapsed, reason);
            var error = MediatorErrors.FromException(MediatorErrorCodes.BehaviorException, ex, $"Error running {GetType().Name} for {typeof(TQuery).Name}.");
            return Left<MediatorError, TResponse>(error);
        }

        var totalElapsed = Stopwatch.GetElapsedTime(startedAt);

        _ = outcome.Match(
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
