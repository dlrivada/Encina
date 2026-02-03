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
    /// Copies all domain events from another aggregate root to this instance.
    /// </summary>
    /// <param name="source">The source aggregate root to copy events from.</param>
    /// <remarks>
    /// <para>
    /// This method is used to preserve domain events when working with immutable entities
    /// (C# records). When updating an immutable entity using a with-expression, the new
    /// instance won't have the domain events from the original. This method transfers
    /// those events to the new instance.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public void CopyEventsFrom(IAggregateRoot source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var domainEvent in source.DomainEvents)
        {
            AddDomainEvent(domainEvent);
        }
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

/// <summary>
/// Base class for entities with automatic audit tracking via EF Core interceptors.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="Entity{TId}"/> and implements <see cref="IAuditableEntity"/>
/// with public setters, allowing the <c>AuditInterceptor</c> to automatically populate
/// audit fields when entities are added or modified.
/// </para>
/// <para>
/// <b>AuditedEntity vs Entity with IAuditable:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="AuditedEntity{TId}"/>: Uses <see cref="IAuditableEntity"/> with public setters.
///       Audit fields are automatically populated by interceptors. Best for typical CRUD scenarios.
///     </description>
///   </item>
///   <item>
///     <description>
///       Entity implementing <see cref="IAuditable"/>: Uses getter-only properties.
///       Audit fields are set via explicit domain methods. Best for immutable domain patterns.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderLine : AuditedEntity&lt;Guid&gt;
/// {
///     public Guid OrderId { get; private set; }
///     public string ProductName { get; private set; }
///     public decimal UnitPrice { get; private set; }
///     public int Quantity { get; private set; }
///
///     private OrderLine() : base(Guid.Empty) { } // For ORM
///
///     public OrderLine(Guid id, Guid orderId, string productName, decimal unitPrice, int quantity)
///         : base(id)
///     {
///         OrderId = orderId;
///         ProductName = productName;
///         UnitPrice = unitPrice;
///         Quantity = quantity;
///     }
/// }
/// </code>
/// </example>
[SuppressMessage("SonarAnalyzer.CSharp", "S4035:Seal class or implement IEqualityComparer",
    Justification = "DDD base class: Entity equality is by ID, derived types inherit this semantic")]
public abstract class AuditedEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    /// <summary>
    /// Gets the time provider used for setting audit timestamps in tests.
    /// </summary>
    /// <remarks>
    /// This property is primarily used for unit testing to inject a controlled time source.
    /// In production, the <c>AuditInterceptor</c> sets the timestamps directly.
    /// </remarks>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTime? ModifiedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditedEntity{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testing. In production, the <c>AuditInterceptor</c>
    /// sets the timestamps, so this parameter is typically only used in tests.
    /// </param>
    protected AuditedEntity(TId id, TimeProvider? timeProvider = null) : base(id)
    {
        TimeProvider = timeProvider ?? TimeProvider.System;
    }
}

/// <summary>
/// Base class for soft-deletable entities with method-based (private setter) population.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="AuditedEntity{TId}"/> and implements <see cref="ISoftDeletable"/>
/// with private setters. Soft delete fields are set via explicit domain methods (<see cref="Delete"/>
/// and <see cref="Restore"/>).
/// </para>
/// <para>
/// <b>SoftDeletableEntity vs FullyAuditedEntity:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="SoftDeletableEntity{TId}"/>: Uses <see cref="ISoftDeletable"/> with <b>private setters</b>.
///       Soft delete fields are set via explicit domain methods. Best for immutable domain patterns.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="FullyAuditedEntity{TId}"/>: Uses <see cref="ISoftDeletableEntity"/> with <b>public setters</b>.
///       Best for interceptor-based automatic population.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderLine : SoftDeletableEntity&lt;Guid&gt;
/// {
///     public Guid OrderId { get; private set; }
///     public string ProductName { get; private set; }
///
///     private OrderLine() : base(Guid.Empty) { } // For ORM
///
///     public OrderLine(Guid id, Guid orderId, string productName) : base(id)
///     {
///         OrderId = orderId;
///         ProductName = productName;
///     }
///
///     public void Remove(string? deletedBy = null)
///     {
///         Delete(deletedBy);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="FullyAuditedEntity{TId}"/>
/// <seealso cref="SoftDeletableAggregateRoot{TId}"/>
[SuppressMessage("SonarAnalyzer.CSharp", "S4035:Seal class or implement IEqualityComparer",
    Justification = "DDD base class: Entity equality is by ID, derived types inherit this semantic")]
