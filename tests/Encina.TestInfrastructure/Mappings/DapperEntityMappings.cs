namespace Encina.TestInfrastructure.Mappings;

/// <summary>
/// Provides SQL column mappings for Dapper and ADO.NET operations.
/// Contains provider-specific SQL for each supported database.
/// </summary>
public static class DapperEntityMappings
{
    /// <summary>
    /// Gets the TenantTestEntity column mappings.
    /// </summary>
    public static class TenantTestEntity
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public const string TableName = "TenantTestEntities";

        /// <summary>
        /// Gets all columns for SELECT operations.
        /// </summary>
        public const string SelectColumns = """
            Id, TenantId, Name, Description, Amount, IsActive, CreatedAtUtc, UpdatedAtUtc
            """;

        /// <summary>
        /// Gets columns for INSERT operations (excludes auto-generated).
        /// </summary>
        public const string InsertColumns = """
            Id, TenantId, Name, Description, Amount, IsActive, CreatedAtUtc, UpdatedAtUtc
            """;

        /// <summary>
        /// Gets the INSERT parameter placeholders (standard @param format for all providers).
        /// </summary>
        public const string InsertValues = """
            @Id, @TenantId, @Name, @Description, @Amount, @IsActive, @CreatedAtUtc, @UpdatedAtUtc
            """;
    }

    /// <summary>
    /// Gets the ModuleTestEntity column mappings.
    /// </summary>
    public static class ModuleTestEntity
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public const string TableName = "ModuleTestEntities";

        /// <summary>
        /// Gets all columns for SELECT operations.
        /// </summary>
        public const string SelectColumns = """
            Id, ModuleName, Name, Data, Version, CreatedAtUtc
            """;

        /// <summary>
        /// Gets columns for INSERT operations.
        /// </summary>
        public const string InsertColumns = """
            Id, ModuleName, Name, Data, Version, CreatedAtUtc
            """;

        /// <summary>
        /// Gets the INSERT parameter placeholders.
        /// </summary>
        public const string InsertValues = """
            @Id, @ModuleName, @Name, @Data, @Version, @CreatedAtUtc
            """;
    }

    /// <summary>
    /// Gets the OrdersModuleEntity column mappings (for "orders" schema).
    /// </summary>
    public static class OrdersModuleEntity
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public const string TableName = "Orders";

        /// <summary>
        /// Gets the default schema.
        /// </summary>
        public const string DefaultSchema = "orders";

        /// <summary>
        /// Gets all columns for SELECT operations.
        /// </summary>
        public const string SelectColumns = """
            Id, OrderNumber, CustomerName, Total, Status, CreatedAtUtc
            """;

        /// <summary>
        /// Gets columns for INSERT operations.
        /// </summary>
        public const string InsertColumns = """
            Id, OrderNumber, CustomerName, Total, Status, CreatedAtUtc
            """;

        /// <summary>
        /// Gets the INSERT parameter placeholders.
        /// </summary>
        public const string InsertValues = """
            @Id, @OrderNumber, @CustomerName, @Total, @Status, @CreatedAtUtc
            """;
    }

    /// <summary>
    /// Gets the InventoryModuleEntity column mappings (for "inventory" schema).
    /// </summary>
    public static class InventoryModuleEntity
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public const string TableName = "InventoryItems";

        /// <summary>
        /// Gets the default schema.
        /// </summary>
        public const string DefaultSchema = "inventory";

        /// <summary>
        /// Gets all columns for SELECT operations.
        /// </summary>
        public const string SelectColumns = """
            Id, Sku, ProductName, QuantityInStock, ReorderThreshold, LastUpdatedAtUtc
            """;

        /// <summary>
        /// Gets columns for INSERT operations.
        /// </summary>
        public const string InsertColumns = """
            Id, Sku, ProductName, QuantityInStock, ReorderThreshold, LastUpdatedAtUtc
            """;

        /// <summary>
        /// Gets the INSERT parameter placeholders.
        /// </summary>
        public const string InsertValues = """
            @Id, @Sku, @ProductName, @QuantityInStock, @ReorderThreshold, @LastUpdatedAtUtc
            """;
    }

    /// <summary>
    /// Gets the SharedLookupEntity column mappings (for "shared" schema).
    /// </summary>
    public static class SharedLookupEntity
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public const string TableName = "Lookups";

        /// <summary>
        /// Gets the default schema.
        /// </summary>
        public const string DefaultSchema = "shared";

        /// <summary>
        /// Gets all columns for SELECT operations.
        /// </summary>
        public const string SelectColumns = """
            Id, Code, DisplayName, Category, IsActive, SortOrder
            """;

        /// <summary>
        /// Gets columns for INSERT operations.
        /// </summary>
        public const string InsertColumns = """
            Id, Code, DisplayName, Category, IsActive, SortOrder
            """;

        /// <summary>
        /// Gets the INSERT parameter placeholders.
        /// </summary>
        public const string InsertValues = """
            @Id, @Code, @DisplayName, @Category, @IsActive, @SortOrder
            """;
    }

    /// <summary>
    /// Gets the ReadWriteTestEntity column mappings.
    /// </summary>
    public static class ReadWriteTestEntity
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public const string TableName = "ReadWriteTestEntities";

        /// <summary>
        /// Gets all columns for SELECT operations.
        /// </summary>
        public const string SelectColumns = """
            Id, Name, Value, Timestamp, WriteCounter
            """;

        /// <summary>
        /// Gets columns for INSERT operations.
        /// </summary>
        public const string InsertColumns = """
            Id, Name, Value, Timestamp, WriteCounter
            """;

        /// <summary>
        /// Gets the INSERT parameter placeholders.
        /// </summary>
        public const string InsertValues = """
            @Id, @Name, @Value, @Timestamp, @WriteCounter
            """;
    }
}
