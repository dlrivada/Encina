using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Registra métricas de duración y estado para consultas del mediador.
/// </summary>
/// <typeparam name="TQuery">Tipo de consulta observado.</typeparam>
/// <typeparam name="TResponse">Tipo devuelto por el handler.</typeparam>
/// <remarks>
/// Complementa a <see cref="CommandMetricsPipelineBehavior{TCommand,TResponse}"/> aportando visibilidad
/// sobre lecturas, incluyendo fallos funcionales.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IMediatorMetrics, MeterMediatorMetrics&gt;();
/// services.AddSimpleMediator(cfg => cfg.AddPipelineBehavior(typeof(QueryMetricsPipelineBehavior&lt;,&gt;)), assemblies);
/// </code>
/// </example>
public sealed class QueryMetricsPipelineBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IMediatorMetrics _metrics;
    private readonly IFunctionalFailureDetector _failureDetector;

    /// <summary>
    /// Crea el behavior con los servicios necesarios.
    /// </summary>
    public QueryMetricsPipelineBehavior(IMediatorMetrics metrics, IFunctionalFailureDetector failureDetector)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;
    }

    /// <inheritdoc />
    public async Task<Either<Error, TResponse>> Handle(TQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var requestName = typeof(TQuery).Name;
        const string requestKind = "query";

        if (request is null)
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, "null_request");
            var message = $"{GetType().Name} recibió un request nulo.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.null_request", message));
        }

        if (next is null)
        {
            _metrics.TrackFailure(requestKind, requestName, TimeSpan.Zero, "null_next");
            var message = $"{GetType().Name} recibió un delegado nulo.";
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
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.cancelled", $"El behavior {GetType().Name} canceló la solicitud {typeof(TQuery).Name}.", ex));
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var reason = ex.GetType().Name;
            _metrics.TrackFailure(requestKind, requestName, elapsed, reason);
            var error = MediatorErrors.FromException("mediator.behavior.exception", ex, $"Error ejecutando {GetType().Name} para {typeof(TQuery).Name}.");
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
