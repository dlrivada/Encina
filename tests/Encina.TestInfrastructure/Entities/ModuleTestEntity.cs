using Encina.DomainModeling;

namespace Encina.TestInfrastructure.Entities;

/// <summary>
/// Test entity for module isolation integration tests.
/// This entity is designed to be placed in a specific schema for testing
/// cross-schema access prevention and schema boundary enforcement.
/// </summary>
/// <remarks>
/// This entity is used in integration tests to verify:
/// <list type="bullet">
/// <item><description>Schema-scoped query execution</description></item>
/// <item><description>Cross-schema access prevention</description></item>
/// <item><description>ModuleSchemaRegistry validation</description></item>
/// <item><description>SchemaValidatingConnection interception</description></item>
/// </list>
/// </remarks>
public sealed class ModuleTestEntity : IEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the module name this entity belongs to.
    /// Used for validation tests without relying on schema metadata.
    /// </summary>
    public string ModuleName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the data payload for testing JSON/text serialization.
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the version for concurrency testing.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Test entity specifically for the Orders module.
/// Used to test schema isolation in the "orders" schema.
/// </summary>
public sealed class OrdersModuleEntity : IEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order total.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Test entity specifically for the Inventory module.
/// Used to test schema isolation in the "inventory" schema.
/// </summary>
public sealed class InventoryModuleEntity : IEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the SKU (Stock Keeping Unit).
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the quantity in stock.
    /// </summary>
    public int QuantityInStock { get; set; }

    /// <summary>
    /// Gets or sets the reorder threshold.
    /// </summary>
    public int ReorderThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Test entity for the shared/common schema.
/// Used to test access to shared lookup data across modules.
/// </summary>
public sealed class SharedLookupEntity : IEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the lookup code.
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the category this lookup belongs to.
    /// </summary>
    public string Category { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether this lookup is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }
}
