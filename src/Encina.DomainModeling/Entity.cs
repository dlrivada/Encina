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
/// <para>
/// Entities can raise domain events to communicate important state changes.
/// These events are collected and dispatched after successful persistence.
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
///         AddDomainEvent(new OrderCreated(id, customerName));
///     }
/// }
/// </code>
/// </example>
[SuppressMessage("SonarAnalyzer.CSharp", "S4035:Seal class or implement IEqualityComparer",
    Justification = "DDD base class: Entity equality is by ID, derived types inherit this semantic")]
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; protected init; }

    /// <summary>
    /// Gets the domain events that have been raised but not yet dispatched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Domain events should be dispatched after successful persistence to ensure
    /// consistency. The persistence layer (e.g., EF Core SaveChanges interceptor)
    /// should collect and dispatch these events after the transaction commits.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Adds a domain event to be raised after the entity is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    /// <remarks>
    /// <para>
    /// Domain events should be dispatched after successful persistence to ensure
    /// consistency. The persistence layer (e.g., EF Core SaveChanges interceptor)
    /// should collect and dispatch these events after the transaction commits.
    /// </para>
    /// </remarks>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a specific domain event from the pending events.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove.</param>
    /// <returns><c>true</c> if the event was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method should be called by the persistence layer after successfully
    /// dispatching all domain events.
    /// </para>
    /// </remarks>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
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
