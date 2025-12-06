using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SimpleMediator;

/// <summary>
/// Define las métricas expuestas por SimpleMediator.
/// </summary>
/// <remarks>
/// Puede personalizarse para integrar con sistemas de observabilidad distintos (Application
/// Insights, Prometheus, etc.). La implementación por defecto usa <see cref="Meter"/>.
/// </remarks>
public interface IMediatorMetrics
{
    /// <summary>
    /// Registra la ejecución exitosa de una solicitud.
    /// </summary>
    /// <param name="requestKind">Tipo lógico del request (por ejemplo, <c>command</c> o <c>query</c>).</param>
    /// <param name="requestName">Nombre amigable del request.</param>
    /// <param name="duration">Tiempo total empleado por el pipeline.</param>
    void TrackSuccess(string requestKind, string requestName, TimeSpan duration);

    /// <summary>
    /// Registra una ejecución con fallo funcional o excepcional.
    /// </summary>
    /// <param name="requestKind">Tipo lógico del request.</param>
    /// <param name="requestName">Nombre amigable del request.</param>
    /// <param name="duration">Tiempo total antes del fallo.</param>
    /// <param name="reason">Código o descripción de la causa.</param>
    void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason);
}

/// <summary>
/// Implementación por defecto que expone métricas mediante <see cref="System.Diagnostics.Metrics"/>.
/// </summary>
/// <remarks>
/// Los instrumentos creados son:
/// <list type="bullet">
/// <item><description><c>simplemediator.request.success</c> (Counter)</description></item>
/// <item><description><c>simplemediator.request.failure</c> (Counter)</description></item>
/// <item><description><c>simplemediator.request.duration</c> (Histogram en milisegundos)</description></item>
/// </list>
/// </remarks>
public sealed class MediatorMetrics : IMediatorMetrics
{
    private static readonly Meter Meter = new("SimpleMediator", "1.0");
    private readonly Counter<long> _successCounter = Meter.CreateCounter<long>("simplemediator.request.success");
    private readonly Counter<long> _failureCounter = Meter.CreateCounter<long>("simplemediator.request.failure");
    private readonly Histogram<double> _durationHistogram = Meter.CreateHistogram<double>(
        "simplemediator.request.duration",
        unit: "ms");

    /// <inheritdoc />
    public void TrackSuccess(string requestKind, string requestName, TimeSpan duration)
    {
        var tags = new TagList
        {
            { "request.kind", requestKind },
            { "request.name", requestName }
        };

        _successCounter.Add(1, tags);
        _durationHistogram.Record(duration.TotalMilliseconds, tags);
    }

    /// <inheritdoc />
    public void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason)
    {
        var tags = new TagList
        {
            { "request.kind", requestKind },
            { "request.name", requestName }
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            tags.Add("failure.reason", reason);
        }

        _failureCounter.Add(1, tags);
        _durationHistogram.Record(duration.TotalMilliseconds, tags);
    }
}
