using Npgsql;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// PostgreSQL schema creation for Encina test databases.
/// </summary>
/// <remarks>
/// All table and column names use lowercase without quotes to match PostgreSQL's default
/// identifier folding behavior. This ensures queries using unquoted identifiers work correctly.
/// </remarks>
public static class PostgreSqlSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS outboxmessages (
                id UUID PRIMARY KEY,
                notificationtype VARCHAR(500) NOT NULL,
                content TEXT NOT NULL,
                createdatutc TIMESTAMP NOT NULL,
                processedatutc TIMESTAMP NULL,
                errormessage TEXT NULL,
                retrycount INTEGER NOT NULL DEFAULT 0,
                nextretryatutc TIMESTAMP NULL
            );

            CREATE INDEX IF NOT EXISTS ix_outboxmessages_processedatutc_nextretryatutc
            ON outboxmessages(processedatutc, nextretryatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS inboxmessages (
                messageid VARCHAR(256) PRIMARY KEY,
                requesttype VARCHAR(500) NOT NULL,
                receivedatutc TIMESTAMP NOT NULL,
                processedatutc TIMESTAMP NULL,
                response TEXT NULL,
                errormessage TEXT NULL,
                retrycount INTEGER NOT NULL DEFAULT 0,
                nextretryatutc TIMESTAMP NULL,
                expiresatutc TIMESTAMP NOT NULL,
                metadata TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_inboxmessages_expiresatutc
            ON inboxmessages(expiresatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS sagastates (
                sagaid UUID PRIMARY KEY,
                sagatype VARCHAR(500) NOT NULL,
                currentstep INTEGER NOT NULL,
                status VARCHAR(50) NOT NULL,
                data TEXT NOT NULL,
                startedatutc TIMESTAMP NOT NULL,
                lastupdatedatutc TIMESTAMP NOT NULL,
                completedatutc TIMESTAMP NULL,
                errormessage TEXT NULL,
                timeoutatutc TIMESTAMP NULL,
                correlationid VARCHAR(256) NULL,
                metadata TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_sagastates_status_lastupdatedatutc
            ON sagastates(status, lastupdatedatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS scheduledmessages (
                id UUID PRIMARY KEY,
                requesttype VARCHAR(500) NOT NULL,
                content TEXT NOT NULL,
                scheduledatutc TIMESTAMP NOT NULL,
                createdatutc TIMESTAMP NOT NULL,
                processedatutc TIMESTAMP NULL,
                lastexecutedatutc TIMESTAMP NULL,
                errormessage TEXT NULL,
                retrycount INTEGER NOT NULL DEFAULT 0,
                nextretryatutc TIMESTAMP NULL,
                correlationid VARCHAR(256) NULL,
                metadata TEXT NULL,
                isrecurring BOOLEAN NOT NULL DEFAULT FALSE,
                cronexpression VARCHAR(200) NULL
            );

            CREATE INDEX IF NOT EXISTS ix_scheduledmessages_scheduledatutc_processedatutc
            ON scheduledmessages(scheduledatutc, processedatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Orders table schema for immutable update integration tests.
    /// </summary>
    public static async Task CreateOrdersSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS orders (
                id UUID PRIMARY KEY,
                customername VARCHAR(200) NOT NULL,
                status VARCHAR(50) NOT NULL
            );
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the TestRepositoryEntities table schema for repository integration tests.
    /// </summary>
    public static async Task CreateTestRepositorySchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS testrepositoryentities (
                id UUID PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                amount DECIMAL(18,2) NOT NULL,
                isactive BOOLEAN NOT NULL,
                createdatutc TIMESTAMP NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_testrepositoryentities_isactive
            ON testrepositoryentities(isactive);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the TenantTestEntities table schema for multi-tenancy integration tests.
    /// </summary>
    public static async Task CreateTenantTestSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS tenanttestentities (
                id UUID PRIMARY KEY,
                tenantid VARCHAR(128) NOT NULL,
                name VARCHAR(200) NOT NULL,
                description VARCHAR(1000) NULL,
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

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table schema for read/write separation tests.
    /// </summary>
    public static async Task CreateReadWriteTestSchemaAsync(NpgsqlConnection connection)
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

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS tenanttestentities CASCADE;
            DROP TABLE IF EXISTS readwritetestentities CASCADE;
            DROP TABLE IF EXISTS orders CASCADE;
            DROP TABLE IF EXISTS testrepositoryentities CASCADE;
            DROP TABLE IF EXISTS scheduledmessages CASCADE;
            DROP TABLE IF EXISTS sagastates CASCADE;
            DROP TABLE IF EXISTS inboxmessages CASCADE;
            DROP TABLE IF EXISTS outboxmessages CASCADE;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears all data from Encina tables without dropping schemas.
    /// Useful for cleaning between tests that share a database fixture.
    /// </summary>
    public static async Task ClearAllDataAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM tenanttestentities;
            DELETE FROM readwritetestentities;
            DELETE FROM orders;
            DELETE FROM testrepositoryentities;
            DELETE FROM scheduledmessages;
            DELETE FROM sagastates;
            DELETE FROM inboxmessages;
            DELETE FROM outboxmessages;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
