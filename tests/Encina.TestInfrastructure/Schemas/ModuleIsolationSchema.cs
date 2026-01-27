using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// Database schema creation utilities for module isolation testing.
/// Creates module-specific schemas and tables for testing schema boundary enforcement.
/// </summary>
/// <remarks>
/// SQLite does not support schemas natively, so module isolation tests should be
/// skipped or adapted for SQLite. For SQLite, we use table name prefixes as an approximation.
/// </remarks>
public static class ModuleIsolationSchema
{
    /// <summary>
    /// Standard schema names used in module isolation tests.
    /// </summary>
    public static class SchemaNames
    {
        /// <summary>
        /// The Orders module schema.
        /// </summary>
        public const string Orders = "orders";

        /// <summary>
        /// The Inventory module schema.
        /// </summary>
        public const string Inventory = "inventory";

        /// <summary>
        /// The shared schema for cross-module lookup data.
        /// </summary>
        public const string Shared = "shared";
    }

    #region SQL Server

    /// <summary>
    /// Creates all module isolation schemas and tables for SQL Server.
    /// </summary>
    public static async Task CreateAllModuleSchemasAsync(SqlConnection connection)
    {
        await CreateSchemaIfNotExistsAsync(connection, SchemaNames.Orders);
        await CreateSchemaIfNotExistsAsync(connection, SchemaNames.Inventory);
        await CreateSchemaIfNotExistsAsync(connection, SchemaNames.Shared);

        await CreateOrdersSchemaTablesAsync(connection);
        await CreateInventorySchemaTablesAsync(connection);
        await CreateSharedSchemaTablesAsync(connection);
    }

