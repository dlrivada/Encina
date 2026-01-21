namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Defines the contract for selecting between write (primary) and read (replica) connection strings.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the central abstraction for read/write database separation.
/// Provider-specific factories (DbContext factories, connection factories) use this
/// interface to obtain the appropriate connection string based on the current
/// <see cref="DatabaseRoutingContext"/>.
/// </para>
/// <para>
/// Implementations handle the logic of selecting between the primary database and
/// read replicas, including fallback behavior when no replicas are available.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyDbContextFactory : IDbContextFactory&lt;MyDbContext&gt;
/// {
///     private readonly IReadWriteConnectionSelector _selector;
///
///     public MyDbContextFactory(IReadWriteConnectionSelector selector)
///     {
///         _selector = selector;
///     }
///
///     public MyDbContext CreateDbContext()
///     {
///         var connectionString = DatabaseRoutingContext.IsReadIntent
///             ? _selector.GetReadConnectionString()
///             : _selector.GetWriteConnectionString();
///
///         return new MyDbContext(connectionString);
///     }
/// }
/// </code>
/// </example>
public interface IReadWriteConnectionSelector
{
    /// <summary>
    /// Gets the connection string for the primary (write) database.
    /// </summary>
    /// <returns>
    /// The connection string for the primary database.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the write connection string has not been configured.
    /// </exception>
    /// <remarks>
    /// This method should be used for all write operations (INSERT, UPDATE, DELETE)
    /// and for read operations that require the latest committed data.
    /// </remarks>
    string GetWriteConnectionString();

    /// <summary>
    /// Gets a connection string for a read replica database.
    /// </summary>
    /// <returns>
    /// A connection string for one of the configured read replicas, or the
    /// write connection string if no replicas are configured.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The specific replica returned depends on the configured
    /// <see cref="ReadWriteSeparationOptions.ReplicaStrategy"/>. This method
    /// automatically falls back to the write connection string if no read
    /// replicas have been configured.
    /// </para>
    /// <para>
    /// <b>Fallback Behavior:</b>
    /// If <see cref="ReadWriteSeparationOptions.ReadConnectionStrings"/> is empty,
    /// this method returns the write connection string to ensure the application
    /// continues to function (though without the benefits of read/write separation).
    /// </para>
    /// </remarks>
    string GetReadConnectionString();

    /// <summary>
    /// Gets the appropriate connection string based on the current routing context.
    /// </summary>
    /// <returns>
    /// The read connection string if the current intent is <see cref="DatabaseIntent.Read"/>
    /// and routing is enabled; otherwise, the write connection string.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that combines the logic of checking the
    /// <see cref="DatabaseRoutingContext"/> and calling the appropriate method.
    /// It returns the write connection string for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="DatabaseIntent.Write"/> intent</description></item>
    ///   <item><description><see cref="DatabaseIntent.ForceWrite"/> intent</description></item>
    ///   <item><description>No intent set (null)</description></item>
    ///   <item><description>Routing disabled (<see cref="DatabaseRoutingContext.IsEnabled"/> is false)</description></item>
    /// </list>
    /// </remarks>
    string GetConnectionString();

    /// <summary>
    /// Gets a value indicating whether read replicas are configured.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if at least one read replica is configured;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// This property can be used to conditionally enable features that depend
    /// on read/write separation or to provide diagnostic information.
    /// </remarks>
    bool HasReadReplicas { get; }
}
