using System.Data;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.Data.SqlClient;

namespace Encina.ADO.SqlServer.ReadWriteSeparation;

/// <summary>
/// SQL Server implementation of <see cref="IReadWriteConnectionFactory"/> that routes
/// connections to the primary database or read replicas based on routing context.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates <see cref="SqlConnection"/> instances configured for either
/// the primary (write) database or one of the configured read replicas based on
/// the current <see cref="DatabaseRoutingContext"/>.
/// </para>
/// <para>
/// The factory uses <see cref="IReadWriteConnectionSelector"/> to determine the
/// appropriate connection string, which handles replica selection strategy
/// (RoundRobin, Random, or LeastConnections).
/// </para>
/// <para>
/// <b>Fallback Behavior:</b>
/// When no read replicas are configured, all connection methods return connections
/// to the primary database, ensuring the application functions correctly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaADO(connectionString, config =>
/// {
///     config.UseReadWriteSeparation = true;
///     config.ReadWriteSeparationOptions.WriteConnectionString = "Server=primary;...";
///     config.ReadWriteSeparationOptions.ReadConnectionStrings.Add("Server=replica;...");
/// });
///
/// // Usage
/// public class OrderService(IReadWriteConnectionFactory connectionFactory)
/// {
///     public async Task&lt;Order?&gt; GetOrderAsync(Guid id, CancellationToken ct)
///     {
///         // Uses read replica automatically
///         await using var conn = await connectionFactory.CreateReadConnectionAsync(ct);
///         using var command = conn.CreateCommand();
///         command.CommandText = "SELECT * FROM Orders WHERE Id = @Id";
///         // ... execute query
///     }
///
///     public async Task SaveOrderAsync(Order order, CancellationToken ct)
///     {
///         // Uses primary database
///         await using var conn = await connectionFactory.CreateWriteConnectionAsync(ct);
///         using var command = conn.CreateCommand();
///         command.CommandText = "INSERT INTO Orders ...";
///         // ... execute command
///     }
/// }
/// </code>
/// </example>
public sealed class ReadWriteConnectionFactory : IReadWriteConnectionFactory
{
    private readonly IReadWriteConnectionSelector _connectionSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteConnectionFactory"/> class.
    /// </summary>
    /// <param name="connectionSelector">
    /// The connection selector that provides connection strings based on routing context.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connectionSelector"/> is <see langword="null"/>.
    /// </exception>
    public ReadWriteConnectionFactory(IReadWriteConnectionSelector connectionSelector)
    {
        ArgumentNullException.ThrowIfNull(connectionSelector);
        _connectionSelector = connectionSelector;
    }

    /// <inheritdoc/>
    public IDbConnection CreateWriteConnection()
    {
        var connectionString = _connectionSelector.GetWriteConnectionString();
        return new SqlConnection(connectionString);
    }

    /// <inheritdoc/>
    public IDbConnection CreateReadConnection()
    {
        var connectionString = _connectionSelector.GetReadConnectionString();
        return new SqlConnection(connectionString);
    }

    /// <inheritdoc/>
    public IDbConnection CreateConnection()
    {
        var connectionString = _connectionSelector.GetConnectionString();
        return new SqlConnection(connectionString);
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateWriteConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = (SqlConnection)CreateWriteConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateReadConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = (SqlConnection)CreateReadConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = (SqlConnection)CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public string GetWriteConnectionString() => _connectionSelector.GetWriteConnectionString();

    /// <inheritdoc/>
    public string GetReadConnectionString() => _connectionSelector.GetReadConnectionString();
}
