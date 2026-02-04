using LanguageExt;

namespace Encina.DomainModeling.Concurrency.Resolvers;

/// <summary>
/// A conflict resolver that always uses the database entity, implementing a "first write wins" strategy.
/// </summary>
/// <typeparam name="TEntity">The type of the entity involved in the conflict.</typeparam>
/// <remarks>
/// <para>
/// This resolver returns the current database entity, effectively discarding the changes
/// from the current operation. The first process to successfully write wins, and subsequent
/// conflicting updates are ignored.
/// </para>
/// <para>
/// <b>Use cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Idempotent operations where re-applying the same change is harmless</description></item>
///   <item><description>Scenarios where preserving the first committed value is preferred</description></item>
///   <item><description>Batch processing where conflicts should be silently ignored</description></item>
/// </list>
/// <para>
/// <b>Caution:</b> This strategy silently discards the current operation's changes.
/// Callers should check if the returned entity differs from their proposed entity
/// to detect when their changes were not applied.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register as the default resolver for AuditLog entities (immutable records)
/// services.AddSingleton&lt;IConcurrencyConflictResolver&lt;AuditLog&gt;, FirstWriteWinsResolver&lt;AuditLog&gt;&gt;();
///
/// // Or use directly
/// var resolver = new FirstWriteWinsResolver&lt;AuditLog&gt;();
/// var result = await resolver.ResolveAsync(original, proposed, database, ct);
/// </code>
/// </example>
public sealed class FirstWriteWinsResolver<TEntity> : IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Returns the database entity unchanged. The version is not modified because
    /// no actual update will be performed - the existing database state is preserved.
    /// </para>
    /// <para>
    /// The caller can compare the result with their proposed entity to determine
    /// if their changes were applied or discarded.
    /// </para>
    /// </remarks>
    public Task<Either<EncinaError, TEntity>> ResolveAsync(
        TEntity current,
        TEntity proposed,
        TEntity database,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Either<EncinaError, TEntity>.Right(database));
    }
}
