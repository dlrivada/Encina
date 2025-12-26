namespace Encina.Messaging.Health;

/// <summary>
/// Represents a health check that is associated with a specific module.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="IEncinaHealthCheck"/> to add module-specific context.
/// Implementations of this interface are automatically discovered and registered when
/// using the modular monolith pattern with <c>AddEncinaModules()</c>.
/// </para>
/// <para>
/// The <see cref="ModuleName"/> property allows health check aggregation and filtering
/// by module, making it easy to identify which module is experiencing issues.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrdersDatabaseHealthCheck : IModuleHealthCheck
/// {
///     public string Name => "orders-database";
///     public string ModuleName => "Orders";
///     public IReadOnlyCollection&lt;string&gt; Tags => ["database", "module", "orders"];
///
///     public async Task&lt;HealthCheckResult&gt; CheckHealthAsync(CancellationToken cancellationToken)
///     {
///         var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
///         return canConnect
///             ? HealthCheckResult.Healthy("Orders database is accessible")
///             : HealthCheckResult.Unhealthy("Cannot connect to Orders database");
///     }
/// }
/// </code>
/// </example>
public interface IModuleHealthCheck : IEncinaHealthCheck
{
    /// <summary>
    /// Gets the name of the module this health check belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should match the <c>Name</c> property of the module that owns this health check.
    /// It is used for grouping health checks by module in aggregated health reports.
    /// </para>
    /// <para>
    /// For example, if the module name is "Orders", all health checks related to the
    /// Orders module should return "Orders" from this property.
    /// </para>
    /// </remarks>
    string ModuleName { get; }
}
