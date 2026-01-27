using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Encina.TestInfrastructure.Entities;

namespace Encina.TestInfrastructure.Mappings;

/// <summary>
/// EF Core entity configuration for <see cref="TenantTestEntity"/>.
/// </summary>
public sealed class TenantTestEntityConfiguration : IEntityTypeConfiguration<TenantTestEntity>
{
    private readonly string? _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantTestEntityConfiguration"/> class.
    /// </summary>
    /// <param name="schema">Optional schema name (defaults to dbo).</param>
    public TenantTestEntityConfiguration(string? schema = null)
    {
        _schema = schema;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantTestEntity> builder)
    {
        if (!string.IsNullOrEmpty(_schema))
        {
            builder.ToTable("TenantTestEntities", _schema);
        }
        else
        {
            builder.ToTable("TenantTestEntities");
        }

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Description)
            .HasMaxLength(1024);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.IsActive });
        builder.HasIndex(e => e.CreatedAtUtc);
    }
}

/// <summary>
/// EF Core entity configuration for <see cref="ModuleTestEntity"/>.
/// </summary>
public sealed class ModuleTestEntityConfiguration : IEntityTypeConfiguration<ModuleTestEntity>
{
    private readonly string? _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleTestEntityConfiguration"/> class.
    /// </summary>
    /// <param name="schema">Optional schema name.</param>
    public ModuleTestEntityConfiguration(string? schema = null)
    {
        _schema = schema;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ModuleTestEntity> builder)
    {
        if (!string.IsNullOrEmpty(_schema))
        {
            builder.ToTable("ModuleTestEntities", _schema);
        }
        else
        {
            builder.ToTable("ModuleTestEntities");
        }

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ModuleName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Data)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(e => e.ModuleName);
    }
}

/// <summary>
/// EF Core entity configuration for <see cref="OrdersModuleEntity"/>.
/// Configured for the "orders" schema.
/// </summary>
public sealed class OrdersModuleEntityConfiguration : IEntityTypeConfiguration<OrdersModuleEntity>
{
    private readonly string _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrdersModuleEntityConfiguration"/> class.
    /// </summary>
    /// <param name="schema">The schema name (defaults to "orders").</param>
    public OrdersModuleEntityConfiguration(string schema = "orders")
    {
        _schema = schema;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrdersModuleEntity> builder)
    {
        builder.ToTable("Orders", _schema);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CustomerName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Total)
            .HasPrecision(18, 2);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.OrderNumber).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAtUtc);
    }
}

/// <summary>
/// EF Core entity configuration for <see cref="InventoryModuleEntity"/>.
/// Configured for the "inventory" schema.
/// </summary>
public sealed class InventoryModuleEntityConfiguration : IEntityTypeConfiguration<InventoryModuleEntity>
{
    private readonly string _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryModuleEntityConfiguration"/> class.
    /// </summary>
    /// <param name="schema">The schema name (defaults to "inventory").</param>
    public InventoryModuleEntityConfiguration(string schema = "inventory")
    {
        _schema = schema;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InventoryModuleEntity> builder)
    {
        builder.ToTable("InventoryItems", _schema);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(e => e.Sku).IsUnique();
        builder.HasIndex(e => e.QuantityInStock);
    }
}

/// <summary>
/// EF Core entity configuration for <see cref="SharedLookupEntity"/>.
/// Configured for the "shared" schema.
/// </summary>
public sealed class SharedLookupEntityConfiguration : IEntityTypeConfiguration<SharedLookupEntity>
{
    private readonly string _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedLookupEntityConfiguration"/> class.
    /// </summary>
    /// <param name="schema">The schema name (defaults to "shared").</param>
    public SharedLookupEntityConfiguration(string schema = "shared")
    {
        _schema = schema;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SharedLookupEntity> builder)
    {
        builder.ToTable("Lookups", _schema);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => new { e.Category, e.Code }).IsUnique();
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core entity configuration for <see cref="ReadWriteTestEntity"/>.
/// </summary>
public sealed class ReadWriteTestEntityConfiguration : IEntityTypeConfiguration<ReadWriteTestEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReadWriteTestEntity> builder)
    {
        builder.ToTable("ReadWriteTestEntities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        // LastReadReplica is not mapped to the database
        builder.Ignore(e => e.LastReadReplica);

        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.WriteCounter);
    }
}
