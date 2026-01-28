using BenchmarkDotNet.Attributes;
using Encina.Dapper.Benchmarks.Infrastructure;
using Encina.Dapper.Sqlite.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.Data.Sqlite;

namespace Encina.Dapper.Benchmarks.Stores;

/// <summary>
/// Benchmarks for Dapper OutboxStore operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class OutboxStoreBenchmarks
{
    private SqliteConnection _connection = null!;
    private OutboxStoreDapper _store = null!;
    private List<BenchmarkOutboxMessage> _pendingMessages = null!;
    private Guid _messageToProcessId;
    private Guid _messageToFailId;

    /// <summary>
    /// Batch size for GetPendingMessages benchmark.
    /// </summary>
    [Params(10, 100, 1000)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Global setup - creates database and populates with test data.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = DapperConnectionFactory.CreateSharedMemorySqliteConnection("outbox_dapper_benchmark");
        ProviderTypeHandlers.EnsureRegistered(DatabaseProvider.Sqlite);
        DapperSchemaBuilder.CreateOutboxTable(_connection, DatabaseProvider.Sqlite);

        // Pre-populate with messages based on max batch size
        _pendingMessages = BenchmarkEntityFactory.CreateOutboxMessages(1100);
        foreach (var message in _pendingMessages)
        {
            InsertMessage(message);
        }

        // Store IDs for single-message operations
        _messageToProcessId = _pendingMessages[0].Id;
        _messageToFailId = _pendingMessages[1].Id;

        _store = new OutboxStoreDapper(_connection);
    }

    /// <summary>
    /// Iteration setup - resets message states for consistent benchmarks.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        // Reset all messages to pending state
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE OutboxMessages
            SET ProcessedAtUtc = NULL,
                ErrorMessage = NULL,
                RetryCount = 0,
                NextRetryAtUtc = NULL";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Global cleanup - disposes resources.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection.Dispose();
    }

    /// <summary>
    /// Benchmarks batch retrieval of pending messages.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<List<IOutboxMessage>> GetPendingMessagesAsync()
    {
        var messages = await _store.GetPendingMessagesAsync(BatchSize, maxRetries: 3);
        return messages.ToList();
    }

    /// <summary>
    /// Benchmarks marking a single message as processed.
    /// </summary>
    [Benchmark]
    public async Task MarkAsProcessedAsync()
    {
        await _store.MarkAsProcessedAsync(_messageToProcessId);
    }

    /// <summary>
    /// Benchmarks marking a single message as failed with error recording.
    /// </summary>
    [Benchmark]
    public async Task MarkAsFailedAsync()
    {
        await _store.MarkAsFailedAsync(
            _messageToFailId,
            "Test error message for benchmark",
            DateTime.UtcNow.AddMinutes(5));
    }

    private void InsertMessage(BenchmarkOutboxMessage message)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO OutboxMessages
            (Id, NotificationType, Content, CreatedAtUtc, ProcessedAtUtc, ErrorMessage, RetryCount, NextRetryAtUtc)
            VALUES
            (@Id, @NotificationType, @Content, @CreatedAtUtc, @ProcessedAtUtc, @ErrorMessage, @RetryCount, @NextRetryAtUtc)";

        AddParameter(command, "@Id", message.Id.ToString());
        AddParameter(command, "@NotificationType", message.NotificationType);
        AddParameter(command, "@Content", message.Content);
        AddParameter(command, "@CreatedAtUtc", message.CreatedAtUtc.ToString("O"));
        AddParameter(command, "@ProcessedAtUtc", message.ProcessedAtUtc?.ToString("O"));
        AddParameter(command, "@ErrorMessage", message.ErrorMessage);
        AddParameter(command, "@RetryCount", message.RetryCount);
        AddParameter(command, "@NextRetryAtUtc", message.NextRetryAtUtc?.ToString("O"));

        command.ExecuteNonQuery();
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }
}
