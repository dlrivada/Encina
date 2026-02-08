using System.Data;
using System.Data.Common;
using Encina.Database;

namespace Encina.Messaging.Health;

/// <summary>
/// Abstract base class for database health monitor implementations.
/// </summary>
/// <remarks>
/// <para>
/// Provides shared health check logic using a <c>SELECT 1</c> query pattern, template methods
/// for provider-specific pool statistics and pool clearing, and integrated circuit breaker
/// state tracking.
/// </para>
/// <para>
/// Each of the 13 database providers (ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1) should
/// implement a concrete subclass. ADO.NET implementations serve as the foundation that
/// Dapper and EF Core providers can delegate to.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class SqlServerDatabaseHealthMonitor : DatabaseHealthMonitorBase
/// {
///     public SqlServerDatabaseHealthMonitor(
///         Func&lt;IDbConnection&gt; connectionFactory,
///         DatabaseResilienceOptions? options = null)
///         : base("ado-sqlserver", connectionFactory, options)
///     {
///     }
///
///     protected override ConnectionPoolStats GetPoolStatisticsCore()
///     {
///         // Use SqlConnection.RetrieveStatistics() for SQL Server-specific metrics
///         return ConnectionPoolStats.CreateEmpty();
///     }
///
///     protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken)
///     {
///         SqlConnection.ClearAllPools();
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public abstract class DatabaseHealthMonitorBase : IDatabaseHealthMonitor
{
    private readonly Func<IDbConnection> _connectionFactory;
    private readonly TimeSpan _healthCheckTimeout;
    private volatile bool _isCircuitOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthMonitorBase"/> class.
    /// </summary>
    /// <param name="providerName">
    /// A lowercase, kebab-case identifier for the provider (e.g., "ado-sqlserver", "dapper-postgresql").
    /// </param>
    /// <param name="connectionFactory">Factory function to create database connections.</param>
    /// <param name="options">Optional resilience configuration. If null, defaults are used.</param>
    protected DatabaseHealthMonitorBase(
        string providerName,
        Func<IDbConnection> connectionFactory,
        DatabaseResilienceOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        ProviderName = providerName;
        _connectionFactory = connectionFactory;
        _healthCheckTimeout = options?.HealthCheckInterval > TimeSpan.Zero
            ? options.HealthCheckInterval
            : TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc />
    public string ProviderName { get; }

    /// <inheritdoc />
    public bool IsCircuitOpen => _isCircuitOpen;

    /// <inheritdoc />
    public ConnectionPoolStats GetPoolStatistics()
    {
        try
        {
            return GetPoolStatisticsCore();
        }
        catch
        {
            // If pool statistics retrieval fails, return empty stats rather than throwing
            return ConnectionPoolStats.CreateEmpty();
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_isCircuitOpen)
        {
            return DatabaseHealthResult.Unhealthy(
                $"Circuit breaker is open for provider '{ProviderName}'.");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_healthCheckTimeout);

        try
        {
            using var connection = _connectionFactory();

            if (connection.State != ConnectionState.Open)
            {
                if (connection is DbConnection dbConnection)
                {
                    await dbConnection.OpenAsync(cts.Token).ConfigureAwait(false);
                }
                else
                {
                    connection.Open();
                }
            }

            using var command = connection.CreateCommand();
            command.CommandText = GetHealthCheckQuery();
            command.CommandTimeout = (int)_healthCheckTimeout.TotalSeconds;

            if (command is DbCommand dbCommand)
            {
                await dbCommand.ExecuteScalarAsync(cts.Token).ConfigureAwait(false);
            }
            else
            {
                command.ExecuteScalar();
            }

            _isCircuitOpen = false;

            var stats = GetPoolStatistics();
            var data = new Dictionary<string, object>
            {
                ["provider"] = ProviderName,
                ["activeConnections"] = stats.ActiveConnections,
                ["idleConnections"] = stats.IdleConnections,
                ["totalConnections"] = stats.TotalConnections,
                ["poolUtilization"] = stats.PoolUtilization
            };

            // Report degraded if pool utilization is very high
            if (stats.PoolUtilization > 0.9)
            {
                return DatabaseHealthResult.Degraded(
                    $"Pool utilization is high ({stats.PoolUtilization:P0}) for provider '{ProviderName}'.",
                    data: data);
            }

            return DatabaseHealthResult.Healthy(
                $"Database connection is healthy for provider '{ProviderName}'.",
                data: data);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return DatabaseHealthResult.Unhealthy(
                $"Database health check timed out after {_healthCheckTimeout.TotalSeconds}s for provider '{ProviderName}'.");
        }
        catch (Exception ex)
        {
            _isCircuitOpen = true;
            return DatabaseHealthResult.Unhealthy(
                $"Database health check failed for provider '{ProviderName}': {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task ClearPoolAsync(CancellationToken cancellationToken = default)
    {
        await ClearPoolCoreAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the SQL query used to verify database connectivity.
    /// </summary>
    /// <returns>A simple SQL query that should succeed on a healthy database.</returns>
    /// <remarks>
    /// The default implementation returns <c>SELECT 1</c>, which is supported by most databases.
    /// Override this method if the provider requires a different health check query.
    /// </remarks>
    protected virtual string GetHealthCheckQuery() => "SELECT 1";

    /// <summary>
    /// Gets provider-specific connection pool statistics.
    /// </summary>
    /// <returns>A <see cref="ConnectionPoolStats"/> snapshot.</returns>
    /// <remarks>
    /// Providers that do not support pool statistics should return
    /// <see cref="ConnectionPoolStats.CreateEmpty()"/>.
    /// </remarks>
    protected abstract ConnectionPoolStats GetPoolStatisticsCore();

    /// <summary>
    /// Clears the connection pool using provider-specific APIs.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the pool has been cleared.</returns>
    /// <remarks>
    /// Providers that do not support pool clearing should return <see cref="Task.CompletedTask"/>.
    /// </remarks>
    protected abstract Task ClearPoolCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets the circuit breaker state. Can be called by subclasses to synchronize
    /// circuit state with external circuit breaker infrastructure.
    /// </summary>
    /// <param name="isOpen"><c>true</c> to open the circuit; <c>false</c> to close it.</param>
    protected void SetCircuitState(bool isOpen)
    {
        _isCircuitOpen = isOpen;
    }
}