    private static async Task CreateSchemaIfNotExistsAsync(SqlConnection connection, string schemaName)
    {
        var sql = $"""
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schemaName}')
            BEGIN
                EXEC('CREATE SCHEMA [{schemaName}]')
            END
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Orders module schema tables for SQL Server.
    /// </summary>
    public static async Task CreateOrdersSchemaTablesAsync(SqlConnection connection)
    {
        var sql = $"""
            IF OBJECT_ID('{SchemaNames.Orders}.Orders', 'U') IS NULL
            BEGIN
                CREATE TABLE [{SchemaNames.Orders}].[Orders] (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    OrderNumber NVARCHAR(50) NOT NULL,
                    CustomerName NVARCHAR(256) NOT NULL,
                    Total DECIMAL(18,2) NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    CreatedAtUtc DATETIME2 NOT NULL,
                    CONSTRAINT UQ_Orders_OrderNumber UNIQUE (OrderNumber)
                );

                CREATE INDEX IX_Orders_Status ON [{SchemaNames.Orders}].[Orders](Status);
                CREATE INDEX IX_Orders_CreatedAtUtc ON [{SchemaNames.Orders}].[Orders](CreatedAtUtc);
            END
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inventory module schema tables for SQL Server.
    /// </summary>
    public static async Task CreateInventorySchemaTablesAsync(SqlConnection connection)
    {
        var sql = $"""
            IF OBJECT_ID('{SchemaNames.Inventory}.InventoryItems', 'U') IS NULL
            BEGIN
                CREATE TABLE [{SchemaNames.Inventory}].[InventoryItems] (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Sku NVARCHAR(50) NOT NULL,
                    ProductName NVARCHAR(256) NOT NULL,
                    QuantityInStock INT NOT NULL DEFAULT 0,
                    ReorderThreshold INT NOT NULL DEFAULT 10,
                    LastUpdatedAtUtc DATETIME2 NOT NULL,
                    CONSTRAINT UQ_InventoryItems_Sku UNIQUE (Sku)
                );

                CREATE INDEX IX_InventoryItems_QuantityInStock ON [{SchemaNames.Inventory}].[InventoryItems](QuantityInStock);
            END
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Shared schema tables for SQL Server.
    /// </summary>
    public static async Task CreateSharedSchemaTablesAsync(SqlConnection connection)
    {
        var sql = $"""
            IF OBJECT_ID('{SchemaNames.Shared}.Lookups', 'U') IS NULL
            BEGIN
                CREATE TABLE [{SchemaNames.Shared}].[Lookups] (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Code NVARCHAR(50) NOT NULL,
                    DisplayName NVARCHAR(256) NOT NULL,
                    Category NVARCHAR(100) NOT NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    SortOrder INT NOT NULL DEFAULT 0,
                    CONSTRAINT UQ_Lookups_Category_Code UNIQUE (Category, Code)
                );

                CREATE INDEX IX_Lookups_IsActive ON [{SchemaNames.Shared}].[Lookups](IsActive);
            END
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all module isolation schemas and tables for SQL Server.
    /// </summary>
    public static async Task DropAllModuleSchemasAsync(SqlConnection connection)
    {
        var sql = $"""
            -- Drop tables first
            DROP TABLE IF EXISTS [{SchemaNames.Orders}].[Orders];
            DROP TABLE IF EXISTS [{SchemaNames.Inventory}].[InventoryItems];
            DROP TABLE IF EXISTS [{SchemaNames.Shared}].[Lookups];

            -- Then drop schemas (only if empty)
            IF EXISTS (SELECT * FROM sys.schemas WHERE name = '{SchemaNames.Orders}')
                AND NOT EXISTS (SELECT * FROM sys.objects WHERE schema_id = SCHEMA_ID('{SchemaNames.Orders}'))
                DROP SCHEMA [{SchemaNames.Orders}];

            IF EXISTS (SELECT * FROM sys.schemas WHERE name = '{SchemaNames.Inventory}')
                AND NOT EXISTS (SELECT * FROM sys.objects WHERE schema_id = SCHEMA_ID('{SchemaNames.Inventory}'))
                DROP SCHEMA [{SchemaNames.Inventory}];

            IF EXISTS (SELECT * FROM sys.schemas WHERE name = '{SchemaNames.Shared}')
                AND NOT EXISTS (SELECT * FROM sys.objects WHERE schema_id = SCHEMA_ID('{SchemaNames.Shared}'))
                DROP SCHEMA [{SchemaNames.Shared}];
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears data from all module isolation tables for SQL Server.
    /// </summary>
    public static async Task ClearModuleIsolationDataAsync(SqlConnection connection)
    {
        var sql = $"""
            IF OBJECT_ID('{SchemaNames.Orders}.Orders', 'U') IS NOT NULL DELETE FROM [{SchemaNames.Orders}].[Orders];
            IF OBJECT_ID('{SchemaNames.Inventory}.InventoryItems', 'U') IS NOT NULL DELETE FROM [{SchemaNames.Inventory}].[InventoryItems];
            IF OBJECT_ID('{SchemaNames.Shared}.Lookups', 'U') IS NOT NULL DELETE FROM [{SchemaNames.Shared}].[Lookups];
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region PostgreSQL

    /// <summary>
    /// Creates all module isolation schemas and tables for PostgreSQL.
    /// </summary>
    public static async Task CreateAllModuleSchemasAsync(NpgsqlConnection connection)
    {
        // Create schemas
        await CreatePostgreSqlSchemaIfNotExistsAsync(connection, SchemaNames.Orders);
        await CreatePostgreSqlSchemaIfNotExistsAsync(connection, SchemaNames.Inventory);
        await CreatePostgreSqlSchemaIfNotExistsAsync(connection, SchemaNames.Shared);

        // Create tables
        await CreateOrdersSchemaTablesAsync(connection);
        await CreateInventorySchemaTablesAsync(connection);
        await CreateSharedSchemaTablesAsync(connection);
    }

    private static async Task CreatePostgreSqlSchemaIfNotExistsAsync(NpgsqlConnection connection, string schemaName)
    {
        var sql = $"CREATE SCHEMA IF NOT EXISTS {schemaName}";

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Orders module schema tables for PostgreSQL.
    /// </summary>
    public static async Task CreateOrdersSchemaTablesAsync(NpgsqlConnection connection)
    {
        var sql = $"""
            CREATE TABLE IF NOT EXISTS {SchemaNames.Orders}.Orders (
                Id UUID PRIMARY KEY,
                OrderNumber VARCHAR(50) NOT NULL UNIQUE,
                CustomerName VARCHAR(256) NOT NULL,
                Total DECIMAL(18,2) NOT NULL,
                Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                CreatedAtUtc TIMESTAMP NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Orders_Status ON {SchemaNames.Orders}.Orders(Status);
            CREATE INDEX IF NOT EXISTS IX_Orders_CreatedAtUtc ON {SchemaNames.Orders}.Orders(CreatedAtUtc);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inventory module schema tables for PostgreSQL.
    /// </summary>
    public static async Task CreateInventorySchemaTablesAsync(NpgsqlConnection connection)
    {
        var sql = $"""
            CREATE TABLE IF NOT EXISTS {SchemaNames.Inventory}.InventoryItems (
                Id UUID PRIMARY KEY,
                Sku VARCHAR(50) NOT NULL UNIQUE,
                ProductName VARCHAR(256) NOT NULL,
                QuantityInStock INT NOT NULL DEFAULT 0,
                ReorderThreshold INT NOT NULL DEFAULT 10,
                LastUpdatedAtUtc TIMESTAMP NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_InventoryItems_QuantityInStock ON {SchemaNames.Inventory}.InventoryItems(QuantityInStock);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Shared schema tables for PostgreSQL.
    /// </summary>
    public static async Task CreateSharedSchemaTablesAsync(NpgsqlConnection connection)
    {
        var sql = $"""
            CREATE TABLE IF NOT EXISTS {SchemaNames.Shared}.Lookups (
                Id UUID PRIMARY KEY,
                Code VARCHAR(50) NOT NULL,
                DisplayName VARCHAR(256) NOT NULL,
                Category VARCHAR(100) NOT NULL,
                IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                SortOrder INT NOT NULL DEFAULT 0,
                UNIQUE (Category, Code)
            );

            CREATE INDEX IF NOT EXISTS IX_Lookups_IsActive ON {SchemaNames.Shared}.Lookups(IsActive);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all module isolation schemas and tables for PostgreSQL.
    /// </summary>
    public static async Task DropAllModuleSchemasAsync(NpgsqlConnection connection)
    {
        var sql = $"""
            DROP SCHEMA IF EXISTS {SchemaNames.Orders} CASCADE;
            DROP SCHEMA IF EXISTS {SchemaNames.Inventory} CASCADE;
            DROP SCHEMA IF EXISTS {SchemaNames.Shared} CASCADE;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears data from all module isolation tables for PostgreSQL.
    /// </summary>
    public static async Task ClearModuleIsolationDataAsync(NpgsqlConnection connection)
    {
        var sql = $"""
            DELETE FROM {SchemaNames.Orders}.Orders;
            DELETE FROM {SchemaNames.Inventory}.InventoryItems;
            DELETE FROM {SchemaNames.Shared}.Lookups;
            """;

        try
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01") // undefined_table
        {
            // Tables don't exist - skip
        }
    }

    #endregion

    #region MySQL

    /// <summary>
    /// Creates all module isolation schemas (databases) and tables for MySQL.
    /// </summary>
    /// <remarks>
    /// MySQL uses separate databases instead of schemas within a database.
    /// For module isolation testing, we create prefixed tables within the same database.
    /// </remarks>
    public static async Task CreateAllModuleSchemasAsync(MySqlConnection connection)
    {
        await CreateOrdersSchemaTablesAsync(connection);
        await CreateInventorySchemaTablesAsync(connection);
        await CreateSharedSchemaTablesAsync(connection);
    }

    /// <summary>
    /// Creates the Orders module schema tables for MySQL.
    /// Uses table prefix since MySQL doesn't support schemas within a database.
    /// </summary>
    public static async Task CreateOrdersSchemaTablesAsync(MySqlConnection connection)
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS orders_Orders (
                Id CHAR(36) PRIMARY KEY,
                OrderNumber VARCHAR(50) NOT NULL UNIQUE,
                CustomerName VARCHAR(256) NOT NULL,
                Total DECIMAL(18,2) NOT NULL,
                Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                CreatedAtUtc DATETIME(6) NOT NULL,
                INDEX IX_orders_Orders_Status (Status),
                INDEX IX_orders_Orders_CreatedAtUtc (CreatedAtUtc)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inventory module schema tables for MySQL.
    /// </summary>
    public static async Task CreateInventorySchemaTablesAsync(MySqlConnection connection)
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS inventory_InventoryItems (
                Id CHAR(36) PRIMARY KEY,
                Sku VARCHAR(50) NOT NULL UNIQUE,
                ProductName VARCHAR(256) NOT NULL,
                QuantityInStock INT NOT NULL DEFAULT 0,
                ReorderThreshold INT NOT NULL DEFAULT 10,
                LastUpdatedAtUtc DATETIME(6) NOT NULL,
                INDEX IX_inventory_InventoryItems_QuantityInStock (QuantityInStock)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Shared schema tables for MySQL.
    /// </summary>
    public static async Task CreateSharedSchemaTablesAsync(MySqlConnection connection)
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS shared_Lookups (
                Id CHAR(36) PRIMARY KEY,
                Code VARCHAR(50) NOT NULL,
                DisplayName VARCHAR(256) NOT NULL,
                Category VARCHAR(100) NOT NULL,
                IsActive TINYINT(1) NOT NULL DEFAULT 1,
                SortOrder INT NOT NULL DEFAULT 0,
                UNIQUE KEY UQ_shared_Lookups_Category_Code (Category, Code),
                INDEX IX_shared_Lookups_IsActive (IsActive)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all module isolation tables for MySQL.
    /// </summary>
    public static async Task DropAllModuleSchemasAsync(MySqlConnection connection)
    {
        var sql = """
            DROP TABLE IF EXISTS orders_Orders;
            DROP TABLE IF EXISTS inventory_InventoryItems;
            DROP TABLE IF EXISTS shared_Lookups;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears data from all module isolation tables for MySQL.
    /// </summary>
    public static async Task ClearModuleIsolationDataAsync(MySqlConnection connection)
    {
        var sql = """
            DELETE FROM orders_Orders;
            DELETE FROM inventory_InventoryItems;
            DELETE FROM shared_Lookups;
            """;

        try
        {
            await using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex) when (ex.Number == 1146) // Table doesn't exist
        {
            // Tables don't exist - skip
        }
    }

    #endregion
}
