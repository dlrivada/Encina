using System.Diagnostics.CodeAnalysis;
using LanguageExt;

namespace Encina.Cdc.Abstractions;

/// <summary>
/// Handles typed change events for a specific entity type.
/// Implementations react to database changes captured by a CDC connector.
/// </summary>
/// <typeparam name="TEntity">The entity type this handler processes.</typeparam>
/// <remarks>
/// <para>
/// All handler methods return <see cref="Either{EncinaError, Unit}"/> following
/// the Railway Oriented Programming pattern. Return <c>Right(unit)</c> for success
/// or <c>Left(error)</c> for failures.
/// </para>
/// <para>
/// The CDC dispatcher resolves handlers from the DI container and routes
/// change events based on table-to-entity type mappings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderChangeHandler : IChangeEventHandler&lt;Order&gt;
/// {
///     public ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleInsertAsync(
///         Order entity, ChangeContext ctx)
///     {
///         // React to new order creation
///         return new(Right(unit));
///     }
///
///     public ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleUpdateAsync(
///         Order before, Order after, ChangeContext ctx)
///     {
///         // React to order modification
///         return new(Right(unit));
///     }
///
///     public ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleDeleteAsync(
///         Order entity, ChangeContext ctx)
///     {
///         // React to order deletion
///         return new(Right(unit));
///     }
/// }
/// </code>
/// </example>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "IChangeEventHandler processes CDC change events, not .NET event delegates")]
public interface IChangeEventHandler<in TEntity>
{
    /// <summary>
    /// Handles an insert change event for the specified entity.
    /// </summary>
    /// <param name="entity">The newly inserted entity.</param>
    /// <param name="context">The change context containing metadata and cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(TEntity entity, ChangeContext context);

    /// <summary>
    /// Handles an update change event for the specified entity.
    /// </summary>
    /// <param name="before">The state of the entity before the update.</param>
    /// <param name="after">The state of the entity after the update.</param>
    /// <param name="context">The change context containing metadata and cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(TEntity before, TEntity after, ChangeContext context);

    /// <summary>
    /// Handles a delete change event for the specified entity.
    /// </summary>
    /// <param name="entity">The entity that was deleted.</param>
    /// <param name="context">The change context containing metadata and cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(TEntity entity, ChangeContext context);
}
