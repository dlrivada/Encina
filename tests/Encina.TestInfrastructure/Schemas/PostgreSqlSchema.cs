using Npgsql;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// PostgreSQL schema creation for Encina test databases.
/// </summary>
public static class PostgreSqlSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS OutboxMessages (
                Id UUID PRIMARY KEY,
                NotificationType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TIMESTAMP NOT NULL,
                ProcessedAtUtc TIMESTAMP NULL,
                ErrorMessage TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TIMESTAMP NULL
            );

            CREATE INDEX IF NOT EXISTS IX_OutboxMessages_ProcessedAtUtc_NextRetryAtUtc
            ON OutboxMessages(ProcessedAtUtc, NextRetryAtUtc);
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
            CREATE TABLE IF NOT EXISTS InboxMessages (
                MessageId VARCHAR(256) PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                ReceivedAtUtc TIMESTAMP NOT NULL,
                ProcessedAtUtc TIMESTAMP NULL,
                Response TEXT NULL,
                ErrorMessage TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TIMESTAMP NULL,
                ExpiresAtUtc TIMESTAMP NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_InboxMessages_ExpiresAtUtc
            ON InboxMessages(ExpiresAtUtc);
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
            CREATE TABLE IF NOT EXISTS SagaStates (
                SagaId UUID PRIMARY KEY,
                SagaType VARCHAR(500) NOT NULL,
                CurrentStep INTEGER NOT NULL,
                Status VARCHAR(50) NOT NULL,
                Data TEXT NOT NULL,
                StartedAtUtc TIMESTAMP NOT NULL,
                LastUpdatedAtUtc TIMESTAMP NOT NULL,
                CompletedAtUtc TIMESTAMP NULL,
                ErrorMessage TEXT NULL,
                TimeoutAtUtc TIMESTAMP NULL
            );

            CREATE INDEX IF NOT EXISTS IX_SagaStates_Status_LastUpdatedAtUtc
            ON SagaStates(Status, LastUpdatedAtUtc);
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
            CREATE TABLE IF NOT EXISTS ScheduledMessages (
                Id UUID PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc TIMESTAMP NOT NULL,
                ProcessedAtUtc TIMESTAMP NULL,
                ErrorMessage TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TIMESTAMP NULL,
                RecurrencePattern VARCHAR(200) NULL
            );

            CREATE INDEX IF NOT EXISTS IX_ScheduledMessages_ScheduledAtUtc_ProcessedAtUtc
            ON ScheduledMessages(ScheduledAtUtc, ProcessedAtUtc);
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
            DROP TABLE IF EXISTS ScheduledMessages CASCADE;
            DROP TABLE IF EXISTS SagaStates CASCADE;
            DROP TABLE IF EXISTS InboxMessages CASCADE;
            DROP TABLE IF EXISTS OutboxMessages CASCADE;
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
            DELETE FROM ScheduledMessages;
            DELETE FROM SagaStates;
            DELETE FROM InboxMessages;
            DELETE FROM OutboxMessages;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
