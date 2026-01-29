namespace Encina.DomainModeling;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design with state-based persistence.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
/// <remarks>
/// <para>
/// This is a provider-agnostic base class that works with EF Core, Dapper, ADO.NET, or any other
/// state-based persistence technology. For event-sourced aggregates, use <c>Encina.Marten.AggregateBase</c> instead.
/// </para>
/// <para>
/// Aggregate roots:
/// <list type="bullet">
///   <item><description>Are the entry point for a cluster of related entities.</description></item>
///   <item><description>Enforce invariants across the entire aggregate.</description></item>
///   <item><description>Are the unit of persistence (saved/loaded as a whole).</description></item>
///   <item><description>Raise domain events to communicate with other aggregates.</description></item>
///   <item><description>Support optimistic concurrency via RowVersion.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : AggregateRoot&lt;OrderId&gt;
/// {
///     private readonly List&lt;OrderLine&gt; _lines = [];
///
///     public IReadOnlyList&lt;OrderLine&gt; Lines => _lines.AsReadOnly();
///     public string CustomerName { get; private set; }
///     public OrderStatus Status { get; private set; }
///
///     private Order() : base(default!) { } // For ORM
///
///     public static Order Create(OrderId id, string customerName)
///     {
///         var order = new Order(id) { CustomerName = customerName, Status = OrderStatus.Pending };
///         order.RaiseDomainEvent(new OrderCreated(id.Value, customerName));
///         return order;
///     }
///
///     public void AddLine(ProductId productId, int quantity, decimal price)
///     {
///         if (Status != OrderStatus.Pending)
///             throw new DomainException("Cannot modify a non-pending order");
///
///         _lines.Add(new OrderLine(productId, quantity, price));
///         RaiseDomainEvent(new OrderLineAdded(Id.Value, productId.Value, quantity));
///     }
/// }
/// </code>
/// </example>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>, IConcurrencyAware
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the concurrency token (row version) for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is automatically managed by the persistence layer (e.g., EF Core)
    /// and should not be modified directly in domain code.
    /// </para>
    /// <para>
    /// When using EF Core, configure this property as a concurrency token:
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .Property(e => e.RowVersion)
    ///     .IsRowVersion();
    /// </code>
    /// </para>
    /// </remarks>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this aggregate root.</param>
    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Raises a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    /// <remarks>
    /// <para>
    /// This is an alias for <see cref="Entity{TId}.AddDomainEvent"/> that provides
    /// a more expressive name for aggregate root usage patterns.
    /// </para>
    /// <para>
    /// Domain events should be dispatched after successful persistence to ensure
    /// consistency. The persistence layer (e.g., EF Core SaveChanges interceptor)
    /// should collect and dispatch these events after the transaction commits.
    /// </para>
    /// </remarks>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        AddDomainEvent(domainEvent);
    }
}

/// <summary>
/// Base class for auditable aggregate roots that track creation and modification timestamps.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditable
    where TId : notnull
{
    /// <summary>
    /// Gets the time provider used for setting audit timestamps.
    /// </summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; private set; }

    /// <inheritdoc />
    public string? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTime? ModifiedAtUtc { get; private set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableAggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this aggregate root.</param>
    /// <param name="timeProvider">Optional time provider for testing. Defaults to <see cref="System.TimeProvider.System"/>.</param>
    protected AuditableAggregateRoot(TId id, TimeProvider? timeProvider = null) : base(id)
    {
        TimeProvider = timeProvider ?? TimeProvider.System;
        CreatedAtUtc = TimeProvider.GetUtcNow().UtcDateTime;
    }

    /// <summary>
    /// Sets the creation audit information.
    /// </summary>
    /// <param name="createdBy">The user who created this aggregate.</param>
    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Sets the modification audit information.
    /// </summary>
    /// <param name="modifiedBy">The user who modified this aggregate.</param>
    public void SetModifiedBy(string modifiedBy)
    {
        ModifiedBy = modifiedBy;
        ModifiedAtUtc = TimeProvider.GetUtcNow().UtcDateTime;
    }
}

/// <summary>
/// Base class for soft-deletable aggregate roots.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public abstract class SoftDeletableAggregateRoot<TId> : AuditableAggregateRoot<TId>, ISoftDeletable
    where TId : notnull
{
    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTime? DeletedAtUtc { get; private set; }

    /// <inheritdoc />
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeletableAggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this aggregate root.</param>
    /// <param name="timeProvider">Optional time provider for testing. Defaults to <see cref="System.TimeProvider.System"/>.</param>
    protected SoftDeletableAggregateRoot(TId id, TimeProvider? timeProvider = null) : base(id, timeProvider)
    {
    }

    /// <summary>
    /// Marks this aggregate as deleted (soft delete).
    /// </summary>
    /// <param name="deletedBy">The user who deleted this aggregate.</param>
    public virtual void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = TimeProvider.GetUtcNow().UtcDateTime;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted aggregate.
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
