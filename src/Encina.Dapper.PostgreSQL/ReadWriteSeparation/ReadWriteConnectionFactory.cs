using System.Data;
using Encina.Messaging.ReadWriteSeparation;
using Npgsql;

namespace Encina.Dapper.PostgreSQL.ReadWriteSeparation;

/// <summary>
/// PostgreSQL implementation of <see cref="IReadWriteConnectionFactory"/> that routes
/// connections to the primary database or read replicas based on routing context.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates <see cref="NpgsqlConnection"/> instances configured for either
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
/// services.AddEncinaDapperPostgreSQL(connectionString, config =>
/// {
///     config.UseReadWriteSeparation = true;
///     config.ReadWriteSeparationOptions.WriteConnectionString = "Host=primary;...";
///     config.ReadWriteSeparationOptions.ReadConnectionStrings.Add("Host=replica;...");
/// });
///
/// // Usage
/// public class OrderService(IReadWriteConnectionFactory connectionFactory)
/// {
///     public async Task&lt;Order?&gt; GetOrderAsync(Guid id, CancellationToken ct)
///     {
///         // Uses read replica automatically
///         await using var conn = await connectionFactory.CreateReadConnectionAsync(ct);
///         return await conn.QuerySingleOrDefaultAsync&lt;Order&gt;(
///             "SELECT * FROM Orders WHERE Id = @Id", new { Id = id });
///     }
///
///     public async Task SaveOrderAsync(Order order, CancellationToken ct)
///     {
///         // Uses primary database
///         await using var conn = await connectionFactory.CreateWriteConnectionAsync(ct);
///         await conn.ExecuteAsync("INSERT INTO Orders ...", order);
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
        return new NpgsqlConnection(connectionString);
    }

    /// <inheritdoc/>
    public IDbConnection CreateReadConnection()
    {
        var connectionString = _connectionSelector.GetReadConnectionString();
        return new NpgsqlConnection(connectionString);
    }

    /// <inheritdoc/>
    public IDbConnection CreateConnection()
    {
        var connectionString = _connectionSelector.GetConnectionString();
        return new NpgsqlConnection(connectionString);
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateWriteConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = (NpgsqlConnection)CreateWriteConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateReadConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = (NpgsqlConnection)CreateReadConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = (NpgsqlConnection)CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public string GetWriteConnectionString() => _connectionSelector.GetWriteConnectionString();

    /// <inheritdoc/>
    public string GetReadConnectionString() => _connectionSelector.GetReadConnectionString();
}
