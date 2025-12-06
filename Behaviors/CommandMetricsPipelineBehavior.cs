using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Registra métricas de duración y estado para comandos ejecutados por el mediador.
/// </summary>
/// <typeparam name="TCommand">Tipo de comando observado.</typeparam>
/// <typeparam name="TResponse">Tipo devuelto por el handler.</typeparam>
/// <remarks>
/// Utiliza <see cref="IMediatorMetrics"/> para exponer contadores de éxito/fracaso y un histograma
/// de duración. El detector de fallos permite identificar errores funcionales sin excepciones.
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
    /// Crea el behavior a partir de los servicios de métricas y detector de fallos.
    /// </summary>
    public CommandMetricsPipelineBehavior(IMediatorMetrics metrics, IFunctionalFailureDetector failureDetector)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _failureDetector = failureDetector ?? NullFunctionalFailureDetector.Instance;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var stopwatch = Stopwatch.StartNew();
        var requestName = typeof(TCommand).Name;
        const string requestKind = "command";

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
