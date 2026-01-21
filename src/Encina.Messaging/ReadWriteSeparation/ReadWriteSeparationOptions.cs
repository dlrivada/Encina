namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Configuration options for read/write database separation (CQRS physical split).
/// </summary>
/// <remarks>
/// <para>
/// These options are shared across all storage providers (EF Core, Dapper, ADO.NET, MongoDB)
/// to ensure consistent read/write separation behavior. The feature routes queries to read
/// replicas and commands to the primary database.
/// </para>
/// <para>
/// <b>Benefits of Read/Write Separation:</b>
/// <list type="bullet">
///   <item><description>Offload reads to replicas for massive query parallelism</description></item>
///   <item><description>Reduce load on primary database</description></item>
///   <item><description>Improve read latency with geographically distributed replicas</description></item>
///   <item><description>Scale reads independently from writes</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Important Considerations:</b>
/// Read replicas have eventual consistency due to replication lag. For scenarios
/// requiring read-after-write consistency, use the <see cref="ForceWriteDatabaseAttribute"/>
/// on query classes that need to read from the primary database.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore(config =>
/// {
///     config.UseReadWriteSeparation = true;
///     config.ReadWriteSeparationOptions.WriteConnectionString = "Server=primary;...";
///     config.ReadWriteSeparationOptions.ReadConnectionStrings = new[]
///     {
///         "Server=replica1;...",
///         "Server=replica2;..."
///     };
///     config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.RoundRobin;
/// });
/// </code>
/// </example>
public sealed class ReadWriteSeparationOptions
{
    /// <summary>
    /// Gets or sets the connection string for the primary (write) database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All write operations (INSERT, UPDATE, DELETE) and commands are routed to this database.
    /// This connection string is also used as a fallback when no read replicas are configured
    /// or when all replicas are unhealthy.
    /// </para>
    /// <para>
    /// For Azure SQL, this should point to the primary database without the
    /// <c>ApplicationIntent=ReadOnly</c> parameter.
    /// </para>
    /// </remarks>
    /// <value>
    /// The connection string for the primary database. Default is <see langword="null"/>.
    /// </value>
    public string? WriteConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the list of connection strings for read replica databases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All read operations (queries implementing <c>IQuery</c>) are routed to one of these
    /// replicas based on the configured <see cref="ReplicaStrategy"/>.
    /// </para>
    /// <para>
    /// If this list is empty or <see langword="null"/>, all operations will use the
    /// <see cref="WriteConnectionString"/> (no read/write separation will occur).
    /// </para>
    /// <para>
    /// For Azure SQL with read-scale out, you can use the same connection string with
    /// <c>ApplicationIntent=ReadOnly</c> added to enable automatic routing to read replicas.
    /// </para>
    /// </remarks>
    /// <value>
    /// A list of connection strings for read replicas. Default is an empty list.
    /// </value>
    /// <example>
    /// <code>
    /// // Multiple dedicated replicas
    /// options.ReadConnectionStrings = new List&lt;string&gt;
    /// {
    ///     "Server=replica1;Database=MyDb;...",
    ///     "Server=replica2;Database=MyDb;..."
    /// };
    ///
    /// // Azure SQL read-scale out
    /// options.ReadConnectionStrings = new List&lt;string&gt;
    /// {
    ///     "Server=myserver.database.windows.net;Database=MyDb;ApplicationIntent=ReadOnly;..."
    /// };
    /// </code>
    /// </example>
    public IList<string> ReadConnectionStrings { get; set; } = [];

    /// <summary>
    /// Gets or sets the strategy for selecting which read replica to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The strategy determines how read requests are distributed across configured replicas.
    /// Choose based on your replica configuration and query patterns:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="ReplicaStrategy.RoundRobin"/>: Even distribution, best for similar replicas.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="ReplicaStrategy.Random"/>: Simple, good for unpredictable patterns.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="ReplicaStrategy.LeastConnections"/>: Adaptive, best for varied query times.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <value>
    /// Default: <see cref="ReplicaStrategy.RoundRobin"/>.
    /// </value>
    public ReplicaStrategy ReplicaStrategy { get; set; } = ReplicaStrategy.RoundRobin;

    /// <summary>
    /// Gets or sets whether to validate database connectivity on application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the application will attempt to connect to both the primary database
    /// and all configured read replicas during startup. If any connection fails, an
    /// exception will be thrown, preventing the application from starting with invalid
    /// database configuration.
    /// </para>
    /// <para>
    /// This is useful for catching configuration errors early but may slow down startup
    /// or cause issues in environments where databases may not be immediately available.
    /// </para>
    /// </remarks>
    /// <value>
    /// Default: <see langword="false"/>.
    /// </value>
    public bool ValidateOnStartup { get; set; }
}
