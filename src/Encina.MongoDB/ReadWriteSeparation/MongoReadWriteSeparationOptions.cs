namespace Encina.MongoDB.ReadWriteSeparation;

/// <summary>
/// Configuration options for MongoDB read/write separation.
/// </summary>
/// <remarks>
/// <para>
/// These options configure how MongoDB operations are routed between primary and
/// secondary members of a replica set for CQRS physical split scenarios.
/// </para>
/// <para>
/// <b>Requirements:</b>
/// Read/write separation requires a MongoDB replica set deployment. Standalone
/// MongoDB servers do not support read preferences other than Primary.
/// </para>
/// <para>
/// <b>How it works:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       Commands (<c>ICommand&lt;T&gt;</c>) always use <see cref="MongoReadPreference.Primary"/>
///     </description>
///   </item>
///   <item>
///     <description>
///       Queries (<c>IQuery&lt;T&gt;</c>) use the configured <see cref="ReadPreference"/>
///     </description>
///   </item>
///   <item>
///     <description>
///       Queries with <c>ForceWriteDatabaseAttribute</c> use Primary
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaMongoDB(options =>
/// {
///     options.ConnectionString = "mongodb://localhost:27017/?replicaSet=rs0";
///     options.DatabaseName = "MyApp";
///     options.UseReadWriteSeparation = true;
///     options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
///     options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Majority;
/// });
/// </code>
/// </example>
public sealed class MongoReadWriteSeparationOptions
{
    /// <summary>
    /// Gets or sets the read preference for query operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This preference is applied to all read operations except those marked with
    /// <c>ForceWriteDatabaseAttribute</c> or when <c>DatabaseIntent.ForceWrite</c>
    /// is set in the routing context.
    /// </para>
    /// <para>
    /// For read/write separation scenarios, <see cref="MongoReadPreference.SecondaryPreferred"/>
    /// is recommended as it provides read offloading while maintaining availability.
    /// </para>
    /// </remarks>
    /// <value>
    /// Default: <see cref="MongoReadPreference.SecondaryPreferred"/>.
    /// </value>
    public MongoReadPreference ReadPreference { get; set; } = MongoReadPreference.SecondaryPreferred;

    /// <summary>
    /// Gets or sets the read concern level for read operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The read concern level determines the consistency guarantees for read operations.
    /// Higher levels provide stronger guarantees but may impact performance.
    /// </para>
    /// <para>
    /// For most applications, <see cref="MongoReadConcern.Majority"/> provides
    /// a good balance between consistency and performance.
    /// </para>
    /// </remarks>
    /// <value>
    /// Default: <see cref="MongoReadConcern.Majority"/>.
    /// </value>
    public MongoReadConcern ReadConcern { get; set; } = MongoReadConcern.Majority;

    /// <summary>
    /// Gets or sets whether to validate replica set configuration on startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the application verifies that:
    /// <list type="bullet">
    ///   <item><description>The MongoDB deployment is a replica set</description></item>
    ///   <item><description>At least one secondary is available for read operations</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This helps catch configuration errors early but may slow down startup.
    /// </para>
    /// </remarks>
    /// <value>
    /// Default: <see langword="false"/>.
    /// </value>
    public bool ValidateOnStartup { get; set; }

    /// <summary>
    /// Gets or sets whether to fall back to primary when no secondaries are available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled and the read preference cannot be satisfied (e.g., no secondaries
    /// available with <see cref="MongoReadPreference.Secondary"/>), the operation
    /// will automatically fall back to the primary.
    /// </para>
    /// <para>
    /// This is only relevant for <see cref="MongoReadPreference.Secondary"/> mode.
    /// <see cref="MongoReadPreference.SecondaryPreferred"/> already includes fallback behavior.
    /// </para>
    /// </remarks>
    /// <value>
    /// Default: <see langword="true"/>.
    /// </value>
    public bool FallbackToPrimaryOnNoSecondaries { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum acceptable staleness for secondary reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, secondaries that are more than this duration behind the primary
    /// will not be selected for read operations. This helps ensure that reads
    /// don't return excessively stale data.
    /// </para>
    /// <para>
    /// Set to <see langword="null"/> to disable staleness checking (any secondary
    /// can be selected regardless of replication lag).
    /// </para>
    /// <para>
    /// MongoDB requires a minimum value of 90 seconds for maxStalenessSeconds.
    /// </para>
    /// </remarks>
    /// <value>
    /// Default: <see langword="null"/> (no maximum staleness).
    /// </value>
    public TimeSpan? MaxStaleness { get; set; }
}
