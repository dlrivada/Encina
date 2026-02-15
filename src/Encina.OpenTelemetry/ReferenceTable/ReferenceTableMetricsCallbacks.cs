namespace Encina.OpenTelemetry.ReferenceTable;

/// <summary>
/// Provides callback delegates for reference table observable gauge metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class bridges the <c>Encina</c> sharding package with <c>Encina.OpenTelemetry</c>
/// without creating a direct project reference. The reference table infrastructure registers
/// an instance of this class with the appropriate callbacks, and the
/// <see cref="ReferenceTableMetricsInitializer"/> uses it to create the
/// <see cref="ReferenceTableMetrics"/> on startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered by Encina when reference table replication is enabled:
/// services.AddSingleton(new ReferenceTableMetricsCallbacks(
///     registeredTablesCallback: () => registry.GetAllConfigurations().Count,
///     stalenessCallback: () => stalenessData));
/// </code>
/// </example>
public sealed class ReferenceTableMetricsCallbacks
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceTableMetricsCallbacks"/> class.
    /// </summary>
    /// <param name="registeredTablesCallback">
    /// Callback that returns the current number of registered reference tables.
    /// </param>
    /// <param name="stalenessCallback">
    /// Callback that returns per-table staleness measurements as tuples of (entityType, stalenessMs).
    /// </param>
    public ReferenceTableMetricsCallbacks(
        Func<int> registeredTablesCallback,
        Func<IEnumerable<(string EntityType, double StalenessMs)>> stalenessCallback)
    {
        ArgumentNullException.ThrowIfNull(registeredTablesCallback);
        ArgumentNullException.ThrowIfNull(stalenessCallback);

        RegisteredTablesCallback = registeredTablesCallback;
        StalenessCallback = stalenessCallback;
    }

    /// <summary>
    /// Gets the callback that returns the current number of registered reference tables.
    /// </summary>
    internal Func<int> RegisteredTablesCallback { get; }

    /// <summary>
    /// Gets the callback that returns per-table staleness measurements.
    /// </summary>
    internal Func<IEnumerable<(string EntityType, double StalenessMs)>> StalenessCallback { get; }
}
