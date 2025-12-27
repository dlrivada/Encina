using System.Data;
using Encina.Messaging.Health;

namespace Encina.DistributedLock.SqlServer.Health;

/// <summary>
/// Health check for SQL Server distributed lock provider.
/// </summary>
public sealed class SqlServerDistributedLockHealthCheck : IEncinaHealthCheck
{
    private readonly string _connectionString;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDistributedLockHealthCheck"/> class.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="options">The health check options.</param>
    public SqlServerDistributedLockHealthCheck(
        string connectionString,
        ProviderHealthCheckOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(options);

        _connectionString = connectionString;
        _options = options;
    }

    /// <inheritdoc/>
    public string Name => _options.Name ?? "sqlserver-distributed-lock";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags => _options.Tags ?? ["sqlserver", "distributed-lock", "ready"];

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var testResource = $"health:lock:{Guid.NewGuid():N}";

            // Test sp_getapplock with 0 timeout
            await using var command = connection.CreateCommand();
            command.CommandText = "sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@Resource", testResource);
            command.Parameters.AddWithValue("@LockMode", "Exclusive");
            command.Parameters.AddWithValue("@LockOwner", "Session");
            command.Parameters.AddWithValue("@LockTimeout", 0);

            var returnValue = new SqlParameter("@ReturnValue", SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            };
            command.Parameters.Add(returnValue);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            var result = (int)returnValue.Value!;

            if (result >= 0)
            {
                // Release the test lock
                await using var releaseCommand = connection.CreateCommand();
                releaseCommand.CommandText = "sp_releaseapplock";
                releaseCommand.CommandType = CommandType.StoredProcedure;
                releaseCommand.Parameters.AddWithValue("@Resource", testResource);
                releaseCommand.Parameters.AddWithValue("@LockOwner", "Session");

                await releaseCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                return HealthCheckResult.Healthy();
            }

            return HealthCheckResult.Unhealthy($"sp_getapplock returned: {result}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"SQL Server connection failed: {ex.Message}");
        }
    }
}
