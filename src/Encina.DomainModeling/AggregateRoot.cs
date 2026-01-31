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

/// <summary>
/// Base class for aggregate roots with automatic audit tracking via EF Core interceptors.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="AggregateRoot{TId}"/> and implements <see cref="IAuditableEntity"/>
/// with public setters, allowing the <c>AuditInterceptor</c> to automatically populate
/// audit fields when aggregates are added or modified.
/// </para>
/// <para>
/// <b>AuditedAggregateRoot vs AuditableAggregateRoot:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="AuditedAggregateRoot{TId}"/>: Uses <see cref="IAuditableEntity"/> with <b>public setters</b>.
///       Audit fields are automatically populated by interceptors. Best for typical CRUD scenarios.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="AuditableAggregateRoot{TId}"/>: Uses <see cref="IAuditable"/> with <b>private setters</b>.
///       Audit fields are set via explicit domain methods (<c>SetCreatedBy</c>, <c>SetModifiedBy</c>).
///       Best for immutable domain patterns.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// This class also inherits <see cref="IConcurrencyAware"/> from <see cref="AggregateRoot{TId}"/>,
/// providing optimistic concurrency support via <see cref="AggregateRoot{TId}.RowVersion"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : AuditedAggregateRoot&lt;OrderId&gt;
/// {
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
/// }
///
/// // Configuration - audit fields are automatically populated
/// services.AddEncinaEntityFrameworkCore(config =>
/// {
///     config.UseAuditing = true;
/// });
/// </code>
/// </example>
public abstract class AuditedAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity
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
    /// Initializes a new instance of the <see cref="AuditedAggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this aggregate root.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testing. In production, the <c>AuditInterceptor</c>
    /// sets the timestamps, so this parameter is typically only used in tests.
    /// </param>
    protected AuditedAggregateRoot(TId id, TimeProvider? timeProvider = null) : base(id)
    {
        TimeProvider = timeProvider ?? TimeProvider.System;
    }
}

/// <summary>
/// Base class for aggregate roots with automatic audit tracking and soft delete support.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="AuditedAggregateRoot{TId}"/> and implements <see cref="ISoftDeletable"/>,
/// combining automatic audit population with soft delete capabilities. Use this when you need:
/// <list type="bullet">
///   <item><description>Automatic creation and modification tracking via interceptors</description></item>
///   <item><description>Soft delete support with deletion tracking</description></item>
///   <item><description>Optimistic concurrency via RowVersion</description></item>
/// </list>
/// </para>
/// <para>
/// Soft delete properties (<see cref="IsDeleted"/>, <see cref="DeletedAtUtc"/>, <see cref="DeletedBy"/>)
/// have public setters for interceptor compatibility, but prefer using the <see cref="Delete"/> and
/// <see cref="Restore"/> methods for domain logic as they maintain consistency.
/// </para>
/// <para>
/// <b>FullyAuditedAggregateRoot vs SoftDeletableAggregateRoot:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="FullyAuditedAggregateRoot{TId}"/>: Uses <see cref="IAuditableEntity"/> with <b>public setters</b>.
///       Best for interceptor-based automatic population.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="SoftDeletableAggregateRoot{TId}"/>: Uses <see cref="IAuditable"/> with <b>private setters</b>.
///       Best for method-based population in immutable domain patterns.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Customer : FullyAuditedAggregateRoot&lt;CustomerId&gt;
/// {
///     public string Name { get; private set; }
///     public string Email { get; private set; }
///
///     private Customer() : base(default!) { } // For ORM
///
///     public static Customer Create(CustomerId id, string name, string email)
///     {
///         var customer = new Customer(id) { Name = name, Email = email };
///         customer.RaiseDomainEvent(new CustomerCreated(id.Value, name));
///         return customer;
///     }
///
///     public void Deactivate(string? deletedBy = null)
///     {
///         Delete(deletedBy);
///         RaiseDomainEvent(new CustomerDeactivated(Id.Value));
///     }
/// }
/// </code>
/// </example>
public abstract class FullyAuditedAggregateRoot<TId> : AuditedAggregateRoot<TId>, ISoftDeletable
    where TId : notnull
{
    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAtUtc { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FullyAuditedAggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this aggregate root.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testing. In production, the <c>AuditInterceptor</c>
    /// sets the timestamps, so this parameter is typically only used in tests.
    /// </param>
    protected FullyAuditedAggregateRoot(TId id, TimeProvider? timeProvider = null) : base(id, timeProvider)
    {
    }

    /// <summary>
    /// Marks this aggregate as deleted (soft delete).
    /// </summary>
    /// <param name="deletedBy">The identifier of the user who deleted this aggregate.</param>
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
    /// Restores a soft-deleted aggregate.
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
