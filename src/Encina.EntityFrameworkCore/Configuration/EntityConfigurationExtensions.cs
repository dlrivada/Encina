using System.Reflection;
using Encina.DomainModeling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Configuration;

/// <summary>
/// Extension methods for configuring domain entities in Entity Framework Core.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide convenient, standardized configuration for common
/// domain entity patterns. They help ensure consistency across all entity configurations
/// and reduce boilerplate code.
/// </para>
/// <para>
/// <b>Available configurations</b>:
/// <list type="bullet">
/// <item><description><see cref="ConfigureConcurrencyToken{T}"/>: Row version for optimistic concurrency</description></item>
/// <item><description><see cref="ConfigureAuditProperties{T}"/>: Created/Modified timestamps and users</description></item>
/// <item><description><see cref="ConfigureSoftDelete{T}"/>: Soft delete with global query filter</description></item>
/// <item><description><see cref="ConfigureAggregateRoot{T,TId}"/>: Combined configuration for aggregate roots</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderConfiguration : IEntityTypeConfiguration&lt;Order&gt;
/// {
///     public void Configure(EntityTypeBuilder&lt;Order&gt; builder)
///     {
///         builder.HasKey(o => o.Id);
///
///         // Apply all aggregate root configurations
///         builder.ConfigureAggregateRoot&lt;Order, OrderId&gt;();
///
///         // Or apply individual configurations
///         // builder.ConfigureConcurrencyToken();
///         // builder.ConfigureAuditProperties();
///         // builder.ConfigureSoftDelete();
///     }
/// }
/// </code>
/// </example>
public static class EntityConfigurationExtensions
{
    /// <summary>
    /// Configures the row version property for optimistic concurrency control.
    /// </summary>
    /// <typeparam name="T">The entity type that implements <see cref="IConcurrencyAware"/>.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the <see cref="IConcurrencyAware.RowVersion"/> property as
    /// a row version (timestamp) column, which is automatically updated by the database
    /// on every update operation.
    /// </para>
    /// <para>
    /// <b>Database-specific behavior</b>:
    /// <list type="bullet">
    /// <item><description><b>SQL Server</b>: Uses <c>rowversion</c> data type (8 bytes, auto-increment)</description></item>
    /// <item><description><b>PostgreSQL</b>: Uses <c>xmin</c> system column or custom implementation</description></item>
    /// <item><description><b>SQLite</b>: Stores as BLOB, requires manual increment on update</description></item>
    /// <item><description><b>MySQL</b>: Uses <c>TIMESTAMP</c> or <c>VARBINARY(8)</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.ConfigureConcurrencyToken&lt;Order&gt;();
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureConcurrencyToken<T>(this EntityTypeBuilder<T> builder)
        where T : class, IConcurrencyAware
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        return builder;
    }

    /// <summary>
    /// Configures the audit properties for tracking creation and modification metadata.
    /// </summary>
    /// <typeparam name="T">The entity type that implements <see cref="IAuditable"/>.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the following properties from <see cref="IAuditable"/>:
    /// <list type="bullet">
    /// <item><description><see cref="IAuditable.CreatedAtUtc"/>: Required, when the entity was created</description></item>
    /// <item><description><see cref="IAuditable.CreatedBy"/>: Optional, who created the entity</description></item>
    /// <item><description><see cref="IAuditable.ModifiedAtUtc"/>: Optional, when the entity was last modified</description></item>
    /// <item><description><see cref="IAuditable.ModifiedBy"/>: Optional, who last modified the entity</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note</b>: This configuration does not automatically populate these values.
    /// Use a SaveChanges interceptor or override SaveChanges to set these properties.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.ConfigureAuditProperties&lt;Order&gt;();
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureAuditProperties<T>(this EntityTypeBuilder<T> builder)
        where T : class, IAuditable
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(256);

        builder.Property(e => e.ModifiedAtUtc);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(256);

        return builder;
    }

    /// <summary>
    /// Configures the audit properties for entities with automatic interceptor-based population.
    /// </summary>
    /// <typeparam name="T">The entity type that implements <see cref="IAuditableEntity"/>.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the same properties as <see cref="ConfigureAuditProperties{T}"/>,
    /// but is intended for entities implementing <see cref="IAuditableEntity"/> (with public setters)
    /// that are automatically populated by the <c>AuditInterceptor</c>.
    /// </para>
    /// <para>
    /// <b>IAuditableEntity vs IAuditable</b>:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="IAuditableEntity"/>: Has <b>public setters</b> for interceptor-based population.
    /// Use <see cref="ConfigureAuditableEntityProperties{T}"/> for these entities.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="IAuditable"/>: Has <b>getter-only</b> properties for method-based population.
    /// Use <see cref="ConfigureAuditProperties{T}"/> for these entities.
    /// </description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// The configuration includes:
    /// <list type="bullet">
    /// <item><description><see cref="IAuditableEntity.CreatedAtUtc"/>: Required, when the entity was created</description></item>
    /// <item><description><see cref="IAuditableEntity.CreatedBy"/>: Optional (max 256 chars), who created the entity</description></item>
    /// <item><description><see cref="IAuditableEntity.ModifiedAtUtc"/>: Optional, when the entity was last modified</description></item>
    /// <item><description><see cref="IAuditableEntity.ModifiedBy"/>: Optional (max 256 chars), who last modified the entity</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderConfiguration : IEntityTypeConfiguration&lt;Order&gt;
    /// {
    ///     public void Configure(EntityTypeBuilder&lt;Order&gt; builder)
    ///     {
    ///         builder.HasKey(o => o.Id);
    ///         builder.ConfigureAuditableEntityProperties();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureAuditableEntityProperties<T>(this EntityTypeBuilder<T> builder)
        where T : class, IAuditableEntity
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(256);

        builder.Property(e => e.ModifiedAtUtc);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(256);

        return builder;
    }

    /// <summary>
    /// Configures soft delete support with a global query filter.
    /// </summary>
    /// <typeparam name="T">The entity type that implements <see cref="ISoftDeletable"/>.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures:
    /// <list type="bullet">
    /// <item><description><see cref="ISoftDeletable.IsDeleted"/>: Required boolean property</description></item>
    /// <item><description><see cref="ISoftDeletable.DeletedAtUtc"/>: Optional timestamp when deleted</description></item>
    /// <item><description><see cref="ISoftDeletable.DeletedBy"/>: Optional user who performed the deletion</description></item>
    /// <item><description>Global query filter: <c>HasQueryFilter(e =&gt; !e.IsDeleted)</c></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Query Filter</b>: The global filter automatically excludes soft-deleted entities
    /// from all queries. To include deleted entities, use <c>IgnoreQueryFilters()</c>:
    /// </para>
    /// <code>
    /// var allOrders = await context.Orders.IgnoreQueryFilters().ToListAsync();
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.ConfigureSoftDelete&lt;Order&gt;();
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureSoftDelete<T>(this EntityTypeBuilder<T> builder)
        where T : class, ISoftDeletable
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAtUtc);

        builder.Property(e => e.DeletedBy)
            .HasMaxLength(256);

        builder.HasQueryFilter(e => !e.IsDeleted);

        return builder;
    }

    /// <summary>
    /// Configures an aggregate root with all applicable configurations.
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <typeparam name="TId">The aggregate root's identifier type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method applies configurations based on which interfaces the aggregate root implements:
    /// <list type="bullet">
    /// <item><description><see cref="IConcurrencyAware"/>: Calls <see cref="ConfigureConcurrencyToken{T}"/></description></item>
    /// <item><description><see cref="IAuditable"/>: Calls <see cref="ConfigureAuditProperties{T}"/></description></item>
    /// <item><description><see cref="ISoftDeletable"/>: Calls <see cref="ConfigureSoftDelete{T}"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Additionally, it ignores the <see cref="IAggregateRoot.DomainEvents"/> property to prevent
    /// EF Core from trying to map it to the database.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderConfiguration : IEntityTypeConfiguration&lt;Order&gt;
    /// {
    ///     public void Configure(EntityTypeBuilder&lt;Order&gt; builder)
    ///     {
    ///         builder.HasKey(o => o.Id);
    ///         builder.ConfigureAggregateRoot&lt;Order, OrderId&gt;();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureAggregateRoot<T, TId>(this EntityTypeBuilder<T> builder)
        where T : AggregateRoot<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Ignore the DomainEvents navigation property
        builder.Ignore(e => e.DomainEvents);

        // Always configure concurrency token for aggregate roots (they implement IConcurrencyAware)
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Check for IAuditableEntity (interceptor-based) first, then IAuditable (method-based)
        if (typeof(IAuditableEntity).IsAssignableFrom(typeof(T)))
        {
            ConfigureAuditableEntityPropertiesInternal(builder);
        }
        else if (typeof(IAuditable).IsAssignableFrom(typeof(T)))
        {
            // We need to cast the builder, but since T implements IAuditable, this is safe
            ConfigureAuditableProperties(builder);
        }

        // Check for ISoftDeletable
        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            ConfigureSoftDeletableProperties(builder);
        }

        return builder;
    }

    /// <summary>
    /// Configures an aggregate root with all applicable configurations (non-generic ID version).
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience overload that works with aggregate roots where the ID type
    /// doesn't need to be specified explicitly. It applies the same configurations as
    /// <see cref="ConfigureAggregateRoot{T,TId}"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.ConfigureAggregateRoot&lt;Order&gt;();
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureAggregateRoot<T>(this EntityTypeBuilder<T> builder)
        where T : class, IAggregateRoot, IConcurrencyAware
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Ignore the DomainEvents navigation property
        builder.Ignore(e => e.DomainEvents);

        // Configure concurrency token
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Check for IAuditableEntity (interceptor-based) first, then IAuditable (method-based)
        if (typeof(IAuditableEntity).IsAssignableFrom(typeof(T)))
        {
            ConfigureAuditableEntityPropertiesInternal(builder);
        }
        else if (typeof(IAuditable).IsAssignableFrom(typeof(T)))
        {
            ConfigureAuditableProperties(builder);
        }

        // Check for ISoftDeletable
        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            ConfigureSoftDeletableProperties(builder);
        }

        return builder;
    }

    /// <summary>
    /// Helper method to configure audit properties without requiring IAuditable constraint.
    /// </summary>
    private static void ConfigureAuditableProperties<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        var entityType = typeof(T);
        var createdAtProperty = entityType.GetProperty(nameof(IAuditable.CreatedAtUtc));
        var createdByProperty = entityType.GetProperty(nameof(IAuditable.CreatedBy));
        var modifiedAtProperty = entityType.GetProperty(nameof(IAuditable.ModifiedAtUtc));
        var modifiedByProperty = entityType.GetProperty(nameof(IAuditable.ModifiedBy));

        if (createdAtProperty is not null)
        {
            builder.Property(nameof(IAuditable.CreatedAtUtc))
                .IsRequired();
        }

        if (createdByProperty is not null)
        {
            builder.Property(nameof(IAuditable.CreatedBy))
                .HasMaxLength(256);
        }

        if (modifiedAtProperty is not null)
        {
            builder.Property(nameof(IAuditable.ModifiedAtUtc));
        }

        if (modifiedByProperty is not null)
        {
            builder.Property(nameof(IAuditable.ModifiedBy))
                .HasMaxLength(256);
        }
    }

    /// <summary>
    /// Helper method to configure audit properties for IAuditableEntity without requiring the constraint.
    /// Used internally by ConfigureAggregateRoot when the entity implements IAuditableEntity.
    /// </summary>
    private static void ConfigureAuditableEntityPropertiesInternal<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        var entityType = typeof(T);
        var createdAtProperty = entityType.GetProperty(nameof(IAuditableEntity.CreatedAtUtc));
        var createdByProperty = entityType.GetProperty(nameof(IAuditableEntity.CreatedBy));
        var modifiedAtProperty = entityType.GetProperty(nameof(IAuditableEntity.ModifiedAtUtc));
        var modifiedByProperty = entityType.GetProperty(nameof(IAuditableEntity.ModifiedBy));

        if (createdAtProperty is not null)
        {
            builder.Property(nameof(IAuditableEntity.CreatedAtUtc))
                .IsRequired();
        }

        if (createdByProperty is not null)
        {
            builder.Property(nameof(IAuditableEntity.CreatedBy))
                .HasMaxLength(256);
        }

        if (modifiedAtProperty is not null)
        {
            builder.Property(nameof(IAuditableEntity.ModifiedAtUtc));
        }

        if (modifiedByProperty is not null)
        {
            builder.Property(nameof(IAuditableEntity.ModifiedBy))
                .HasMaxLength(256);
        }
    }

    /// <summary>
    /// Helper method to configure soft delete properties without requiring ISoftDeletable constraint.
    /// </summary>
    private static void ConfigureSoftDeletableProperties<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        var entityType = typeof(T);
        var isDeletedProperty = entityType.GetProperty(nameof(ISoftDeletable.IsDeleted));
        var deletedAtProperty = entityType.GetProperty(nameof(ISoftDeletable.DeletedAtUtc));
        var deletedByProperty = entityType.GetProperty(nameof(ISoftDeletable.DeletedBy));

        if (isDeletedProperty is not null)
        {
            builder.Property(nameof(ISoftDeletable.IsDeleted))
                .IsRequired()
                .HasDefaultValue(false);
        }

        if (deletedAtProperty is not null)
        {
            builder.Property(nameof(ISoftDeletable.DeletedAtUtc));
        }

        if (deletedByProperty is not null)
        {
            builder.Property(nameof(ISoftDeletable.DeletedBy))
                .HasMaxLength(256);
        }

        // Add query filter - we need to build the expression dynamically
        if (isDeletedProperty is not null)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
            var property = System.Linq.Expressions.Expression.Property(parameter, isDeletedProperty);
            var notDeleted = System.Linq.Expressions.Expression.Not(property);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(notDeleted, parameter);

            builder.HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// Applies soft delete query filters to all entities implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method scans all entity types in the model and applies a global query filter
    /// (<c>HasQueryFilter(e =&gt; !e.IsDeleted)</c>) to those implementing <see cref="ISoftDeletable"/>.
    /// </para>
    /// <para>
    /// <b>Usage</b>: Call this method in your <c>OnModelCreating</c> override after all entities
    /// are configured, or use it in combination with <c>ApplyConfigurationsFromAssembly</c>.
    /// </para>
    /// <para>
    /// <b>Note</b>: If you need to include soft-deleted entities in a query, use
    /// <c>IgnoreQueryFilters()</c> on the query.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///
    ///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    ///
    ///     // Apply soft delete filters to all ISoftDeletable entities
    ///     modelBuilder.ApplySoftDeleteQueryFilters();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(EntityConfigurationExtensions)
                    .GetMethod(nameof(ApplySoftDeleteQueryFilterInternal), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, [modelBuilder]);
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// Internal method to apply soft delete query filter to a specific entity type.
    /// </summary>
    private static void ApplySoftDeleteQueryFilterInternal<T>(ModelBuilder modelBuilder)
        where T : class, ISoftDeletable
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Configures soft delete support for entities implementing <see cref="ISoftDeletableEntity"/> with mutable properties.
    /// </summary>
    /// <typeparam name="T">The entity type that implements <see cref="ISoftDeletableEntity"/>.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the same properties as <see cref="ConfigureSoftDelete{T}"/>,
    /// but is intended for entities implementing <see cref="ISoftDeletableEntity"/> (with public setters)
    /// that are automatically populated by the <c>SoftDeleteInterceptor</c>.
    /// </para>
    /// <para>
    /// <b>ISoftDeletableEntity vs ISoftDeletable</b>:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="ISoftDeletableEntity"/>: Has <b>public setters</b> for interceptor-based population.
    /// Use <see cref="ConfigureSoftDeletableEntity{T}"/> for these entities.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="ISoftDeletable"/>: Has <b>getter-only</b> properties for method-based population.
    /// Use <see cref="ConfigureSoftDelete{T}"/> for these entities.
    /// </description>
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderConfiguration : IEntityTypeConfiguration&lt;Order&gt;
    /// {
    ///     public void Configure(EntityTypeBuilder&lt;Order&gt; builder)
    ///     {
    ///         builder.HasKey(o => o.Id);
    ///         builder.ConfigureSoftDeletableEntity();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureSoftDeletableEntity<T>(this EntityTypeBuilder<T> builder)
        where T : class, ISoftDeletableEntity
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAtUtc);

        builder.Property(e => e.DeletedBy)
            .HasMaxLength(256);

        builder.HasQueryFilter(e => !e.IsDeleted);

        return builder;
    }

    /// <summary>
    /// Configures the entity as a SQL Server temporal table (system-versioned).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="historyTableName">
    /// Optional custom name for the history table. If not specified, defaults to <c>{TableName}History</c>.
    /// </param>
    /// <param name="historyTableSchema">
    /// Optional schema for the history table. If not specified, uses the same schema as the main table.
    /// </param>
    /// <param name="periodStartColumnName">
    /// Optional name for the period start column. Defaults to <c>PeriodStart</c>.
    /// </param>
    /// <param name="periodEndColumnName">
    /// Optional name for the period end column. Defaults to <c>PeriodEnd</c>.
    /// </param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// SQL Server temporal tables (system-versioned tables) automatically track the full history
    /// of data changes. This method configures an entity to use temporal table features.
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description>SQL Server 2016 or later</description></item>
    /// <item><description>Microsoft.EntityFrameworkCore.SqlServer package</description></item>
    /// <item><description>EF Core 6.0 or later</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Generated Table Structure</b>:
    /// <list type="bullet">
    /// <item><description>Main table: Contains current data with hidden period columns</description></item>
    /// <item><description>History table: Contains all historical versions of rows</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note</b>: This method uses EF Core's built-in temporal table support via
    /// <c>ToTable(tb => tb.IsTemporal())</c>. The SQL Server provider handles
    /// the DDL generation for temporal tables.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderConfiguration : IEntityTypeConfiguration&lt;Order&gt;
    /// {
    ///     public void Configure(EntityTypeBuilder&lt;Order&gt; builder)
    ///     {
    ///         builder.HasKey(o => o.Id);
    ///
    ///         // Configure as temporal table with default settings
    ///         builder.ConfigureTemporalTable();
    ///
    ///         // Or with custom history table
    ///         builder.ConfigureTemporalTable(
    ///             historyTableName: "OrderAuditHistory",
    ///             historyTableSchema: "audit");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureTemporalTable<T>(
        this EntityTypeBuilder<T> builder,
        string? historyTableName = null,
        string? historyTableSchema = null,
        string periodStartColumnName = "PeriodStart",
        string periodEndColumnName = "PeriodEnd")
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(tb => tb.IsTemporal(temporal =>
        {
            temporal.HasPeriodStart(periodStartColumnName);
            temporal.HasPeriodEnd(periodEndColumnName);

            if (historyTableName is not null)
            {
                if (historyTableSchema is not null)
                {
                    temporal.UseHistoryTable(historyTableName, historyTableSchema);
                }
                else
                {
                    temporal.UseHistoryTable(historyTableName);
                }
            }
        }));

        return builder;
    }

    /// <summary>
    /// Configures the entity as a SQL Server temporal table using configuration options.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="options">The temporal table configuration options.</param>
    /// <param name="tableName">
    /// The name of the main table. Required to compute the history table name when using the suffix pattern.
    /// </param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This overload uses <see cref="Encina.Messaging.Temporal.TemporalTableOptions"/> for configuration,
    /// allowing consistent defaults across all temporal tables in an application.
    /// </para>
    /// <para>
    /// The history table name is computed as <c>{tableName}{DefaultHistoryTableSuffix}</c> from options.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In DbContext.OnModelCreating
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.Entity&lt;Order&gt;(entity =>
    ///     {
    ///         entity.ToTable("Orders");
    ///         entity.ConfigureTemporalTable(_temporalOptions, "Orders");
    ///     });
    /// }
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> ConfigureTemporalTable<T>(
        this EntityTypeBuilder<T> builder,
        Messaging.Temporal.TemporalTableOptions options,
        string tableName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tableName);

        var historyTableName = $"{tableName}{options.DefaultHistoryTableSuffix}";

        return builder.ConfigureTemporalTable(
            historyTableName,
            options.DefaultHistoryTableSchema,
            options.DefaultPeriodStartColumnName,
            options.DefaultPeriodEndColumnName);
    }
}
