using System.Data;
using Microsoft.Data.Sqlite;

namespace Encina.Dapper.Tests;

/// <summary>
/// Helper class for creating SQLite in-memory test databases with required schema.
/// </summary>
public sealed class SqliteTestHelper : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestHelper()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public IDbConnection Connection => _connection;

    public void CreateOutboxTable()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE OutboxMessages (
                Id TEXT PRIMARY KEY,
                NotificationType TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT,
                ErrorMessage TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT
            )";
        command.ExecuteNonQuery();
    }

    public void CreateInboxTable()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE InboxMessages (
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
        command.ExecuteNonQuery();
    }

    public void CreateSagaTable()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE SagaStates (
                SagaId TEXT PRIMARY KEY,
                SagaType TEXT NOT NULL,
                Data TEXT NOT NULL,
                Status TEXT NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                LastUpdatedAtUtc TEXT NOT NULL,
                CompletedAtUtc TEXT,
                ErrorMessage TEXT,
                CurrentStep INTEGER NOT NULL DEFAULT 0,
                TimeoutAtUtc TEXT
            )";
        command.ExecuteNonQuery();
    }

    public void CreateScheduledMessageTable()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE ScheduledMessages (
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
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
