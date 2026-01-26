using Oracle.ManagedDataAccess.Client;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// Oracle schema creation for Encina test databases.
/// </summary>
public static class OracleSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(OracleConnection connection)
    {
        const string createTable = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE TABLE OutboxMessages (
                    Id RAW(16) PRIMARY KEY,
                    NotificationType VARCHAR2(500) NOT NULL,
                    Content CLOB NOT NULL,
                    CreatedAtUtc TIMESTAMP NOT NULL,
                    ProcessedAtUtc TIMESTAMP NULL,
                    ErrorMessage CLOB NULL,
                    RetryCount NUMBER(10) DEFAULT 0 NOT NULL,
                    NextRetryAtUtc TIMESTAMP NULL
                )';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        const string createIndex = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE INDEX IX_OutboxMessages_Processed
                ON OutboxMessages(ProcessedAtUtc, NextRetryAtUtc)';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        using (var command = new OracleCommand(createTable, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        using (var command = new OracleCommand(createIndex, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(OracleConnection connection)
    {
        const string createTable = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE TABLE InboxMessages (
                    MessageId VARCHAR2(256) PRIMARY KEY,
                    RequestType VARCHAR2(500) NOT NULL,
                    ReceivedAtUtc TIMESTAMP NOT NULL,
                    ProcessedAtUtc TIMESTAMP NULL,
                    Response CLOB NULL,
                    ErrorMessage CLOB NULL,
                    RetryCount NUMBER(10) DEFAULT 0 NOT NULL,
                    NextRetryAtUtc TIMESTAMP NULL,
                    ExpiresAtUtc TIMESTAMP NOT NULL,
                    Metadata CLOB NULL
                )';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        const string createIndex = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE INDEX IX_InboxMessages_ExpiresAtUtc
                ON InboxMessages(ExpiresAtUtc)';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        using (var command = new OracleCommand(createTable, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        using (var command = new OracleCommand(createIndex, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(OracleConnection connection)
    {
        const string createTable = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE TABLE SagaStates (
                    SagaId RAW(16) PRIMARY KEY,
                    SagaType VARCHAR2(500) NOT NULL,
                    CurrentStep NUMBER(10) NOT NULL,
                    Status VARCHAR2(50) NOT NULL,
                    Data CLOB NOT NULL,
                    StartedAtUtc TIMESTAMP NOT NULL,
                    LastUpdatedAtUtc TIMESTAMP NOT NULL,
                    CompletedAtUtc TIMESTAMP NULL,
                    ErrorMessage CLOB NULL,
                    CorrelationId VARCHAR2(256) NULL,
                    TimeoutAtUtc TIMESTAMP NULL,
                    Metadata CLOB NULL
                )';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        const string createIndex = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE INDEX IX_SagaStates_Status
                ON SagaStates(Status, LastUpdatedAtUtc)';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        using (var command = new OracleCommand(createTable, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        using (var command = new OracleCommand(createIndex, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(OracleConnection connection)
    {
        const string createTable = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE TABLE ScheduledMessages (
                    Id RAW(16) PRIMARY KEY,
                    RequestType VARCHAR2(500) NOT NULL,
                    Content CLOB NOT NULL,
                    ScheduledAtUtc TIMESTAMP NOT NULL,
                    CreatedAtUtc TIMESTAMP NOT NULL,
                    ProcessedAtUtc TIMESTAMP NULL,
                    ErrorMessage CLOB NULL,
                    RetryCount NUMBER(10) DEFAULT 0 NOT NULL,
                    NextRetryAtUtc TIMESTAMP NULL,
                    CorrelationId VARCHAR2(256) NULL,
                    Metadata CLOB NULL,
                    IsRecurring NUMBER(1) DEFAULT 0 NOT NULL,
                    CronExpression VARCHAR2(200) NULL,
                    LastExecutedAtUtc TIMESTAMP NULL
                )';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        const string createIndex = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE INDEX IX_ScheduledMessages_Scheduled
                ON ScheduledMessages(ScheduledAtUtc, ProcessedAtUtc)';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        using (var command = new OracleCommand(createTable, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        using (var command = new OracleCommand(createIndex, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Creates the TestEntities table schema for repository integration tests.
    /// </summary>
    public static async Task CreateRepositoryTestSchemaAsync(OracleConnection connection)
    {
        const string createTable = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE TABLE TestEntities (
                    Id RAW(16) PRIMARY KEY,
                    Name VARCHAR2(200) NOT NULL,
                    Amount NUMBER(18,2) NOT NULL,
                    IsActive NUMBER(1) NOT NULL,
                    CreatedAtUtc TIMESTAMP NOT NULL
                )';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        const string createIndex = """
            BEGIN
                EXECUTE IMMEDIATE 'CREATE INDEX IX_TestEntities_IsActive
                ON TestEntities(IsActive)';
            EXCEPTION
                WHEN OTHERS THEN
                    IF SQLCODE != -955 THEN
                        RAISE;
                    END IF;
            END;
            """;

        using (var command = new OracleCommand(createTable, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        using (var command = new OracleCommand(createIndex, connection))
        {
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(OracleConnection connection)
    {
        string[] tables = ["TestEntities", "ScheduledMessages", "SagaStates", "InboxMessages", "OutboxMessages"];

        foreach (var table in tables)
        {
            var sql = $"""
                BEGIN
                    EXECUTE IMMEDIATE 'DROP TABLE {table} CASCADE CONSTRAINTS';
                EXCEPTION
                    WHEN OTHERS THEN
                        IF SQLCODE != -942 THEN
                            RAISE;
                        END IF;
                END;
                """;

            using var command = new OracleCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Clears all data from Encina tables without dropping schemas.
    /// Useful for cleaning between tests that share a database fixture.
    /// </summary>
    public static async Task ClearAllDataAsync(OracleConnection connection)
    {
        string[] tables = ["TestEntities", "ScheduledMessages", "SagaStates", "InboxMessages", "OutboxMessages"];

        foreach (var table in tables)
        {
            // Use EXECUTE IMMEDIATE to ignore errors if table doesn't exist
            var sql = $"""
                BEGIN
                    EXECUTE IMMEDIATE 'DELETE FROM {table}';
                EXCEPTION
                    WHEN OTHERS THEN
                        IF SQLCODE != -942 THEN
                            RAISE;
                        END IF;
                END;
                """;
            using var command = new OracleCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
