using Microsoft.Data.Sqlite;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// SQLite schema creation for Encina test databases.
/// </summary>
public static class SqliteSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS OutboxMessages (
                Id TEXT PRIMARY KEY,
                NotificationType TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT NULL,
                ErrorMessage TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_OutboxMessages_ProcessedAtUtc_NextRetryAtUtc
            ON OutboxMessages(ProcessedAtUtc, NextRetryAtUtc);
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS InboxMessages (
                MessageId TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                ReceivedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT NULL,
                Response TEXT NULL,
                ErrorMessage TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                Metadata TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_InboxMessages_ExpiresAtUtc
            ON InboxMessages(ExpiresAtUtc);
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS SagaStates (
                SagaId TEXT PRIMARY KEY,
                SagaType TEXT NOT NULL,
                CurrentStep TEXT NOT NULL,
                Status TEXT NOT NULL,
                Data TEXT NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                LastUpdatedAtUtc TEXT NOT NULL,
                CompletedAtUtc TEXT NULL,
                ErrorMessage TEXT NULL,
                CompensationData TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_SagaStates_Status_LastUpdatedAtUtc
            ON SagaStates(Status, LastUpdatedAtUtc);
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS ScheduledMessages (
                Id TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT NULL,
                LastExecutedAtUtc TEXT NULL,
                ErrorMessage TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT NULL,
                IsRecurring INTEGER NOT NULL DEFAULT 0,
                CronExpression TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_ScheduledMessages_ScheduledAtUtc_ProcessedAtUtc
            ON ScheduledMessages(ScheduledAtUtc, ProcessedAtUtc);
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(SqliteConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS ScheduledMessages;
            DROP TABLE IF EXISTS SagaStates;
            DROP TABLE IF EXISTS InboxMessages;
            DROP TABLE IF EXISTS OutboxMessages;
            """;

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears all data from Encina tables without dropping schemas.
    /// Useful for cleaning between tests that share a database fixture.
    /// Uses conditional deletion to handle cases where tables may not exist.
    /// </summary>
    public static async Task ClearAllDataAsync(SqliteConnection connection)
    {
        // Delete from each table individually, ignoring errors for missing tables
        var tables = new[] { "ScheduledMessages", "SagaStates", "InboxMessages", "OutboxMessages" };
        foreach (var table in tables)
        {
            try
            {
                using var command = new SqliteCommand($"DELETE FROM {table};", connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1) // SQLITE_ERROR: no such table
            {
                // Table doesn't exist - this is ok, just skip
            }
        }
    }
}
