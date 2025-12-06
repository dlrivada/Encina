using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
    public async Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var stopwatch = Stopwatch.StartNew();
        var requestName = typeof(TQuery).Name;
        const string requestKind = "query";

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();
            if (_failureDetector.TryExtractFailure(response, out var failureReason, out _))
            {
                _metrics.TrackFailure(requestKind, requestName, stopwatch.Elapsed, failureReason);
            }
            else
            {
                _metrics.TrackSuccess(requestKind, requestName, stopwatch.Elapsed);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _metrics.TrackFailure(requestKind, requestName, stopwatch.Elapsed, "cancelled");
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var reason = ex.GetType().Name;
            _metrics.TrackFailure(requestKind, requestName, stopwatch.Elapsed, reason);
            throw;
        }
    }
}
