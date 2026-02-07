using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// Database schema creation utilities for multi-tenancy testing.
/// Creates tenant-aware tables across all supported database providers.
/// </summary>
public static class TenancySchema
{
    #region SQLite

    /// <summary>
    /// Creates the TenantTestEntities table for SQLite.
    /// </summary>
    public static async Task CreateTenantTestEntitiesSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS TenantTestEntities (
                Id TEXT PRIMARY KEY,
                TenantId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                Amount REAL NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_TenantTestEntities_TenantId
            ON TenantTestEntities(TenantId);

            CREATE INDEX IF NOT EXISTS IX_TenantTestEntities_TenantId_IsActive
            ON TenantTestEntities(TenantId, IsActive);

            CREATE INDEX IF NOT EXISTS IX_TenantTestEntities_CreatedAtUtc
            ON TenantTestEntities(CreatedAtUtc);
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table for SQLite.
    /// </summary>
    public static async Task CreateReadWriteTestEntitiesSchemaAsync(SqliteConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        const string sql = """
            CREATE TABLE IF NOT EXISTS ReadWriteTestEntities (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Value INTEGER NOT NULL,
                Timestamp TEXT NOT NULL,
                WriteCounter INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS IX_ReadWriteTestEntities_Timestamp
            ON ReadWriteTestEntities(Timestamp);
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all tenancy test tables for SQLite.
    /// </summary>
    public static async Task DropTenancyTablesAsync(SqliteConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS TenantTestEntities;
            DROP TABLE IF EXISTS ReadWriteTestEntities;
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears data from tenancy test tables for SQLite.
    /// </summary>
    public static async Task ClearTenancyDataAsync(SqliteConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        var tables = new[] { "TenantTestEntities", "ReadWriteTestEntities" };
        foreach (var table in tables)
        {
            try
            {
                using var command = new SqliteCommand($"DELETE FROM {table};", connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
            {
                // Table doesn't exist - skip
            }
        }
    }

    #endregion

    #region SQL Server

    /// <summary>
    /// Creates the TenantTestEntities table for SQL Server.
    /// </summary>
    public static async Task CreateTenantTestEntitiesSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID('TenantTestEntities', 'U') IS NULL
            BEGIN
                CREATE TABLE TenantTestEntities (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    TenantId NVARCHAR(128) NOT NULL,
                    Name NVARCHAR(256) NOT NULL,
                    Description NVARCHAR(1024) NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL
                );

                CREATE INDEX IX_TenantTestEntities_TenantId
                ON TenantTestEntities(TenantId);

                CREATE INDEX IX_TenantTestEntities_TenantId_IsActive
                ON TenantTestEntities(TenantId, IsActive);

                CREATE INDEX IX_TenantTestEntities_CreatedAtUtc
                ON TenantTestEntities(CreatedAtUtc);
            END
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table for SQL Server.
    /// </summary>
    public static async Task CreateReadWriteTestEntitiesSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID('ReadWriteTestEntities', 'U') IS NULL
            BEGIN
                CREATE TABLE ReadWriteTestEntities (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Name NVARCHAR(256) NOT NULL,
                    Value INT NOT NULL,
                    Timestamp DATETIME2 NOT NULL,
                    WriteCounter INT NOT NULL DEFAULT 0
                );

                CREATE INDEX IX_ReadWriteTestEntities_Timestamp
                ON ReadWriteTestEntities(Timestamp);
            END
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all tenancy test tables for SQL Server.
    /// </summary>
    public static async Task DropTenancyTablesAsync(SqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS TenantTestEntities;
            DROP TABLE IF EXISTS ReadWriteTestEntities;
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears data from tenancy test tables for SQL Server.
    /// </summary>
    public static async Task ClearTenancyDataAsync(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID('TenantTestEntities', 'U') IS NOT NULL DELETE FROM TenantTestEntities;
            IF OBJECT_ID('ReadWriteTestEntities', 'U') IS NOT NULL DELETE FROM ReadWriteTestEntities;
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region PostgreSQL

    /// <summary>
    /// Creates the TenantTestEntities table for PostgreSQL.
    /// </summary>
    /// <remarks>
    /// Uses lowercase identifiers to match PostgreSQL's default identifier folding behavior.
    /// </remarks>
    public static async Task CreateTenantTestEntitiesSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS tenanttestentities (
                id UUID PRIMARY KEY,
                tenantid VARCHAR(128) NOT NULL,
                name VARCHAR(256) NOT NULL,
                description VARCHAR(1024) NULL,
                amount DECIMAL(18,2) NOT NULL,
                isactive BOOLEAN NOT NULL DEFAULT TRUE,
                createdatutc TIMESTAMPTZ NOT NULL,
                updatedatutc TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS ix_tenanttestentities_tenantid
            ON tenanttestentities(tenantid);

            CREATE INDEX IF NOT EXISTS ix_tenanttestentities_tenantid_isactive
            ON tenanttestentities(tenantid, isactive);

            CREATE INDEX IF NOT EXISTS ix_tenanttestentities_createdatutc
            ON tenanttestentities(createdatutc);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table for PostgreSQL.
    /// </summary>
    /// <remarks>
    /// Uses lowercase identifiers to match PostgreSQL's default identifier folding behavior.
    /// </remarks>
    public static async Task CreateReadWriteTestEntitiesSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS readwritetestentities (
                id UUID PRIMARY KEY,
                name VARCHAR(256) NOT NULL,
                value INTEGER NOT NULL,
                timestamp TIMESTAMPTZ NOT NULL,
                writecounter INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS ix_readwritetestentities_timestamp
            ON readwritetestentities(timestamp);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all tenancy test tables for PostgreSQL.
    /// </summary>
    public static async Task DropTenancyTablesAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS tenanttestentities CASCADE;
            DROP TABLE IF EXISTS readwritetestentities CASCADE;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears data from tenancy test tables for PostgreSQL.
    /// </summary>
    public static async Task ClearTenancyDataAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM tenanttestentities;
            DELETE FROM readwritetestentities;
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
    /// Creates the TenantTestEntities table for MySQL.
    /// </summary>
    public static async Task CreateTenantTestEntitiesSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS TenantTestEntities (
                Id CHAR(36) PRIMARY KEY,
                TenantId VARCHAR(128) NOT NULL,
                Name VARCHAR(256) NOT NULL,
                Description VARCHAR(1024) NULL,
                Amount DECIMAL(18,2) NOT NULL,
                IsActive TINYINT(1) NOT NULL DEFAULT 1,
                CreatedAtUtc DATETIME(6) NOT NULL,
                UpdatedAtUtc DATETIME(6) NULL,
                INDEX IX_TenantTestEntities_TenantId (TenantId),
                INDEX IX_TenantTestEntities_TenantId_IsActive (TenantId, IsActive),
                INDEX IX_TenantTestEntities_CreatedAtUtc (CreatedAtUtc)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table for MySQL.
    /// </summary>
    public static async Task CreateReadWriteTestEntitiesSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS ReadWriteTestEntities (
                Id CHAR(36) PRIMARY KEY,
                Name VARCHAR(256) NOT NULL,
                Value INT NOT NULL,
                Timestamp DATETIME(6) NOT NULL,
                WriteCounter INT NOT NULL DEFAULT 0,
                INDEX IX_ReadWriteTestEntities_Timestamp (Timestamp)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all tenancy test tables for MySQL.
    /// </summary>
    public static async Task DropTenancyTablesAsync(MySqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS TenantTestEntities;
            DROP TABLE IF EXISTS ReadWriteTestEntities;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears data from tenancy test tables for MySQL.
    /// </summary>
    public static async Task ClearTenancyDataAsync(MySqlConnection connection)
    {
        const string sql = """
            DELETE FROM TenantTestEntities;
            DELETE FROM ReadWriteTestEntities;
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
