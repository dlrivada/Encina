using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.Sqlite.Scheduling;
using Encina.Messaging.Scheduling;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Benchmarks.Stores;

/// <summary>
/// Benchmarks for ADO.NET ScheduledMessageStore operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ScheduledMessageStoreBenchmarks
{
    private SqliteConnection _connection = null!;
    private ScheduledMessageStoreADO _store = null!;
    private List<BenchmarkScheduledMessage> _dueMessages = null!;
    private List<BenchmarkScheduledMessage> _recurringMessages = null!;
    private Guid _messageToProcessId;
    private Guid _messageToFailId;
    private Guid _recurringMessageId;
    private Guid _messageToCancel;
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
        _connection = AdoConnectionFactory.CreateSharedMemorySqliteConnection("scheduled_ado_benchmark");
        AdoSchemaBuilder.CreateScheduledMessageTable(_connection, DatabaseProvider.Sqlite);

        _dueMessages = new List<BenchmarkScheduledMessage>();
        _recurringMessages = new List<BenchmarkScheduledMessage>();

        // Due one-time messages (ScheduledAtUtc in the past)
        for (int i = 0; i < 500; i++)
        {
            var msg = BenchmarkEntityFactory.CreateScheduledMessage();
            msg.ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10);
            msg.IsRecurring = false;
            _dueMessages.Add(msg);
            InsertMessage(msg);
        }

        // Due recurring messages
        for (int i = 0; i < 200; i++)
        {
            var msg = BenchmarkEntityFactory.CreateScheduledMessage();
            msg.ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5);
            msg.IsRecurring = true;
            msg.CronExpression = "0 * * * *"; // Every hour
            _recurringMessages.Add(msg);
            InsertMessage(msg);
        }

        // Future messages (not due yet)
        for (int i = 0; i < 300; i++)
        {
            var msg = BenchmarkEntityFactory.CreateScheduledMessage();
            msg.ScheduledAtUtc = DateTime.UtcNow.AddHours(1);
            InsertMessage(msg);
        }

        // Processed messages
        for (int i = 0; i < 200; i++)
        {
            var msg = BenchmarkEntityFactory.CreateScheduledMessage();
            msg.ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30);
            msg.IsRecurring = false;
            InsertMessage(msg);
        }

        // Failed messages with retries
        for (int i = 0; i < 100; i++)
        {
            var msg = BenchmarkEntityFactory.CreateScheduledMessage();
            msg.ErrorMessage = "Transient failure";
            msg.RetryCount = 2;
            msg.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-1);
            InsertMessage(msg);
        }

        _messageToProcessId = _dueMessages[0].Id;
        _messageToFailId = _dueMessages[1].Id;
        _recurringMessageId = _recurringMessages[0].Id;
        _messageToCancel = _dueMessages[2].Id;
        _newMessageCounter = 0;

        _store = new ScheduledMessageStoreADO(_connection);
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
    /// Benchmarks querying for due messages with batch size.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<List<IScheduledMessage>> GetDueMessagesAsync()
    {
        var messages = await _store.GetDueMessagesAsync(BatchSize, maxRetries: 5);
        return messages.ToList();
    }

    /// <summary>
    /// Benchmarks adding a new scheduled message.
    /// </summary>
    [Benchmark]
    public async Task AddAsync()
    {
        var msg = BenchmarkEntityFactory.CreateScheduledMessage();
        msg.Id = Guid.NewGuid();
        await _store.AddAsync(msg);
    }

    /// <summary>
    /// Benchmarks marking a message as processed.
    /// </summary>
    [Benchmark]
    public async Task MarkAsProcessedAsync()
    {
        await _store.MarkAsProcessedAsync(_messageToProcessId);
    }

    /// <summary>
    /// Benchmarks marking a message as failed with retry scheduling.
    /// </summary>
    [Benchmark]
    public async Task MarkAsFailedAsync()
    {
        await _store.MarkAsFailedAsync(
            _messageToFailId,
            "Simulated benchmark failure",
            DateTime.UtcNow.AddMinutes(5));
    }

    /// <summary>
    /// Benchmarks rescheduling a recurring message.
    /// </summary>
    [Benchmark]
    public async Task RescheduleRecurringMessageAsync()
    {
        await _store.RescheduleRecurringMessageAsync(
            _recurringMessageId,
            DateTime.UtcNow.AddHours(1));
    }

    /// <summary>
    /// Benchmarks canceling a scheduled message.
    /// </summary>
    [Benchmark]
    public async Task CancelAsync()
    {
        // Create a new message each iteration to avoid canceling the same message
        var msg = BenchmarkEntityFactory.CreateScheduledMessage();
        msg.Id = Guid.NewGuid();
        InsertMessage(msg);
        await _store.CancelAsync(msg.Id);
    }

    private void InsertMessage(BenchmarkScheduledMessage message)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ScheduledMessages
            (Id, RequestType, Content, ScheduledAtUtc, CreatedAtUtc, ProcessedAtUtc, LastExecutedAtUtc,
             ErrorMessage, RetryCount, NextRetryAtUtc, IsRecurring, CronExpression)
            VALUES
            (@Id, @RequestType, @Content, @ScheduledAtUtc, @CreatedAtUtc, @ProcessedAtUtc, @LastExecutedAtUtc,
             @ErrorMessage, @RetryCount, @NextRetryAtUtc, @IsRecurring, @CronExpression)";

        command.Parameters.AddWithValue("@Id", message.Id.ToString());
        command.Parameters.AddWithValue("@RequestType", message.RequestType);
        command.Parameters.AddWithValue("@Content", message.Content);
        command.Parameters.AddWithValue("@ScheduledAtUtc", message.ScheduledAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@CreatedAtUtc", message.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@ProcessedAtUtc", message.ProcessedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LastExecutedAtUtc", message.LastExecutedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", message.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", message.RetryCount);
        command.Parameters.AddWithValue("@NextRetryAtUtc", message.NextRetryAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@IsRecurring", message.IsRecurring ? 1 : 0);
        command.Parameters.AddWithValue("@CronExpression", message.CronExpression ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }
}
