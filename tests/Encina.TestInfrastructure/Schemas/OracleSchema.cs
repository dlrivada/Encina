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
                    ExpiresAtUtc TIMESTAMP NOT NULL
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
                    CurrentStep VARCHAR2(200) NOT NULL,
                    Status VARCHAR2(50) NOT NULL,
                    Data CLOB NOT NULL,
                    StartedAtUtc TIMESTAMP NOT NULL,
                    LastUpdatedAtUtc TIMESTAMP NOT NULL,
                    CompletedAtUtc TIMESTAMP NULL,
                    ErrorMessage CLOB NULL,
                    CompensationData CLOB NULL
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
                    ProcessedAtUtc TIMESTAMP NULL,
                    ErrorMessage CLOB NULL,
                    RetryCount NUMBER(10) DEFAULT 0 NOT NULL,
                    NextRetryAtUtc TIMESTAMP NULL,
                    RecurrencePattern VARCHAR2(200) NULL
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
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(OracleConnection connection)
    {
        string[] tables = ["ScheduledMessages", "SagaStates", "InboxMessages", "OutboxMessages"];

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
        string[] tables = ["ScheduledMessages", "SagaStates", "InboxMessages", "OutboxMessages"];

        foreach (var table in tables)
        {
            var sql = $"DELETE FROM {table}";
            using var command = new OracleCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
