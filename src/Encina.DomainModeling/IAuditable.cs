namespace Encina.DomainModeling;

#region Granular Audit Interfaces (Mutable - for Interceptors)

/// <summary>
/// Tracks the creation timestamp for an entity.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for automatic population by EF Core SaveChanges interceptors.
/// Entities implementing this interface will have their <see cref="CreatedAtUtc"/> property
/// automatically set when they are added to the database.
/// </para>
/// <para>
/// The property has a public setter to allow the interceptor to populate it.
/// For immutable domain patterns where audit fields are set via methods, use <see cref="IAuditable"/> instead.
/// </para>
/// </remarks>
public interface ICreatedAtUtc
{
    /// <summary>
    /// Gets or sets the timestamp when this entity was created (UTC).
    /// </summary>
    /// <remarks>
    /// This property is automatically populated by the <c>AuditInterceptor</c> when
    /// the entity state is <c>EntityState.Added</c>.
    /// </remarks>
    DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Tracks the user who created an entity.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for automatic population by EF Core SaveChanges interceptors.
/// Entities implementing this interface will have their <see cref="CreatedBy"/> property
/// automatically set from <c>IRequestContext.UserId</c> when they are added to the database.
/// </para>
/// <para>
/// The property has a public setter to allow the interceptor to populate it.
/// For immutable domain patterns where audit fields are set via methods, use <see cref="IAuditable"/> instead.
/// </para>
/// </remarks>
public interface ICreatedBy
{
    /// <summary>
    /// Gets or sets the identifier of the user who created this entity.
    /// </summary>
    /// <remarks>
    /// This property is automatically populated by the <c>AuditInterceptor</c> when
    /// the entity state is <c>EntityState.Added</c>. The value comes from
    /// <c>IRequestContext.UserId</c>.
    /// </remarks>
    string? CreatedBy { get; set; }
}

/// <summary>
/// Tracks the last modification timestamp for an entity.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for automatic population by EF Core SaveChanges interceptors.
/// Entities implementing this interface will have their <see cref="ModifiedAtUtc"/> property
/// automatically set when they are modified in the database.
/// </para>
/// <para>
/// The property has a public setter to allow the interceptor to populate it.
/// For immutable domain patterns where audit fields are set via methods, use <see cref="IAuditable"/> instead.
/// </para>
/// </remarks>
public interface IModifiedAtUtc
{
    /// <summary>
    /// Gets or sets the timestamp when this entity was last modified (UTC).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is automatically populated by the <c>AuditInterceptor</c> when
    /// the entity state is <c>EntityState.Modified</c>.
    /// </para>
    /// <para>
    /// The value is <c>null</c> if the entity has never been modified after creation.
    /// </para>
    /// </remarks>
    DateTime? ModifiedAtUtc { get; set; }
}

/// <summary>
/// Tracks the user who last modified an entity.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for automatic population by EF Core SaveChanges interceptors.
/// Entities implementing this interface will have their <see cref="ModifiedBy"/> property
/// automatically set from <c>IRequestContext.UserId</c> when they are modified in the database.
/// </para>
/// <para>
/// The property has a public setter to allow the interceptor to populate it.
/// For immutable domain patterns where audit fields are set via methods, use <see cref="IAuditable"/> instead.
/// </para>
/// </remarks>
public interface IModifiedBy
{
    /// <summary>
    /// Gets or sets the identifier of the user who last modified this entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is automatically populated by the <c>AuditInterceptor</c> when
    /// the entity state is <c>EntityState.Modified</c>. The value comes from
    /// <c>IRequestContext.UserId</c>.
    /// </para>
    /// <para>
    /// The value is <c>null</c> if the entity has never been modified after creation.
    /// </para>
    /// </remarks>
    string? ModifiedBy { get; set; }
}

/// <summary>
/// Composite interface for entities requiring full audit tracking with mutable properties.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary interface to implement for automatic interceptor-based audit population.
/// Entities implementing this interface will have all four audit properties
/// (<see cref="ICreatedAtUtc.CreatedAtUtc"/>, <see cref="ICreatedBy.CreatedBy"/>,
/// <see cref="IModifiedAtUtc.ModifiedAtUtc"/>, <see cref="IModifiedBy.ModifiedBy"/>)
/// automatically populated by the <c>AuditInterceptor</c>.
/// </para>
/// <para>
/// <b>IAuditableEntity vs IAuditable:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="IAuditableEntity"/>: Has <b>public setters</b> for interceptor-based population.
///       Use this when you want automatic audit field population via EF Core interceptors.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IAuditable"/>: Has <b>getter-only</b> properties for method-based population.
///       Use this for immutable domain patterns where audit fields are set via domain methods
///       (e.g., <c>SetCreatedBy</c>, <c>SetModifiedBy</c>).
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Automatic audit population via interceptor
/// public class Order : AuditedAggregateRoot&lt;OrderId&gt;
/// {
///     // IAuditableEntity properties are inherited and auto-populated
///     public string CustomerName { get; private set; }
/// }
///
/// // Configuration
/// services.AddEncinaEntityFrameworkCore(config =>
/// {
///     config.UseAuditing = true;
/// });
/// </code>
/// </example>
public interface IAuditableEntity : ICreatedAtUtc, ICreatedBy, IModifiedAtUtc, IModifiedBy
{
}

#endregion

#region Immutable Audit Interface (Getter-only - for Domain Methods)

/// <summary>
/// Interface for entities that track creation and modification audit information
/// using an immutable pattern with getter-only properties.
/// </summary>
/// <remarks>
/// <para>
/// This interface uses getter-only properties, making it suitable for immutable domain models
/// where audit fields are set via explicit domain methods (e.g., <c>SetCreatedBy</c>, <c>SetModifiedBy</c>).
/// </para>
/// <para>
/// <b>IAuditable vs IAuditableEntity:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="IAuditable"/>: Has <b>getter-only</b> properties for method-based population.
///       Use this for immutable domain patterns where audit fields are set via domain methods.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IAuditableEntity"/>: Has <b>public setters</b> for interceptor-based population.
///       Use this when you want automatic audit field population via EF Core interceptors.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="IAuditableEntity"/>
/// <seealso cref="AuditableAggregateRoot{TId}"/>
public interface IAuditable
{
    /// <summary>
    /// Gets the timestamp when this entity was created (UTC).
    /// </summary>
    DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who created this entity.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the timestamp when this entity was last modified (UTC).
    /// </summary>
    DateTime? ModifiedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified this entity.
    /// </summary>
    string? ModifiedBy { get; }
}

/// <summary>
/// Interface for entities that support soft delete with getter-only properties.
/// </summary>
/// <remarks>
/// <para>
/// Soft delete keeps the record in the database but marks it as deleted.
/// This is useful for audit trails, data recovery, and GDPR compliance.
/// </para>
/// <para>
/// Query filters should be applied to exclude soft-deleted entities from normal queries.
/// </para>
/// <para>
/// <b>ISoftDeletable vs ISoftDeletableEntity:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="ISoftDeletable"/>: Has <b>getter-only</b> properties for method-based population.
///       Use this for immutable domain patterns where soft delete fields are set via domain methods
///       (e.g., <c>Delete</c>, <c>Restore</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="ISoftDeletableEntity"/>: Has <b>public setters</b> for interceptor-based population.
///       Use this when you want automatic soft delete field population via EF Core interceptors.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="ISoftDeletableEntity"/>
/// <seealso cref="SoftDeletableAggregateRoot{TId}"/>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the timestamp when this entity was deleted (UTC).
    /// </summary>
    DateTime? DeletedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who deleted this entity.
    /// </summary>
    string? DeletedBy { get; }
}

