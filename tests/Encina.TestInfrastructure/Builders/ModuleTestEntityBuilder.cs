using Encina.TestInfrastructure.Entities;

namespace Encina.TestInfrastructure.Builders;

/// <summary>
/// Builder for creating <see cref="ModuleTestEntity"/> test data.
/// </summary>
public sealed class ModuleTestEntityBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _moduleName = "TestModule";
    private string _name = "Test Entity";
    private string? _data;
    private int _version = 1;
    private DateTime _createdAtUtc = DateTime.UtcNow;

    /// <summary>
    /// Sets the entity ID.
    /// </summary>
    public ModuleTestEntityBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the module name.
    /// </summary>
    public ModuleTestEntityBuilder WithModuleName(string moduleName)
    {
        _moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        return this;
    }

    /// <summary>
    /// Sets the entity name.
    /// </summary>
    public ModuleTestEntityBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the data payload.
    /// </summary>
    public ModuleTestEntityBuilder WithData(string? data)
    {
        _data = data;
        return this;
    }

    /// <summary>
    /// Sets the version number.
    /// </summary>
    public ModuleTestEntityBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    public ModuleTestEntityBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ModuleTestEntity"/> instance.
    /// </summary>
    public ModuleTestEntity Build() => new()
    {
        Id = _id,
        ModuleName = _moduleName,
        Name = _name,
        Data = _data,
        Version = _version,
        CreatedAtUtc = _createdAtUtc
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static ModuleTestEntityBuilder Create() => new();

    /// <summary>
    /// Creates a builder for the Orders module.
    /// </summary>
    public static ModuleTestEntityBuilder ForOrdersModule()
        => new ModuleTestEntityBuilder().WithModuleName("Orders");

    /// <summary>
    /// Creates a builder for the Inventory module.
    /// </summary>
    public static ModuleTestEntityBuilder ForInventoryModule()
        => new ModuleTestEntityBuilder().WithModuleName("Inventory");
}

/// <summary>
/// Builder for creating <see cref="OrdersModuleEntity"/> test data.
/// </summary>
public sealed class OrdersModuleEntityBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
    private string _customerName = "Test Customer";
    private decimal _total;
    private string _status = "Pending";
    private DateTime _createdAtUtc = DateTime.UtcNow;

    /// <summary>
    /// Sets the entity ID.
    /// </summary>
    public OrdersModuleEntityBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the order number.
    /// </summary>
    public OrdersModuleEntityBuilder WithOrderNumber(string orderNumber)
    {
        _orderNumber = orderNumber ?? throw new ArgumentNullException(nameof(orderNumber));
        return this;
    }

    /// <summary>
    /// Sets the customer name.
    /// </summary>
    public OrdersModuleEntityBuilder WithCustomerName(string customerName)
    {
        _customerName = customerName ?? throw new ArgumentNullException(nameof(customerName));
        return this;
    }

    /// <summary>
    /// Sets the order total.
    /// </summary>
    public OrdersModuleEntityBuilder WithTotal(decimal total)
    {
        _total = total;
        return this;
    }

    /// <summary>
    /// Sets the order status.
    /// </summary>
    public OrdersModuleEntityBuilder WithStatus(string status)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    public OrdersModuleEntityBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="OrdersModuleEntity"/> instance.
    /// </summary>
    public OrdersModuleEntity Build() => new()
    {
        Id = _id,
        OrderNumber = _orderNumber,
        CustomerName = _customerName,
        Total = _total,
        Status = _status,
        CreatedAtUtc = _createdAtUtc
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static OrdersModuleEntityBuilder Create() => new();
}

/// <summary>
/// Builder for creating <see cref="InventoryModuleEntity"/> test data.
/// </summary>
public sealed class InventoryModuleEntityBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _sku = $"SKU-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    private string _productName = "Test Product";
    private int _quantityInStock;
    private int _reorderThreshold = 10;
    private DateTime _lastUpdatedAtUtc = DateTime.UtcNow;

    /// <summary>
    /// Sets the entity ID.
    /// </summary>
    public InventoryModuleEntityBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the SKU.
    /// </summary>
    public InventoryModuleEntityBuilder WithSku(string sku)
    {
        _sku = sku ?? throw new ArgumentNullException(nameof(sku));
        return this;
    }

    /// <summary>
    /// Sets the product name.
    /// </summary>
    public InventoryModuleEntityBuilder WithProductName(string productName)
    {
        _productName = productName ?? throw new ArgumentNullException(nameof(productName));
        return this;
    }

    /// <summary>
    /// Sets the quantity in stock.
    /// </summary>
    public InventoryModuleEntityBuilder WithQuantityInStock(int quantityInStock)
    {
        _quantityInStock = quantityInStock;
        return this;
    }

    /// <summary>
    /// Sets the reorder threshold.
    /// </summary>
    public InventoryModuleEntityBuilder WithReorderThreshold(int reorderThreshold)
    {
        _reorderThreshold = reorderThreshold;
        return this;
    }

    /// <summary>
    /// Sets the last updated timestamp.
    /// </summary>
    public InventoryModuleEntityBuilder WithLastUpdatedAtUtc(DateTime lastUpdatedAtUtc)
    {
        _lastUpdatedAtUtc = lastUpdatedAtUtc;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="InventoryModuleEntity"/> instance.
    /// </summary>
    public InventoryModuleEntity Build() => new()
    {
        Id = _id,
        Sku = _sku,
        ProductName = _productName,
        QuantityInStock = _quantityInStock,
        ReorderThreshold = _reorderThreshold,
        LastUpdatedAtUtc = _lastUpdatedAtUtc
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static InventoryModuleEntityBuilder Create() => new();
}

/// <summary>
/// Builder for creating <see cref="SharedLookupEntity"/> test data.
/// </summary>
public sealed class SharedLookupEntityBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _code = "CODE";
    private string _displayName = "Display Name";
    private string _category = "General";
    private bool _isActive = true;
    private int _sortOrder;

    /// <summary>
    /// Sets the entity ID.
    /// </summary>
    public SharedLookupEntityBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the lookup code.
    /// </summary>
    public SharedLookupEntityBuilder WithCode(string code)
    {
        _code = code ?? throw new ArgumentNullException(nameof(code));
        return this;
    }

    /// <summary>
    /// Sets the display name.
    /// </summary>
    public SharedLookupEntityBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        return this;
    }

    /// <summary>
    /// Sets the category.
    /// </summary>
    public SharedLookupEntityBuilder WithCategory(string category)
    {
        _category = category ?? throw new ArgumentNullException(nameof(category));
        return this;
    }

    /// <summary>
    /// Sets the active status.
    /// </summary>
    public SharedLookupEntityBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    /// <summary>
    /// Sets the sort order.
    /// </summary>
    public SharedLookupEntityBuilder WithSortOrder(int sortOrder)
    {
        _sortOrder = sortOrder;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="SharedLookupEntity"/> instance.
    /// </summary>
    public SharedLookupEntity Build() => new()
    {
        Id = _id,
        Code = _code,
        DisplayName = _displayName,
        Category = _category,
        IsActive = _isActive,
        SortOrder = _sortOrder
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static SharedLookupEntityBuilder Create() => new();
}
