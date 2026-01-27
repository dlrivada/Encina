using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Encina.TestInfrastructure.DbContexts;

/// <summary>
/// DbContext for Module Isolation integration tests.
/// Configures entities across multiple schemas to test schema boundary enforcement.
/// </summary>
/// <remarks>
/// <para>
/// This context maps entities to three different schemas:
/// <list type="bullet">
/// <item><description><b>orders</b>: Contains <see cref="OrdersModuleEntity"/> (Module A's data)</description></item>
/// <item><description><b>inventory</b>: Contains <see cref="InventoryModuleEntity"/> (Module B's data)</description></item>
/// <item><description><b>shared</b>: Contains <see cref="SharedLookupEntity"/> (accessible to all modules)</description></item>
/// </list>
/// </para>
/// <para>
/// The schema mappings are configured in <see cref="OnModelCreating"/> using
/// <c>ToTable(tableName, schemaName)</c>. The actual schema creation is handled
/// by <see cref="ModuleIsolationSchema"/> utilities.
/// </para>
/// <para>
/// <b>Note for SQLite</b>: SQLite does not support schemas. When running against SQLite,
/// the schema names are ignored and all tables exist in a single namespace.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create context with interceptor for module isolation testing
/// var interceptor = new ModuleSchemaValidationInterceptor(...);
/// var options = new DbContextOptionsBuilder&lt;ModuleIsolationTestDbContext&gt;()
///     .UseSqlServer(connectionString)
///     .AddInterceptors(interceptor)
///     .Options;
///
/// await using var context = new ModuleIsolationTestDbContext(options);
///
/// // Set module context before querying
/// moduleContext.SetModule("Orders");
/// var orders = await context.Orders.ToListAsync(); // Works
/// var items = await context.InventoryItems.ToListAsync(); // Throws!
/// </code>
/// </example>
public class ModuleIsolationTestDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleIsolationTestDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ModuleIsolationTestDbContext(DbContextOptions<ModuleIsolationTestDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Orders entities (in the orders schema).
    /// </summary>
    /// <remarks>
    /// Accessible only to Module A (Orders) and infrastructure queries.
    /// </remarks>
    public DbSet<OrdersModuleEntity> Orders => Set<OrdersModuleEntity>();

    /// <summary>
    /// Gets or sets the Inventory Items entities (in the inventory schema).
    /// </summary>
    /// <remarks>
    /// Accessible only to Module B (Inventory) and infrastructure queries.
    /// </remarks>
    public DbSet<InventoryModuleEntity> InventoryItems => Set<InventoryModuleEntity>();

    /// <summary>
    /// Gets or sets the Shared Lookup entities (in the shared schema).
    /// </summary>
    /// <remarks>
    /// Accessible to all modules as it's in the shared schema.
    /// </remarks>
    public DbSet<SharedLookupEntity> SharedLookups => Set<SharedLookupEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOrdersEntity(modelBuilder);
        ConfigureInventoryEntity(modelBuilder);
        ConfigureSharedLookupEntity(modelBuilder);
    }

    /// <summary>
    /// Configures the <see cref="OrdersModuleEntity"/> mapping.
    /// </summary>
    private static void ConfigureOrdersEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrdersModuleEntity>(entity =>
        {
            // Map to orders schema
            entity.ToTable("Orders", ModuleIsolationSchema.SchemaNames.Orders);

            entity.HasKey(e => e.Id);

            entity.Property(e => e.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CustomerName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Total)
                .HasPrecision(18, 2);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(e => e.OrderNumber)
                .IsUnique();

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAtUtc);
        });
    }

    /// <summary>
    /// Configures the <see cref="InventoryModuleEntity"/> mapping.
    /// </summary>
    private static void ConfigureInventoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryModuleEntity>(entity =>
        {
            // Map to inventory schema
            entity.ToTable("InventoryItems", ModuleIsolationSchema.SchemaNames.Inventory);

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Sku)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.QuantityInStock)
                .HasDefaultValue(0);

            entity.Property(e => e.ReorderThreshold)
                .HasDefaultValue(10);

            entity.Property(e => e.LastUpdatedAtUtc)
                .IsRequired();

            entity.HasIndex(e => e.Sku)
                .IsUnique();

            entity.HasIndex(e => e.QuantityInStock);
        });
    }

    /// <summary>
    /// Configures the <see cref="SharedLookupEntity"/> mapping.
    /// </summary>
    private static void ConfigureSharedLookupEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SharedLookupEntity>(entity =>
        {
            // Map to shared schema
            entity.ToTable("Lookups", ModuleIsolationSchema.SchemaNames.Shared);

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            entity.HasIndex(e => new { e.Category, e.Code })
                .IsUnique();

            entity.HasIndex(e => e.IsActive);
        });
    }
}