/// <summary>
/// Interface for entities that support soft delete with mutable properties for interceptor-based population.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for automatic population by EF Core SaveChanges interceptors.
/// Entities implementing this interface will have their soft delete properties automatically
/// set when they are deleted from the database (delete operation is converted to soft delete).
/// </para>
/// <para>
/// The properties have public setters to allow the interceptor to populate them.
/// For immutable domain patterns where soft delete fields are set via methods, use <see cref="ISoftDeletable"/> instead.
/// </para>
/// <para>
/// <b>ISoftDeletableEntity vs ISoftDeletable:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="ISoftDeletableEntity"/>: Has <b>public setters</b> for interceptor-based population.
///       Use this when you want automatic soft delete field population via EF Core interceptors.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="ISoftDeletable"/>: Has <b>getter-only</b> properties for method-based population.
///       Use this for immutable domain patterns where soft delete fields are set via domain methods.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Automatic soft delete via interceptor
/// public class Order : FullyAuditedAggregateRoot&lt;OrderId&gt;
/// {
///     // ISoftDeletableEntity properties are inherited and auto-populated
///     // when a delete operation is performed
///     public string CustomerName { get; private set; }
/// }
///
/// // Configuration
/// services.AddEncinaEntityFrameworkCore(config =>
/// {
///     config.UseSoftDelete = true;
/// });
/// </code>
/// </example>
/// <seealso cref="ISoftDeletable"/>
/// <seealso cref="FullyAuditedAggregateRoot{TId}"/>
public interface ISoftDeletableEntity : ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    /// <remarks>
    /// This property is automatically set to <c>true</c> by the <c>SoftDeleteInterceptor</c>
    /// when the entity state is <c>EntityState.Deleted</c>.
    /// </remarks>
    new bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this entity was deleted (UTC).
    /// </summary>
    /// <remarks>
    /// This property is automatically populated by the <c>SoftDeleteInterceptor</c>
    /// when the entity is soft-deleted.
    /// </remarks>
    new DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted this entity.
    /// </summary>
    /// <remarks>
    /// This property is automatically populated by the <c>SoftDeleteInterceptor</c>
    /// from <c>IRequestContext.UserId</c> when the entity is soft-deleted.
    /// </remarks>
    new string? DeletedBy { get; set; }
}

