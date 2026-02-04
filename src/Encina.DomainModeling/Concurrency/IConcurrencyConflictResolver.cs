using LanguageExt;

namespace Encina.DomainModeling.Concurrency;

/// <summary>
/// Defines a strategy for resolving optimistic concurrency conflicts.
/// </summary>
/// <typeparam name="TEntity">The type of the entity involved in the conflict.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to define custom conflict resolution logic for specific entity types.
/// The resolver receives all three entity states and must decide how to merge or select between them.
/// </para>
/// <para>
/// Encina provides three built-in resolvers:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="Resolvers.LastWriteWinsResolver{TEntity}"/>: Always uses the proposed entity (current operation wins).
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Resolvers.FirstWriteWinsResolver{TEntity}"/>: Always uses the database entity (previous operation wins).
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Resolvers.MergeResolver{TEntity}"/>: Abstract base class for implementing custom merge logic.
///     </description>
///   </item>
/// </list>
/// <para>
/// The resolver returns <c>Either&lt;EncinaError, TEntity&gt;</c> following Encina's ROP pattern.
/// Return <c>Left</c> with an error if the conflict cannot be resolved (e.g., conflicting changes
/// to the same property that cannot be merged). Return <c>Right</c> with the resolved entity to proceed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom resolver that merges non-conflicting field changes
/// public class OrderMergeResolver : MergeResolver&lt;Order&gt;
/// {
///     protected override Task&lt;Either&lt;EncinaError, Order&gt;&gt; MergeAsync(
///         Order current,
///         Order proposed,
///         Order database,
///         CancellationToken ct)
///     {
///         // Check for irreconcilable conflicts
///         if (current.Status != database.Status &amp;&amp; proposed.Status != current.Status)
///         {
///             // Both operations changed Status - cannot auto-merge
///             return Task.FromResult(
///                 Either&lt;EncinaError, Order&gt;.Left(
///                     RepositoryErrors.ConcurrencyConflict&lt;Order&gt;()));
///         }
///
///         // Merge non-conflicting changes
///         var merged = database with
///         {
///             Status = proposed.Status,
///             Version = database.Version + 1
///         };
///
///         return Task.FromResult(Either&lt;EncinaError, Order&gt;.Right(merged));
///     }
/// }
/// </code>
/// </example>
public interface IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Attempts to resolve a concurrency conflict between entity states.
    /// </summary>
    /// <param name="current">
    /// The entity state when it was originally loaded (before modifications).
    /// </param>
    /// <param name="proposed">
    /// The entity state that the current operation is trying to save.
    /// </param>
    /// <param name="database">
    /// The current state in the database at the time the conflict was detected.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right</c> with the resolved entity if the conflict was successfully resolved,
    /// or <c>Left</c> with an error if the conflict cannot be resolved automatically.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The resolved entity should have an updated version number:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       For <see cref="IVersionedEntity"/> entities: Set <c>Version</c> to <c>database.Version + 1</c>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       For <see cref="IConcurrencyAwareEntity"/> entities: Leave <c>RowVersion</c> unchanged;
    ///       the database will generate a new value.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    Task<Either<EncinaError, TEntity>> ResolveAsync(
        TEntity current,
        TEntity proposed,
        TEntity database,
        CancellationToken cancellationToken = default);
}
