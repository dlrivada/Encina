using System.Diagnostics.CodeAnalysis;

namespace Encina.DomainModeling;

/// <summary>
/// Base class for entities in Domain-Driven Design.
/// Entities have identity that defines their equality - two entities with the same ID are considered equal.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
/// <remarks>
/// <para>
/// This is a provider-agnostic base class that works with EF Core, Dapper, ADO.NET, or any other data access technology.
/// For event-sourced aggregates, use <c>Encina.Marten.AggregateBase</c> instead.
/// </para>
/// <para>
/// Entities are compared by their identity (Id), not by their attributes.
/// Two entities are equal if and only if they have the same Id.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : Entity&lt;OrderId&gt;
/// {
///     public string CustomerName { get; private set; }
///     public decimal Total { get; private set; }
///
///     // Private constructor for ORM
///     private Order() : base(default!) { }
///
///     public Order(OrderId id, string customerName) : base(id)
///     {
///         CustomerName = customerName;
///         Total = 0;
///     }
/// }
/// </code>
/// </example>
[SuppressMessage("SonarAnalyzer.CSharp", "S4035:Seal class or implement IEqualityComparer",
    Justification = "DDD base class: Entity equality is by ID, derived types inherit this semantic")]
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; protected init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Determines whether this entity is equal to another entity.
    /// Two entities are equal if they have the same type and the same Id.
    /// </summary>
    /// <param name="other">The entity to compare with.</param>
    /// <returns><c>true</c> if the entities are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Entities of different types are never equal
        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns><c>true</c> if the entities are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns><c>true</c> if the entities are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{GetType().Name} [Id={Id}]";
    }
}