#endregion

#region Concurrency and Versioning Interfaces

/// <summary>
/// Interface for entities that support optimistic concurrency using a binary row version
/// with getter-only property for immutable domain patterns.
/// </summary>
/// <remarks>
/// <para>
/// Optimistic concurrency uses a version token (row version, ETag) to detect conflicts
/// when multiple clients try to update the same record simultaneously.
/// </para>
/// <para>
/// <b>IConcurrencyAware vs IConcurrencyAwareEntity:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="IConcurrencyAware"/>: Has a <b>getter-only</b> property for immutable domain patterns.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IConcurrencyAwareEntity"/>: Has a <b>public setter</b> for interceptor-based population.
///       Use this when you want automatic row version management via EF Core interceptors.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Supported databases with native row versioning:
/// </para>
/// <list type="bullet">
///   <item><description><b>SQL Server</b>: Uses <c>ROWVERSION</c>/<c>TIMESTAMP</c> columns (automatic)</description></item>
///   <item><description><b>PostgreSQL</b>: Uses <c>xmin</c> system column or custom <c>bytea</c> column</description></item>
/// </list>
/// <para>
/// For databases that don't support native row versioning (SQLite, MySQL, MongoDB),
/// use <see cref="IVersioned"/> or <see cref="IVersionedEntity"/> instead.
/// </para>
/// </remarks>
/// <seealso cref="IConcurrencyAwareEntity"/>
/// <seealso cref="IVersioned"/>
public interface IConcurrencyAware
{
    /// <summary>
    /// Gets the concurrency token (row version) for optimistic concurrency control.
    /// </summary>
    byte[]? RowVersion { get; }
}

