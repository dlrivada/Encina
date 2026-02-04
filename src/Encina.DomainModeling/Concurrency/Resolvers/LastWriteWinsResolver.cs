using LanguageExt;

namespace Encina.DomainModeling.Concurrency.Resolvers;

/// <summary>
/// A conflict resolver that always uses the proposed entity, implementing a "last write wins" strategy.
/// </summary>
/// <typeparam name="TEntity">The type of the entity involved in the conflict.</typeparam>
/// <remarks>
/// <para>
/// This resolver simply returns the proposed entity with an updated version number,
/// effectively discarding any changes made by other processes since the entity was loaded.
/// </para>
/// <para>
/// <b>Use cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Entities where the latest value is always preferred</description></item>
///   <item><description>Configuration or settings that should be overwritten entirely</description></item>
///   <item><description>Scenarios where conflicts are rare and data loss is acceptable</description></item>
/// </list>
/// <para>
/// <b>Caution:</b> This strategy can result in lost updates. If two users edit the same entity
/// simultaneously, the first user's changes will be overwritten without notification.
/// Consider using <see cref="FirstWriteWinsResolver{TEntity}"/> or a custom <see cref="MergeResolver{TEntity}"/>
/// for scenarios where data integrity is critical.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register as the default resolver for Product entities
/// services.AddSingleton&lt;IConcurrencyConflictResolver&lt;Product&gt;, LastWriteWinsResolver&lt;Product&gt;&gt;();
///
/// // Or use directly
/// var resolver = new LastWriteWinsResolver&lt;Product&gt;();
/// var result = await resolver.ResolveAsync(original, proposed, database, ct);
/// </code>
/// </example>
public sealed class LastWriteWinsResolver<TEntity> : IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Returns the proposed entity with the version incremented based on the database entity's current version.
    /// </para>
    /// <para>
    /// For <see cref="IVersionedEntity"/> entities, the version is set to <c>database.Version + 1</c>.
    /// For <see cref="IConcurrencyAwareEntity"/> entities, the row version is left unchanged as the
    /// database will generate a new value on save.
    /// </para>
    /// </remarks>
    public Task<Either<EncinaError, TEntity>> ResolveAsync(
        TEntity current,
        TEntity proposed,
        TEntity database,
        CancellationToken cancellationToken = default)
    {
        var resolved = proposed;

        // Use IVersionedEntity (mutable) for setting the version
        if (proposed is IVersionedEntity versionedProposed && database is IVersioned versionedDatabase)
        {
            versionedProposed.Version = (int)(versionedDatabase.Version + 1);
        }

        return Task.FromResult(Either<EncinaError, TEntity>.Right(resolved));
    }
}
