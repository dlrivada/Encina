using Encina;
using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Interface for high-performance bulk database operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// Bulk operations provide significant performance improvements over standard ORM operations
/// when working with large datasets. While standard SaveChanges() operations process entities
/// one at a time, bulk operations leverage database-specific features like SqlBulkCopy,
/// COPY command, or BulkWrite to achieve up to 459x faster performance.
/// </para>
/// <para>
/// <b>Performance Comparison (1,000 entities, measured with Testcontainers):</b>
/// <list type="bullet">
/// <item><description>Dapper + SQL Server: Insert 30x, Update 125x, Delete 370x faster</description></item>
/// <item><description>EF Core + SQL Server: Insert 112x, Update 178x, Delete 200x faster</description></item>
/// <item><description>ADO.NET + SQL Server: Insert 104x, Update 187x, Delete 459x faster</description></item>
/// <item><description>MongoDB: Insert 130x, Update 16x, Delete 21x faster</description></item>
/// </list>
/// </para>
/// <para>
/// This interface is accessed through <c>IUnitOfWork.BulkOperations&lt;TEntity&gt;()</c> or
/// via dependency injection. It is intentionally separate from <see cref="IFunctionalRepository{TEntity, TId}"/>
/// to maintain repository simplicity while providing opt-in bulk capabilities.
/// </para>
/// <para>
/// All methods return <see cref="Either{EncinaError, T}"/> following the Railway Oriented Programming
/// pattern for explicit error handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Bulk insert 10,000 orders with custom batch size
/// var orders = GenerateOrders(10_000);
/// var config = BulkConfig.Default with { BatchSize = 5000 };
/// var result = await bulkOps.BulkInsertAsync(orders, config, ct);
///
/// result.Match(
///     Right: count => Console.WriteLine($"Inserted {count} orders"),
///     Left: error => Console.WriteLine($"Error: {error.Message}")
/// );
/// </code>
/// </example>
public interface IBulkOperations<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Inserts multiple entities into the database using bulk copy operations.
    /// </summary>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="config">Optional bulk configuration settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the number of inserted entities; Left with <c>RepositoryErrors.BulkInsertFailed</c>
    /// if the operation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses database-specific bulk insert mechanisms:
    /// <list type="bullet">
    /// <item><description>SQL Server: SqlBulkCopy</description></item>
    /// <item><description>PostgreSQL: COPY command</description></item>
    /// <item><description>MySQL: LOAD DATA or multi-row INSERT</description></item>
    /// <item><description>MongoDB: InsertMany</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When <see cref="BulkConfig.SetOutputIdentity"/> is true, database-generated IDs
    /// are populated back into the entities after insertion.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, int>> BulkInsertAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple existing entities in the database using bulk operations.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="config">Optional bulk configuration settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the number of updated entities; Left with <c>RepositoryErrors.BulkUpdateFailed</c>
    /// if the operation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses database-specific bulk update mechanisms:
    /// <list type="bullet">
    /// <item><description>SQL Server: MERGE statement with Table-Valued Parameters</description></item>
    /// <item><description>MongoDB: BulkWrite with ReplaceOneModel</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Use <see cref="BulkConfig.PropertiesToInclude"/> or <see cref="BulkConfig.PropertiesToExclude"/>
    /// to control which properties are updated.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, int>> BulkUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities from the database using bulk operations.
    /// </summary>
    /// <param name="entities">The entities to delete (matched by primary key).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the number of deleted entities; Left with <c>RepositoryErrors.BulkDeleteFailed</c>
    /// if the operation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Entities are matched for deletion by their primary key values.
    /// Uses database-specific bulk delete mechanisms:
    /// <list type="bullet">
    /// <item><description>SQL Server: DELETE with Table-Valued Parameters</description></item>
    /// <item><description>MongoDB: BulkWrite with DeleteOneModel</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, int>> BulkDeleteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an upsert operation (insert if not exists, update if exists) on multiple entities.
    /// </summary>
    /// <param name="entities">The entities to merge.</param>
    /// <param name="config">Optional bulk configuration settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the number of affected entities (inserted + updated);
    /// Left with <c>RepositoryErrors.BulkMergeFailed</c> if the operation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses database-specific upsert mechanisms:
    /// <list type="bullet">
    /// <item><description>SQL Server: MERGE statement</description></item>
    /// <item><description>PostgreSQL: INSERT ... ON CONFLICT</description></item>
    /// <item><description>MySQL: INSERT ... ON DUPLICATE KEY UPDATE</description></item>
    /// <item><description>MongoDB: BulkWrite with ReplaceOneModel (IsUpsert = true)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, int>> BulkMergeAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads multiple entities by their IDs using optimized bulk read operations.
    /// </summary>
    /// <param name="ids">The IDs of the entities to read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with the list of found entities; Left with <c>RepositoryErrors.BulkReadFailed</c>
    /// if the operation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses optimized batch query mechanisms:
    /// <list type="bullet">
    /// <item><description>SQL Server: SELECT with Table-Valued Parameters or IN clause</description></item>
    /// <item><description>MongoDB: Filter.In() with _id field</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Returns only found entities. The count of returned entities may be less than
    /// the count of requested IDs if some entities don't exist.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, IReadOnlyList<TEntity>>> BulkReadAsync(
        IEnumerable<object> ids,
        CancellationToken cancellationToken = default);
}
