using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.Sqlite.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Benchmarks.Stores;

/// <summary>
/// Benchmarks for ADO.NET OutboxStore operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class OutboxStoreBenchmarks
{
    private SqliteConnection _connection = null!;
    private OutboxStoreADO _store = null!;
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
        _connection = AdoConnectionFactory.CreateSharedMemorySqliteConnection("outbox_ado_benchmark");
        AdoSchemaBuilder.CreateOutboxTable(_connection, DatabaseProvider.Sqlite);

        // Pre-populate with messages based on max batch size
        _pendingMessages = BenchmarkEntityFactory.CreateOutboxMessages(1100);
        foreach (var message in _pendingMessages)
        {
            InsertMessage(message);
        }

        // Store IDs for single-message operations
        _messageToProcessId = _pendingMessages[0].Id;
        _messageToFailId = _pendingMessages[1].Id;

        _store = new OutboxStoreADO(_connection);
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

        command.Parameters.AddWithValue("@Id", message.Id.ToString());
        command.Parameters.AddWithValue("@NotificationType", message.NotificationType);
        command.Parameters.AddWithValue("@Content", message.Content);
        command.Parameters.AddWithValue("@CreatedAtUtc", message.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@ProcessedAtUtc", message.ProcessedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", message.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", message.RetryCount);
        command.Parameters.AddWithValue("@NextRetryAtUtc", message.NextRetryAtUtc?.ToString("O") ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }
}
