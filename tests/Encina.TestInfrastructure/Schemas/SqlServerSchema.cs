using Microsoft.Data.SqlClient;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// SQL Server schema creation for Encina test databases.
/// </summary>
public static class SqlServerSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID('OutboxMessages', 'U') IS NULL
            BEGIN
                CREATE TABLE OutboxMessages (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    NotificationType NVARCHAR(500) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    ProcessedAtUtc DATETIME2 NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    RetryCount INT NOT NULL DEFAULT 0,
                    NextRetryAtUtc DATETIME2 NULL
                );

                CREATE INDEX IX_OutboxMessages_ProcessedAtUtc_NextRetryAtUtc
                ON OutboxMessages(ProcessedAtUtc, NextRetryAtUtc);
            END
            """;

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID('InboxMessages', 'U') IS NULL
            BEGIN
                CREATE TABLE InboxMessages (
                    MessageId NVARCHAR(256) PRIMARY KEY,
                    RequestType NVARCHAR(500) NOT NULL,
                    ReceivedAtUtc DATETIME2 NOT NULL,
                    ProcessedAtUtc DATETIME2 NULL,
                    Response NVARCHAR(MAX) NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    RetryCount INT NOT NULL DEFAULT 0,
                    NextRetryAtUtc DATETIME2 NULL,
                    ExpiresAtUtc DATETIME2 NOT NULL
                );

                CREATE INDEX IX_InboxMessages_ExpiresAtUtc
                ON InboxMessages(ExpiresAtUtc);
            END
            """;

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID('SagaStates', 'U') IS NULL
            BEGIN
                CREATE TABLE SagaStates (
                    SagaId UNIQUEIDENTIFIER PRIMARY KEY,
                    SagaType NVARCHAR(500) NOT NULL,
                    CurrentStep NVARCHAR(200) NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    Data NVARCHAR(MAX) NOT NULL,
                    StartedAtUtc DATETIME2 NOT NULL,
                    LastUpdatedAtUtc DATETIME2 NOT NULL,
                    CompletedAtUtc DATETIME2 NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    CompensationData NVARCHAR(MAX) NULL
                );

                CREATE INDEX IX_SagaStates_Status_LastUpdatedAtUtc
                ON SagaStates(Status, LastUpdatedAtUtc);
            END
            """;

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF OBJECT_ID('ScheduledMessages', 'U') IS NULL
            BEGIN
                CREATE TABLE ScheduledMessages (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    RequestType NVARCHAR(500) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    ScheduledAtUtc DATETIME2 NOT NULL,
                    ProcessedAtUtc DATETIME2 NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    RetryCount INT NOT NULL DEFAULT 0,
                    NextRetryAtUtc DATETIME2 NULL,
                    RecurrencePattern NVARCHAR(200) NULL
                );

                CREATE INDEX IX_ScheduledMessages_ScheduledAtUtc_ProcessedAtUtc
                ON ScheduledMessages(ScheduledAtUtc, ProcessedAtUtc);
            END
            """;

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(SqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS ScheduledMessages;
            DROP TABLE IF EXISTS SagaStates;
            DROP TABLE IF EXISTS InboxMessages;
            DROP TABLE IF EXISTS OutboxMessages;
            """;

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears all data from Encina tables without dropping schemas.
    /// Useful for cleaning between tests that share a database fixture.
    /// </summary>
    public static async Task ClearAllDataAsync(SqlConnection connection)
    {
        const string sql = """
            DELETE FROM ScheduledMessages;
            DELETE FROM SagaStates;
            DELETE FROM InboxMessages;
            DELETE FROM OutboxMessages;
            """;

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
