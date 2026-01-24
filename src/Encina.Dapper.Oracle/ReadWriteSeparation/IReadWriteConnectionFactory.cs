using System.Data;

namespace Encina.Dapper.Oracle.ReadWriteSeparation;

/// <summary>
/// Factory for creating database connections that route to the appropriate server
/// based on read/write intent (CQRS physical split).
/// </summary>
/// <remarks>
/// <para>
/// This interface provides connection creation for read/write separation scenarios
/// where commands are executed on the primary database and queries on read replicas.
/// </para>
/// <para>
/// The factory uses <c>IReadWriteConnectionSelector</c> from Encina.Messaging
/// to determine which connection string to use based on the current
/// <c>DatabaseRoutingContext</c>.
/// </para>
/// <para>
/// <b>Fallback Behavior:</b>
/// When no read replicas are configured, all methods return connections to the
/// primary database, ensuring the application functions without read/write separation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderRepository
/// {
///     private readonly IReadWriteConnectionFactory _connectionFactory;
///
///     public OrderRepository(IReadWriteConnectionFactory connectionFactory)
///     {
///         _connectionFactory = connectionFactory;
///     }
///
///     // Query operations use read replicas
///     public async Task&lt;Order?&gt; GetByIdAsync(Guid id, CancellationToken ct)
///     {
///         await using var connection = _connectionFactory.CreateReadConnection();
///         return await connection.QuerySingleOrDefaultAsync&lt;Order&gt;(
///             "SELECT * FROM Orders WHERE Id = :Id", new { Id = id });
///     }
///
///     // Command operations use the primary database
///     public async Task InsertAsync(Order order, CancellationToken ct)
///     {
///         await using var connection = _connectionFactory.CreateWriteConnection();
///         await connection.ExecuteAsync(
///             "INSERT INTO Orders (Id, CustomerId, Total) VALUES (:Id, :CustomerId, :Total)",
///             order);
///     }
/// }
/// </code>
/// </example>
public interface IReadWriteConnectionFactory
{
    /// <summary>
    /// Creates a connection to the primary (write) database.
    /// </summary>
    /// <returns>
    /// An <see cref="IDbConnection"/> configured to connect to the primary database.
    /// The connection is not opened; the caller must open it before use.
    /// </returns>
    /// <remarks>
    /// Use this method for all write operations (INSERT, UPDATE, DELETE) and for
    /// read operations that require the latest committed data (read-after-write consistency).
    /// </remarks>
    IDbConnection CreateWriteConnection();

    /// <summary>
    /// Creates a connection to a read replica database.
    /// </summary>
    /// <returns>
    /// An <see cref="IDbConnection"/> configured to connect to a read replica,
    /// or to the primary database if no replicas are configured.
    /// The connection is not opened; the caller must open it before use.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The specific replica is selected based on the configured <c>ReplicaStrategy</c>.
    /// </para>
    /// <para>
    /// Use this method for read-only queries that can tolerate eventual consistency
    /// due to replication lag.
    /// </para>
    /// </remarks>
    IDbConnection CreateReadConnection();

    /// <summary>
    /// Creates a connection based on the current routing context.
    /// </summary>
    /// <returns>
    /// An <see cref="IDbConnection"/> configured to connect to the appropriate database
    /// based on the current <c>DatabaseRoutingContext</c>.
    /// The connection is not opened; the caller must open it before use.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses the ambient <c>DatabaseRoutingContext</c>
    /// to determine which database to connect to:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <c>DatabaseIntent.Read</c>: Returns a read replica connection.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>DatabaseIntent.Write</c> or <c>DatabaseIntent.ForceWrite</c>:
    ///       Returns a primary database connection.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       No routing context: Returns a primary database connection (safe default).
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    IDbConnection CreateConnection();

    /// <summary>
    /// Creates and opens a connection to the primary (write) database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An opened <see cref="IDbConnection"/> to the primary database.
    /// </returns>
    ValueTask<IDbConnection> CreateWriteConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and opens a connection to a read replica database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An opened <see cref="IDbConnection"/> to a read replica,
    /// or to the primary database if no replicas are configured.
    /// </returns>
    ValueTask<IDbConnection> CreateReadConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and opens a connection based on the current routing context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An opened <see cref="IDbConnection"/> to the appropriate database
    /// based on the current routing context.
    /// </returns>
    ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection string for the primary (write) database.
    /// </summary>
    /// <returns>The connection string for the primary database.</returns>
    string GetWriteConnectionString();

    /// <summary>
    /// Gets a connection string for a read replica database.
    /// </summary>
    /// <returns>
    /// A connection string for a read replica, or the primary database
    /// connection string if no replicas are configured.
    /// </returns>
    string GetReadConnectionString();
}
