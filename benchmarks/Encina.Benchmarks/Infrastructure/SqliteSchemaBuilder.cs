using Microsoft.Data.Sqlite;

namespace Encina.Benchmarks.Infrastructure;

/// <summary>
/// Helper to create SQLite schemas for Dapper benchmarks.
/// </summary>
public static class SqliteSchemaBuilder
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS OutboxMessages (
                Id TEXT PRIMARY KEY,
                NotificationType TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT,
                ErrorMessage TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT
            )";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await Task.Run(() => command.ExecuteNonQuery());
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS InboxMessages (
                MessageId TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                ReceivedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT,
                ExpiresAtUtc TEXT NOT NULL,
                Response TEXT,
                ErrorMessage TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT,
                Metadata TEXT
            )";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await Task.Run(() => command.ExecuteNonQuery());
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS SagaStates (
                SagaId TEXT PRIMARY KEY,
                SagaType TEXT NOT NULL,
                Data TEXT NOT NULL,
                Status TEXT NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                LastUpdatedAtUtc TEXT NOT NULL,
                CompletedAtUtc TEXT,
                ErrorMessage TEXT,
                CurrentStep INTEGER NOT NULL DEFAULT 0
            )";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await Task.Run(() => command.ExecuteNonQuery());
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS ScheduledMessages (
                Id TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT,
                LastExecutedAtUtc TEXT,
                ErrorMessage TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT,
                IsRecurring INTEGER NOT NULL DEFAULT 0,
                CronExpression TEXT
            )";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await Task.Run(() => command.ExecuteNonQuery());
    }
}
