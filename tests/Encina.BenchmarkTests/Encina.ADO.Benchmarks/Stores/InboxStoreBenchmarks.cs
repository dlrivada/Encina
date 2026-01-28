using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.Sqlite.Inbox;
using Encina.Messaging.Inbox;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Benchmarks.Stores;

/// <summary>
/// Benchmarks for ADO.NET InboxStore operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class InboxStoreBenchmarks
{
    private SqliteConnection _connection = null!;
    private InboxStoreADO _store = null!;
    private List<BenchmarkInboxMessage> _messages = null!;
    private List<BenchmarkInboxMessage> _expiredMessages = null!;
    private string _existingMessageId = null!;
    private string _messageToRetryId = null!;
    private int _newMessageCounter;

    /// <summary>
    /// Batch size for batch operations.
    /// </summary>
    [Params(10, 100, 1000)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Global setup - creates database and populates with test data.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = AdoConnectionFactory.CreateSharedMemorySqliteConnection("inbox_ado_benchmark");
        AdoSchemaBuilder.CreateInboxTable(_connection, DatabaseProvider.Sqlite);

        // Create messages with varied states
        _messages = new List<BenchmarkInboxMessage>();
        _expiredMessages = new List<BenchmarkInboxMessage>();

        // Pending messages
        for (int i = 0; i < 500; i++)
        {
            var msg = BenchmarkEntityFactory.CreateInboxMessage();
            _messages.Add(msg);
            InsertMessage(msg);
        }

        // Processed messages
        for (int i = 0; i < 500; i++)
        {
            var msg = BenchmarkEntityFactory.CreateInboxMessage();
            msg.ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-10);
            msg.Response = """{"result":"success"}""";
            _messages.Add(msg);
            InsertMessage(msg);
        }

        // Expired messages (processed + past expiration)
        for (int i = 0; i < 200; i++)
        {
            var msg = BenchmarkEntityFactory.CreateInboxMessage();
            msg.ProcessedAtUtc = DateTime.UtcNow.AddDays(-10);
            msg.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1); // Expired
            msg.Response = """{"result":"old"}""";
            _expiredMessages.Add(msg);
            InsertMessage(msg);
        }

        _existingMessageId = _messages[0].MessageId;
        _messageToRetryId = _messages[1].MessageId;
        _newMessageCounter = 0;

        _store = new InboxStoreADO(_connection);
    }

    /// <summary>
    /// Iteration setup - resets counters for consistent benchmarks.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _newMessageCounter++;
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
    /// Benchmarks looking up an existing message by ID.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<IInboxMessage?> GetMessageAsync()
    {
        return await _store.GetMessageAsync(_existingMessageId);
    }

    /// <summary>
    /// Benchmarks adding a new inbox message.
    /// </summary>
    [Benchmark]
    public async Task AddAsync()
    {
        var msg = BenchmarkEntityFactory.CreateInboxMessage();
        msg.MessageId = $"bench-{_newMessageCounter}-{Guid.NewGuid()}";
        await _store.AddAsync(msg);
    }

    /// <summary>
    /// Benchmarks incrementing retry count.
    /// </summary>
    [Benchmark]
    public async Task IncrementRetryCountAsync()
    {
        await _store.IncrementRetryCountAsync(_messageToRetryId);
    }

    /// <summary>
    /// Benchmarks querying for expired messages.
    /// </summary>
    [Benchmark]
    public async Task<List<IInboxMessage>> GetExpiredMessagesAsync()
    {
        var messages = await _store.GetExpiredMessagesAsync(BatchSize);
        return messages.ToList();
    }

    /// <summary>
    /// Benchmarks the check-then-add idempotency pattern.
    /// </summary>
    [Benchmark]
    public async Task CheckThenAddPattern()
    {
        var messageId = $"idempotent-{_newMessageCounter}-{Guid.NewGuid()}";

        // Check if exists
        var existing = await _store.GetMessageAsync(messageId);
        if (existing == null)
        {
            // Add if not exists
            var msg = BenchmarkEntityFactory.CreateInboxMessage();
            msg.MessageId = messageId;
            await _store.AddAsync(msg);
        }
    }

    private void InsertMessage(BenchmarkInboxMessage message)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO InboxMessages
            (MessageId, RequestType, ReceivedAtUtc, ProcessedAtUtc, ExpiresAtUtc, Response, ErrorMessage, RetryCount, NextRetryAtUtc, Metadata)
            VALUES
            (@MessageId, @RequestType, @ReceivedAtUtc, @ProcessedAtUtc, @ExpiresAtUtc, @Response, @ErrorMessage, @RetryCount, @NextRetryAtUtc, @Metadata)";

        command.Parameters.AddWithValue("@MessageId", message.MessageId);
        command.Parameters.AddWithValue("@RequestType", message.RequestType);
        command.Parameters.AddWithValue("@ReceivedAtUtc", message.ReceivedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@ProcessedAtUtc", message.ProcessedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ExpiresAtUtc", message.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@Response", message.Response ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", message.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", message.RetryCount);
        command.Parameters.AddWithValue("@NextRetryAtUtc", message.NextRetryAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Metadata", DBNull.Value);

        command.ExecuteNonQuery();
    }
}