/// <summary>
/// Interface for entities that support optimistic concurrency using a binary row version
/// with mutable property for interceptor-based population.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for automatic population by database providers (EF Core, ADO.NET, Dapper).
/// Entities implementing this interface will have their <see cref="RowVersion"/> property
/// automatically managed by the database provider during read/write operations.
/// </para>
/// <para>
/// The property has a public setter to allow the provider to populate it.
/// For immutable domain patterns where the row version is read-only, use <see cref="IConcurrencyAware"/> instead.
/// </para>
/// <para>
/// <b>IConcurrencyAwareEntity vs IConcurrencyAware:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="IConcurrencyAwareEntity"/>: Has a <b>public setter</b> for provider-based population.
///       Use this when you want automatic row version management via EF Core configuration.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IConcurrencyAware"/>: Has a <b>getter-only</b> property for immutable domain patterns.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : IEntity&lt;OrderId&gt;, IConcurrencyAwareEntity
/// {
///     public OrderId Id { get; init; }
///     public byte[] RowVersion { get; set; } = [];
///     public string Status { get; private set; } = "Pending";
///
///     public void Ship() =&gt; Status = "Shipped";
/// }
/// </code>
/// </example>
/// <seealso cref="IConcurrencyAware"/>
/// <seealso cref="IVersionedEntity"/>
public interface IConcurrencyAwareEntity : IConcurrencyAware
{
    /// <summary>
    /// Gets or sets the concurrency token (row version) for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is automatically populated by the database provider when the entity is read,
    /// and automatically checked/updated when the entity is saved.
    /// </para>
    /// <para>
    /// Do not modify this property manually in application code.
    /// The database provider manages its value.
    /// </para>
    /// </remarks>
    new byte[] RowVersion { get; set; }
}

/// <summary>
/// Interface for entities that track version numbers with getter-only property
/// for immutable domain patterns.
/// </summary>
/// <remarks>
/// <para>
/// Version numbers are useful for event sourcing, optimistic concurrency with integer versions,
/// and tracking the number of modifications to an entity.
/// </para>
/// <para>
/// <b>IVersioned vs IVersionedEntity:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="IVersioned"/>: Has a <b>getter-only</b> property for immutable domain patterns.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IVersionedEntity"/>: Has a <b>public setter</b> for interceptor-based population.
///       Use this when you want automatic version management via EF Core configuration.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Works consistently across all database providers:
/// </para>
/// <list type="bullet">
///   <item><description><b>MongoDB</b>: Uses a <c>_version</c> field with atomic increment</description></item>
///   <item><description><b>SQLite</b>: Uses integer version column</description></item>
///   <item><description><b>MySQL</b>: Uses integer version column</description></item>
///   <item><description><b>SQL Server</b>: Can use integer version as alternative to ROWVERSION</description></item>
///   <item><description><b>PostgreSQL</b>: Can use integer version as alternative to xmin</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IVersionedEntity"/>
/// <seealso cref="IConcurrencyAware"/>
public interface IVersioned
{
    /// <summary>
    /// Gets the version number of this entity.
    /// </summary>
    long Version { get; }
}

/// <summary>
/// Interface for entities that track version numbers with mutable property
/// for interceptor-based population.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for automatic population by database providers (EF Core, ADO.NET, Dapper).
/// Entities implementing this interface will have their <see cref="Version"/> property
/// automatically incremented by the database provider during update operations.
/// </para>
/// <para>
/// The property has a public setter to allow the provider to populate it.
/// For immutable domain patterns where the version is read-only, use <see cref="IVersioned"/> instead.
/// </para>
/// <para>
/// <b>IVersionedEntity vs IVersioned:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="IVersionedEntity"/>: Has a <b>public setter</b> for provider-based population.
///       Use this when you want automatic version management via EF Core configuration.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="IVersioned"/>: Has a <b>getter-only</b> property for immutable domain patterns.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Product : IEntity&lt;ProductId&gt;, IVersionedEntity
/// {
///     public ProductId Id { get; init; }
///     public int Version { get; set; }
///     public string Name { get; private set; } = string.Empty;
///     public decimal Price { get; private set; }
///
///     public void UpdatePrice(decimal newPrice) =&gt; Price = newPrice;
/// }
/// </code>
/// </example>
/// <seealso cref="IVersioned"/>
/// <seealso cref="IConcurrencyAwareEntity"/>
public interface IVersionedEntity : IVersioned
{
    /// <summary>
    /// Gets or sets the version number for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property starts at 0 for new entities and is incremented by 1 with each successful update.
    /// The increment is performed atomically by the database provider during the update operation.
    /// </para>
    /// <para>
    /// Do not modify this property manually in application code except when creating new entities
    /// (leave at 0 or default). The database provider manages version increments.
    /// </para>
    /// </remarks>
    new int Version { get; set; }
}

#endregion