public abstract class SoftDeletableEntity<TId> : AuditedEntity<TId>, ISoftDeletable
    where TId : notnull
{
    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTime? DeletedAtUtc { get; private set; }

    /// <inheritdoc />
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeletableEntity{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testing. In production, the <c>AuditInterceptor</c>
    /// sets the timestamps, so this parameter is typically only used in tests.
    /// </param>
    protected SoftDeletableEntity(TId id, TimeProvider? timeProvider = null) : base(id, timeProvider)
    {
    }

    /// <summary>
    /// Marks this entity as deleted (soft delete).
    /// </summary>
    /// <param name="deletedBy">The identifier of the user who deleted this entity.</param>
    /// <remarks>
    /// <para>
    /// This method sets <see cref="IsDeleted"/> to <c>true</c>, <see cref="DeletedAtUtc"/> to the
    /// current UTC time, and <see cref="DeletedBy"/> to the provided user identifier.
    /// </para>
    /// <para>
    /// Query filters should be configured in EF Core to automatically exclude soft-deleted entities
    /// from normal queries. Use <c>.IgnoreQueryFilters()</c> when you need to access deleted entities.
    /// </para>
    /// </remarks>
    public virtual void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = TimeProvider.GetUtcNow().UtcDateTime;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    /// <remarks>
    /// This method sets <see cref="IsDeleted"/> to <c>false</c> and clears <see cref="DeletedAtUtc"/>
    /// and <see cref="DeletedBy"/> properties.
    /// </remarks>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}

/// <summary>
/// Base class for entities with automatic audit tracking and soft delete support via EF Core interceptors.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="AuditedEntity{TId}"/> and implements <see cref="ISoftDeletableEntity"/>,
/// combining automatic audit population with soft delete capabilities. Use this when you need:
/// <list type="bullet">
///   <item><description>Automatic creation and modification tracking via interceptors</description></item>
///   <item><description>Soft delete support with deletion tracking</description></item>
/// </list>
/// </para>
/// <para>
/// Soft delete properties (<see cref="IsDeleted"/>, <see cref="DeletedAtUtc"/>, <see cref="DeletedBy"/>)
/// have public setters for interceptor compatibility, but prefer using the <see cref="Delete"/> and
/// <see cref="Restore"/> methods for domain logic as they maintain consistency.
/// </para>
/// <para>
/// <b>FullyAuditedEntity vs SoftDeletableEntity:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="FullyAuditedEntity{TId}"/>: Uses <see cref="ISoftDeletableEntity"/> with <b>public setters</b>.
///       Best for interceptor-based automatic population.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="SoftDeletableEntity{TId}"/>: Uses <see cref="ISoftDeletable"/> with <b>private setters</b>.
///       Best for method-based population in immutable domain patterns.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderLine : FullyAuditedEntity&lt;Guid&gt;
/// {
///     public Guid OrderId { get; private set; }
///     public string ProductName { get; private set; }
///
///     private OrderLine() : base(Guid.Empty) { } // For ORM
///
///     public OrderLine(Guid id, Guid orderId, string productName) : base(id)
///     {
///         OrderId = orderId;
///         ProductName = productName;
///     }
/// }
///
/// // Configuration - soft delete fields are automatically populated
/// services.AddEncinaEntityFrameworkCore(config =>
/// {
///     config.UseSoftDelete = true;
/// });
/// </code>
/// </example>
/// <seealso cref="SoftDeletableEntity{TId}"/>
/// <seealso cref="FullyAuditedAggregateRoot{TId}"/>
[SuppressMessage("SonarAnalyzer.CSharp", "S4035:Seal class or implement IEqualityComparer",
    Justification = "DDD base class: Entity equality is by ID, derived types inherit this semantic")]
public abstract class FullyAuditedEntity<TId> : AuditedEntity<TId>, ISoftDeletableEntity
    where TId : notnull
{
    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAtUtc { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FullyAuditedEntity{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testing. In production, the <c>AuditInterceptor</c>
    /// sets the timestamps, so this parameter is typically only used in tests.
    /// </param>
    protected FullyAuditedEntity(TId id, TimeProvider? timeProvider = null) : base(id, timeProvider)
    {
    }

    /// <summary>
    /// Marks this entity as deleted (soft delete).
    /// </summary>
    /// <param name="deletedBy">The identifier of the user who deleted this entity.</param>
    /// <remarks>
    /// <para>
    /// This method sets <see cref="IsDeleted"/> to <c>true</c>, <see cref="DeletedAtUtc"/> to the
    /// current UTC time, and <see cref="DeletedBy"/> to the provided user identifier.
    /// </para>
    /// <para>
    /// Query filters should be configured in EF Core to automatically exclude soft-deleted entities
    /// from normal queries. Use <c>.IgnoreQueryFilters()</c> when you need to access deleted entities.
    /// </para>
    /// </remarks>
    public virtual void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = TimeProvider.GetUtcNow().UtcDateTime;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    /// <remarks>
    /// This method sets <see cref="IsDeleted"/> to <c>false</c> and clears <see cref="DeletedAtUtc"/>
    /// and <see cref="DeletedBy"/> properties.
    /// </remarks>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
