using LanguageExt;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Provider-agnostic abstraction for bulk upsert operations on reference table data
/// within a single shard.
/// </summary>
/// <remarks>
/// <para>
/// Each database provider (ADO.NET, Dapper, EF Core, MongoDB) implements this interface
/// with provider-specific upsert semantics:
/// </para>
/// <list type="bullet">
/// <item>SQLite: <c>INSERT OR REPLACE</c></item>
/// <item>SQL Server: <c>MERGE ... WHEN MATCHED THEN UPDATE</c></item>
/// <item>PostgreSQL: <c>INSERT ... ON CONFLICT DO UPDATE</c></item>
/// <item>MySQL: <c>INSERT ... ON DUPLICATE KEY UPDATE</c></item>
/// <item>MongoDB: <c>BulkWriteAsync</c> with <c>ReplaceOneModel</c></item>
/// </list>
/// <para>
/// The store operates on a single shard connection. The <see cref="IReferenceTableReplicator"/>
/// coordinates calls across multiple shards.
/// </para>
/// </remarks>
public interface IReferenceTableStore
{
    /// <summary>
    /// Upserts a collection of entities into the reference table on the current shard.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the reference table.</typeparam>
    /// <param name="entities">The entities to upsert.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the number of rows affected; Left with an error if the operation failed.
    /// </returns>
    Task<Either<EncinaError, int>> UpsertAsync<TEntity>(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Retrieves all entities from the reference table on the current shard.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the reference table.</typeparam>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the collection of entities; Left with an error if the operation failed.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<TEntity>>> GetAllAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Computes a content hash of the reference table data on the current shard,
    /// used for change detection during polling-based refresh.
    /// </summary>
    /// <typeparam name="TEntity">The entity type of the reference table.</typeparam>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the content hash as a hex string; Left with an error if the operation failed.
    /// </returns>
    /// <remarks>
    /// The hash should be deterministic for the same data regardless of row ordering.
    /// Implementations should sort rows by primary key before hashing.
    /// </remarks>
    Task<Either<EncinaError, string>> GetHashAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class;
}
