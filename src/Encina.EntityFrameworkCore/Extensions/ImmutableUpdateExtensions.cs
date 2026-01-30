using Encina.DomainModeling;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for handling immutable entity updates with EF Core.
/// </summary>
/// <remarks>
/// <para>
/// These utilities solve the challenge of updating immutable entities (C# records)
/// with EF Core while preserving domain events. When you use a with-expression to
/// create a modified copy of an entity, the new instance doesn't have the domain
/// events from the original.
/// </para>
/// <para>
/// <b>The Problem</b>: EF Core's change tracker tracks the original entity instance.
/// When using immutable records, you create a new instance with modifications using
/// with-expressions. The new instance needs to replace the original in the change
/// tracker, but domain events are lost in the process.
/// </para>
/// <para>
/// <b>The Solution</b>: These extension methods handle the detach/attach pattern
/// and preserve domain events through the <see cref="IAggregateRoot.CopyEventsFrom"/>
/// method.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define an immutable aggregate root record
/// public record Order : AggregateRoot&lt;OrderId&gt;
/// {
///     public required string CustomerName { get; init; }
///     public required OrderStatus Status { get; init; }
///
///     public Order Ship()
///     {
///         AddDomainEvent(new OrderShippedEvent(Id));
///         return this with { Status = OrderStatus.Shipped };
///     }
/// }
///
/// // Update using with-expression and preserve events
/// var order = await context.Orders.FindAsync(orderId);
/// var shippedOrder = order.Ship().WithPreservedEvents(order);
///
/// // Update the change tracker
/// var result = context.UpdateImmutable(shippedOrder);
/// if (result.IsRight)
/// {
///     await context.SaveChangesAsync();
/// }
/// </code>
/// </example>
public static class ImmutableUpdateExtensions
{
    /// <summary>
    /// Updates an immutable entity in the change tracker, replacing the original tracked entity
    /// with the modified version while preserving domain events.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="context">The DbContext containing the entity.</param>
    /// <param name="modified">The modified entity instance to track.</param>
    /// <returns>
    /// <see cref="Either{EncinaError, Unit}"/> where Right indicates success
    /// and Left contains an error if the operation failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    /// <item>Extracts the primary key from the modified entity</item>
    /// <item>Finds the original tracked entity in the Local cache</item>
    /// <item>If found and both implement <see cref="IAggregateRoot"/>, copies domain events</item>
    /// <item>Detaches the original entity</item>
    /// <item>Attaches the modified entity and marks it as Modified</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Error Cases</b>:
    /// <list type="bullet">
    /// <item><see cref="RepositoryErrors.EntityNotTracked{TEntity}()"/> - Original entity not tracked</item>
    /// <item><see cref="RepositoryErrors.UpdateFailed{TEntity}(string, Exception?)"/> - Detach/attach failed</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Load and modify an immutable entity
    /// var order = await context.Orders.FindAsync(orderId);
    /// var updatedOrder = order with { Status = OrderStatus.Shipped };
    ///
    /// // Update with event preservation (if order raised events)
    /// var result = context.UpdateImmutable(updatedOrder);
    ///
    /// result.Match(
    ///     Right: _ => Console.WriteLine("Updated successfully"),
    ///     Left: error => Console.WriteLine($"Failed: {error.Message}"));
    /// </code>
    /// </example>
    public static Either<EncinaError, Unit> UpdateImmutable<TEntity>(
        this DbContext context,
        TEntity modified)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modified);

        try
        {
            // Get the primary key value from the modified entity
            var keyValue = context.GetPrimaryKeyValue(modified);

            // Find the original tracked entity in Local cache
            var original = FindTrackedEntity(context, keyValue);

            if (original is null)
            {
                return RepositoryErrors.EntityNotTracked<TEntity>();
            }

            // Preserve domain events if both are aggregate roots
            if (original is IAggregateRoot originalAggregate && modified is IAggregateRoot modifiedAggregate)
            {
                modifiedAggregate.CopyEventsFrom(originalAggregate);
            }

            // Detach the original entity
            context.Entry(original).State = EntityState.Detached;

            // Attach the modified entity and mark as Modified
            context.Attach(modified);
            context.Entry(modified).State = EntityState.Modified;

            return unit;
        }
        catch (Exception ex)
        {
            return RepositoryErrors.UpdateFailed<TEntity>(
                "Failed to update immutable entity in change tracker",
                ex);
        }

        TEntity? FindTrackedEntity(DbContext ctx, object keyValue)
        {
            // Search in the Local cache for an entity with matching key
            foreach (var entry in ctx.ChangeTracker.Entries<TEntity>())
            {
                var entryKeyValue = ctx.GetPrimaryKeyValue(entry.Entity);
                if (entryKeyValue.Equals(keyValue))
                {
                    return entry.Entity;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Copies domain events from the original aggregate root to the new instance.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The aggregate root type.</typeparam>
    /// <param name="newInstance">The new instance created from a with-expression.</param>
    /// <param name="originalInstance">The original instance that contains the domain events.</param>
    /// <returns>The new instance with events copied, for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="newInstance"/> or <paramref name="originalInstance"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is designed for use with C# record with-expressions. When you create
    /// a modified copy of an aggregate root using a with-expression, domain events raised
    /// in methods that return the new instance need to be preserved.
    /// </para>
    /// <para>
    /// <b>Usage Pattern</b>: Call this method immediately after creating the new instance
    /// with a with-expression, before any further operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In the aggregate root
    /// public record Order : AggregateRoot&lt;OrderId&gt;
    /// {
    ///     public Order Ship()
    ///     {
    ///         AddDomainEvent(new OrderShippedEvent(Id));
    ///         return this with { Status = OrderStatus.Shipped };
    ///     }
    /// }
    ///
    /// // In the application service
    /// var order = await repository.GetByIdAsync(orderId);
    ///
    /// // The Ship method raises an event and returns a new instance
    /// // Use WithPreservedEvents to ensure events are on the new instance
    /// var shippedOrder = order.Ship().WithPreservedEvents(order);
    ///
    /// // Now shippedOrder has the OrderShippedEvent
    /// await context.UpdateImmutable(shippedOrder);
    /// await context.SaveChangesAsync();
    /// // Event will be dispatched after save
    /// </code>
    /// </example>
    public static TAggregateRoot WithPreservedEvents<TAggregateRoot>(
        this TAggregateRoot newInstance,
        TAggregateRoot originalInstance)
        where TAggregateRoot : class, IAggregateRoot
    {
        ArgumentNullException.ThrowIfNull(newInstance);
        ArgumentNullException.ThrowIfNull(originalInstance);

        newInstance.CopyEventsFrom(originalInstance);
        return newInstance;
    }

    /// <summary>
    /// Updates an immutable entity asynchronously, with support for async validation or pre-processing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="context">The DbContext containing the entity.</param>
    /// <param name="modified">The modified entity instance to track.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation, containing
    /// <see cref="Either{EncinaError, Unit}"/> where Right indicates success.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This async overload exists for API consistency and future extensibility.
    /// The current implementation delegates to the synchronous version as EF Core's
    /// local change tracker operations are synchronous.
    /// </para>
    /// </remarks>
    public static Task<Either<EncinaError, Unit>> UpdateImmutableAsync<TEntity>(
        this DbContext context,
        TEntity modified,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(context.UpdateImmutable(modified));
    }
}
