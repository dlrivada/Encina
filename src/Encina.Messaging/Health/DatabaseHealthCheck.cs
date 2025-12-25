using System.Data;
using System.Data.Common;

namespace Encina.Messaging.Health;

/// <summary>
/// Base class for database connectivity health checks.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that the database connection can be opened and a simple query executed.
/// It is provider-agnostic and works with any <see cref="IDbConnection"/> implementation.
/// </para>
/// <para>
/// The default query is <c>SELECT 1</c>, which is supported by most databases.
/// Override <see cref="GetHealthCheckQuery"/> to customize for specific databases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // PostgreSQL health check
/// public class PostgreSqlHealthCheck : DatabaseHealthCheck
/// {
///     public PostgreSqlHealthCheck(
///         Func&lt;IDbConnection&gt; connectionFactory,
///         ProviderHealthCheckOptions? options = null)
///         : base("encina-postgresql", connectionFactory, options)
///     {
///     }
/// }
/// </code>
/// </example>
public class DatabaseHealthCheck : EncinaHealthCheck
{
    private readonly Func<IDbConnection> _connectionFactory;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
    /// </summary>
    /// <param name="name">The name of the health check.</param>
    /// <param name="connectionFactory">Factory function to create database connections.</param>
    /// <param name="options">Optional configuration for the health check.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionFactory"/> is null.</exception>
    protected DatabaseHealthCheck(
        string name,
        Func<IDbConnection> connectionFactory,
        ProviderHealthCheckOptions? options = null)
        : base(
            options?.Name ?? name,
            options?.Tags ?? ["encina", "database", "ready"])
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <summary>
    /// Gets the SQL query used to verify database connectivity.
    /// </summary>
    /// <returns>A simple SQL query that should succeed on a healthy database.</returns>
    /// <remarks>
    /// Override this method to provide a database-specific query if <c>SELECT 1</c> is not supported.
    /// </remarks>
    protected virtual string GetHealthCheckQuery() => "SELECT 1";

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.Timeout);

        try
        {
            // Open connection if not already open
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

            // Execute health check query
            using var command = connection.CreateCommand();
            command.CommandText = GetHealthCheckQuery();
            command.CommandTimeout = (int)_options.Timeout.TotalSeconds;

            if (command is DbCommand dbCommand)
            {
                await dbCommand.ExecuteScalarAsync(cts.Token).ConfigureAwait(false);
            }
            else
            {
                command.ExecuteScalar();
            }

            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred
            return CreateFailureResult($"Database health check timed out after {_options.Timeout.TotalSeconds}s");
        }
    }

    private HealthCheckResult CreateFailureResult(string description, Exception? exception = null)
    {
        return _options.FailureStatus == HealthStatus.Degraded
            ? HealthCheckResult.Degraded(description, exception)
            : HealthCheckResult.Unhealthy(description, exception);
    }
}
