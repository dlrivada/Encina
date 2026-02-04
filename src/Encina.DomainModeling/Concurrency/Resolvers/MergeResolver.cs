using LanguageExt;

namespace Encina.DomainModeling.Concurrency.Resolvers;

/// <summary>
/// Abstract base class for implementing custom merge logic to resolve concurrency conflicts.
/// </summary>
/// <typeparam name="TEntity">The type of the entity involved in the conflict.</typeparam>
/// <remarks>
/// <para>
/// Extend this class to implement domain-specific merge strategies that can intelligently
/// combine changes from multiple sources. This is the most flexible but also most complex
/// conflict resolution approach.
/// </para>
/// <para>
/// <b>When to use:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Different properties can be merged independently</description></item>
///   <item><description>Business rules can determine which changes take precedence</description></item>
///   <item><description>Some conflicts are resolvable while others require manual intervention</description></item>
/// </list>
/// <para>
/// <b>Implementation guidelines:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Compare <c>current</c> with <c>database</c> to identify what other processes changed.
///     </description>
///   </item>
///   <item>
///     <description>
///       Compare <c>current</c> with <c>proposed</c> to identify what this operation changed.
///     </description>
///   </item>
///   <item>
///     <description>
///       If the same property was changed by both, decide how to merge or return an error.
///     </description>
///   </item>
///   <item>
///     <description>
///       Always update the version number on the merged entity.
///     </description>
///   </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class OrderMergeResolver : MergeResolver&lt;Order&gt;
/// {
///     protected override Task&lt;Either&lt;EncinaError, Order&gt;&gt; MergeAsync(
///         Order current,
///         Order proposed,
///         Order database,
///         CancellationToken ct)
///     {
///         // Check for conflicting changes to Status
///         var dbChangedStatus = current.Status != database.Status;
///         var weChangedStatus = current.Status != proposed.Status;
///
///         if (dbChangedStatus &amp;&amp; weChangedStatus)
///         {
///             // Both changed Status - cannot merge automatically
///             var conflictInfo = new ConcurrencyConflictInfo&lt;Order&gt;(current, proposed, database);
///             return Task.FromResult(
///                 Either&lt;EncinaError, Order&gt;.Left(
///                     RepositoryErrors.ConcurrencyConflict(conflictInfo)));
///         }
///
///         // Merge: take our Status change, keep their other changes
///         var merged = database with
///         {
///             Status = weChangedStatus ? proposed.Status : database.Status,
///             Notes = proposed.Notes, // We always take our notes
///             Version = database.Version + 1
///         };
///
///         return Task.FromResult(Either&lt;EncinaError, Order&gt;.Right(merged));
///     }
/// }
/// </code>
/// </example>
public abstract class MergeResolver<TEntity> : IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    private const string MergeNotImplementedCode = "Repository.MergeNotImplemented";

    /// <inheritdoc />
    public Task<Either<EncinaError, TEntity>> ResolveAsync(
        TEntity current,
        TEntity proposed,
        TEntity database,
        CancellationToken cancellationToken = default)
    {
        return MergeAsync(current, proposed, database, cancellationToken);
    }

    /// <summary>
    /// Implements the merge logic to combine changes from multiple sources.
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
    /// <c>Right</c> with the merged entity if changes can be combined successfully,
    /// or <c>Left</c> with an error if the conflict cannot be resolved automatically.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default implementation returns an error indicating that merge is not implemented.
    /// Subclasses must override this method to provide actual merge logic.
    /// </para>
    /// </remarks>
    protected virtual Task<Either<EncinaError, TEntity>> MergeAsync(
        TEntity current,
        TEntity proposed,
        TEntity database,
        CancellationToken cancellationToken)
    {
        var entityTypeName = typeof(TEntity).Name;
        var error = EncinaErrors.Create(
            MergeNotImplementedCode,
            $"Merge logic for entity type '{entityTypeName}' is not implemented. " +
            $"Override the MergeAsync method in your {GetType().Name} class to provide custom merge logic.");

        return Task.FromResult(Either<EncinaError, TEntity>.Left(error));
    }
}
