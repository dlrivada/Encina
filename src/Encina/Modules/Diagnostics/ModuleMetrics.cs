using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Modules.Diagnostics;

/// <summary>
/// Exposes modular monolith metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.modules.dispatches_total</c> (Counter) —
///   Total number of requests dispatched to modules, tagged with <c>module</c> and <c>request_type</c>.</description></item>
///   <item><description><c>encina.modules.active_count</c> (ObservableGauge) —
///   Current number of active modules.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ModuleMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _dispatchesTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleMetrics"/> class,
    /// registering all module metric instruments.
    /// </summary>
    /// <param name="callbacks">Optional callbacks for observable gauge instruments.</param>
    public ModuleMetrics(ModuleMetricsCallbacks? callbacks = null)
    {
        _dispatchesTotal = Meter.CreateCounter<long>(
            "encina.modules.dispatches_total",
            unit: "{dispatches}",
            description: "Total number of requests dispatched to modules.");

        if (callbacks is not null)
        {
            Meter.CreateObservableGauge(
                "encina.modules.active_count",
                callbacks.GetActiveModuleCount,
                unit: "{modules}",
                description: "Current number of active modules.");
        }
    }

    /// <summary>
    /// Records a request dispatch to a module.
    /// </summary>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="requestType">The request type name.</param>
    public void RecordDispatch(string moduleName, string requestType)
    {
        _dispatchesTotal.Add(1, new TagList
        {
            { "module", moduleName },
            { "request_type", requestType }
        });
    }
}

/// <summary>
/// Provides callback functions for observable module metrics gauges.
/// </summary>
public sealed class ModuleMetricsCallbacks
{
    private readonly Func<int> _getActiveModuleCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleMetricsCallbacks"/> class.
    /// </summary>
    /// <param name="getActiveModuleCount">Function that returns the current active module count.</param>
    public ModuleMetricsCallbacks(Func<int> getActiveModuleCount)
    {
        _getActiveModuleCount = getActiveModuleCount ?? throw new ArgumentNullException(nameof(getActiveModuleCount));
    }

    /// <summary>
    /// Gets the current number of active modules.
    /// </summary>
    public int GetActiveModuleCount() => _getActiveModuleCount();
}
