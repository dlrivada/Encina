using System.Diagnostics;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Records duration and outcome metrics for Encina commands.
/// </summary>
/// <typeparam name="TCommand">Command type being observed.</typeparam>
/// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
/// <remarks>
/// Uses <see cref="IEncinaMetrics"/> to expose success/failure counters and a duration
/// histogram. The failure detector identifies functional errors without exceptions.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IEncinaMetrics, MeterEncinaMetrics&gt;();
/// services.AddEncina(cfg => cfg.AddPipelineBehavior(typeof(CommandMetricsPipelineBehavior&lt;,&gt;)), assemblies);
/// </code>
/// </example>
/// <remarks>
/// Builds the behavior using the metrics service and failure detector.
/// </remarks>
public sealed class CommandMetricsPipelineBehavior<TCommand, TResponse>(IEncinaMetrics metrics, IFunctionalFailureDetector failureDetector) : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IEncinaMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly IFunctionalFailureDetector _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(TCommand request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
    {
        var requestName = typeof(TCommand).Name;
        const string requestKind = "command";

        if (!EncinaBehaviorGuards.TryValidateRequest(GetType(), request, out var failure))
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, failure.GetEncinaCode());
            return Left<EncinaError, TResponse>(failure);
        }

        if (!EncinaBehaviorGuards.TryValidateNextStep(GetType(), nextStep, out failure))
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, failure.GetEncinaCode());
            return Left<EncinaError, TResponse>(failure);
        }

        var startedAt = Stopwatch.GetTimestamp();
        Either<EncinaError, TResponse> outcome;

        try
        {
            outcome = await nextStep().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            _metrics.TrackFailure(requestKind, requestName, elapsed, "cancelled");
            return Left<EncinaError, TResponse>(EncinaErrors.Create(EncinaErrorCodes.BehaviorCancelled, $"Behavior {GetType().Name} cancelled the {typeof(TCommand).Name} request.", ex));
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var reason = ex.GetType().Name;
            _metrics.TrackFailure(requestKind, requestName, elapsed, reason);
            var error = EncinaErrors.FromException(EncinaErrorCodes.BehaviorException, ex, $"Error running {GetType().Name} for {typeof(TCommand).Name}.");
            return Left<EncinaError, TResponse>(error);
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
                _metrics.TrackFailure(requestKind, requestName, totalElapsed, effectiveError.GetEncinaCode());
                return Unit.Default;
            });

        return outcome;
    }
}
