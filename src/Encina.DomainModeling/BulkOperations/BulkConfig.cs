namespace Encina.DomainModeling;

/// <summary>
/// Configuration options for bulk database operations.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record provides fine-grained control over bulk operation behavior.
/// All properties have sensible defaults through <see cref="Default"/>.
/// </para>
/// <para>
/// Use C# with-expressions to create customized configurations:
/// <code>
/// var config = BulkConfig.Default with { BatchSize = 5000, SetOutputIdentity = true };
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default configuration (BatchSize = 2000)
/// await bulkOps.BulkInsertAsync(entities);
///
/// // Custom batch size for memory-constrained environments
/// await bulkOps.BulkInsertAsync(entities, BulkConfig.Default with { BatchSize = 500 });
///
/// // Get generated IDs back after insert
/// await bulkOps.BulkInsertAsync(entities, BulkConfig.Default with { SetOutputIdentity = true });
///
/// // Update only specific properties
/// var config = BulkConfig.Default with
/// {
///     PropertiesToInclude = ["Status", "UpdatedAt"]
/// };
/// await bulkOps.BulkUpdateAsync(entities, config);
/// </code>
/// </example>
public sealed record BulkConfig
{
    /// <summary>
    /// Gets the default bulk configuration with standard settings.
    /// </summary>
    /// <value>
    /// A <see cref="BulkConfig"/> with BatchSize = 2000, PreserveInsertOrder = true,
    /// and all other options disabled.
    /// </value>
    public static BulkConfig Default { get; } = new();

    /// <summary>
    /// Gets the number of entities to process in each batch.
    /// </summary>
    /// <value>The batch size. Default is 2000.</value>
    /// <remarks>
    /// <para>
    /// Larger batch sizes improve throughput but require more memory.
    /// Consider reducing this value for entities with large columns (LOBs).
    /// </para>
    /// <para>
    /// For SqlBulkCopy operations, this maps to the BatchSize property.
    /// For MongoDB, entities are chunked into groups of this size.
    /// </para>
    /// </remarks>
    public int BatchSize { get; init; } = 2000;

    /// <summary>
    /// Gets the timeout in seconds for bulk copy operations.
    /// </summary>
    /// <value>
    /// The timeout in seconds, or <c>null</c> to use the provider's default timeout.
    /// </value>
    /// <remarks>
    /// For SqlBulkCopy, this maps to the BulkCopyTimeout property.
    /// A value of 0 indicates no timeout.
    /// </remarks>
    public int? BulkCopyTimeout { get; init; }

    /// <summary>
    /// Gets a value indicating whether database-generated identity values should be
    /// retrieved and set back on the entities after insert.
    /// </summary>
    /// <value>
    /// <c>true</c> to retrieve and set identity values; <c>false</c> otherwise.
    /// Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, the bulk insert operation will read back generated IDs
    /// (identity columns, sequences) and populate them on the source entities.
    /// This has a performance cost as it requires additional queries.
    /// </para>
    /// <para>
    /// Only applies to <see cref="IBulkOperations{TEntity}.BulkInsertAsync"/> and
    /// <see cref="IBulkOperations{TEntity}.BulkMergeAsync"/> operations.
    /// </para>
    /// </remarks>
    public bool SetOutputIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the insert order of entities should be preserved.
    /// </summary>
    /// <value>
    /// <c>true</c> to maintain the original order; <c>false</c> to allow reordering
    /// for potential performance gains. Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Preserving order is important when entity relationships depend on insertion sequence
    /// or when <see cref="SetOutputIdentity"/> is enabled and you need to correlate
    /// returned IDs with input entities.
    /// </para>
    /// <para>
    /// For MongoDB, this maps to InsertManyOptions.IsOrdered.
    /// </para>
    /// </remarks>
    public bool PreserveInsertOrder { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to use a temporary database table
    /// for staging bulk operations.
    /// </summary>
    /// <value>
    /// <c>true</c> to use tempdb for staging; <c>false</c> otherwise.
    /// Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled, bulk operations create a temporary staging table in tempdb
    /// before merging with the target table. This can improve performance for
    /// large datasets by reducing lock contention on the target table.
    /// </para>
    /// <para>
    /// This option is primarily used with SQL Server bulk operations.
    /// </para>
    /// </remarks>
    public bool UseTempDB { get; init; }

    /// <summary>
    /// Gets a value indicating whether entities should be tracked by the
    /// ORM context after bulk operations.
    /// </summary>
    /// <value>
    /// <c>true</c> to attach entities to the context after bulk operations;
    /// <c>false</c> otherwise. Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When enabled with Entity Framework Core, entities are added to the
    /// change tracker after bulk operations. This allows subsequent operations
    /// on the same entities without re-querying.
    /// </para>
    /// <para>
    /// Enabling tracking has memory overhead and is typically unnecessary
    /// for insert-and-forget scenarios.
    /// </para>
    /// </remarks>
    public bool TrackingEntities { get; init; }

    /// <summary>
    /// Gets the list of property names to include in the bulk operation.
    /// </summary>
    /// <value>
    /// A read-only list of property names to include, or <c>null</c> to include all properties.
    /// </value>
    /// <remarks>
    /// <para>
    /// When specified, only the listed properties are included in the operation.
    /// This is mutually exclusive with <see cref="PropertiesToExclude"/>.
    /// If both are specified, <see cref="PropertiesToInclude"/> takes precedence.
    /// </para>
    /// <para>
    /// Useful for partial updates where only specific columns should change.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Only update Status and ModifiedAt columns
    /// var config = BulkConfig.Default with
    /// {
    ///     PropertiesToInclude = ["Status", "ModifiedAt"]
    /// };
    /// </code>
    /// </example>
    public IReadOnlyList<string>? PropertiesToInclude { get; init; }

    /// <summary>
    /// Gets the list of property names to exclude from the bulk operation.
    /// </summary>
    /// <value>
    /// A read-only list of property names to exclude, or <c>null</c> to exclude no properties.
    /// </value>
    /// <remarks>
    /// <para>
    /// When specified, the listed properties are excluded from the operation.
    /// This is useful when you want to update most properties except a few
    /// (like CreatedAt or audit columns).
    /// </para>
    /// <para>
    /// Ignored if <see cref="PropertiesToInclude"/> is specified.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Update all columns except audit fields
    /// var config = BulkConfig.Default with
    /// {
    ///     PropertiesToExclude = ["CreatedAt", "CreatedBy"]
    /// };
    /// </code>
    /// </example>
    public IReadOnlyList<string>? PropertiesToExclude { get; init; }
}
