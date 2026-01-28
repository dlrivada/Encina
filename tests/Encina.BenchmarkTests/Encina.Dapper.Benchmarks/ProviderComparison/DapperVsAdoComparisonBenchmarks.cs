using System.Data;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using Dapper;
using Encina.Dapper.Benchmarks.Infrastructure;
using Encina.Dapper.Sqlite.TypeHandlers;
using Microsoft.Data.Sqlite;
using AdoInboxStore = Encina.ADO.Sqlite.Inbox.InboxStoreADO;
using AdoOutboxStore = Encina.ADO.Sqlite.Outbox.OutboxStoreADO;
using AdoScheduledStore = Encina.ADO.Sqlite.Scheduling.ScheduledMessageStoreADO;
using DapperInboxStore = Encina.Dapper.Sqlite.Inbox.InboxStoreDapper;
using DapperOutboxStore = Encina.Dapper.Sqlite.Outbox.OutboxStoreDapper;
using DapperScheduledStore = Encina.Dapper.Sqlite.Scheduling.ScheduledMessageStoreDapper;

namespace Encina.Dapper.Benchmarks.ProviderComparison;

/// <summary>
/// Direct comparison benchmarks between Dapper and ADO.NET implementations.
/// Focus on operations where micro-ORM overhead vs manual mapping matters most.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class DapperVsAdoComparisonBenchmarks
{
    private SqliteConnection _dapperConnection = null!;
    private SqliteConnection _adoConnection = null!;

    // Dapper stores
    private DapperOutboxStore _dapperOutboxStore = null!;
    private DapperInboxStore _dapperInboxStore = null!;
    private DapperScheduledStore _dapperScheduledStore = null!;

    // ADO stores
    private AdoOutboxStore _adoOutboxStore = null!;
    private AdoInboxStore _adoInboxStore = null!;
    private AdoScheduledStore _adoScheduledStore = null!;

    // Test data
    private List<BenchmarkOutboxMessage> _seededOutboxMessages = null!;
    private List<BenchmarkInboxMessage> _seededInboxMessages = null!;
    private List<BenchmarkScheduledMessage> _seededScheduledMessages = null!;
    private Guid _existingMessageId;
    private string _existingInboxMessageId = null!;

    /// <summary>
    /// Provider selection parameter.
    /// </summary>
    [Params("Dapper", "ADO")]
    public string Provider { get; set; } = "Dapper";

    /// <summary>
    /// Batch size for batch read operations.
    /// </summary>
    [Params(10, 100)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Global setup - creates databases and populates with test data.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        // Ensure type handlers are registered for Dapper
        GuidTypeHandler.EnsureRegistered();

        // Create separate connections for Dapper and ADO to isolate performance
        _dapperConnection = DapperConnectionFactory.CreateSharedMemorySqliteConnection("dapper_vs_ado_dapper");
        _adoConnection = DapperConnectionFactory.CreateSharedMemorySqliteConnection("dapper_vs_ado_ado");

        // Create schemas
        DapperSchemaBuilder.CreateOutboxTable(_dapperConnection, DatabaseProvider.Sqlite);
        DapperSchemaBuilder.CreateInboxTable(_dapperConnection, DatabaseProvider.Sqlite);
        DapperSchemaBuilder.CreateScheduledMessageTable(_dapperConnection, DatabaseProvider.Sqlite);

        DapperSchemaBuilder.CreateOutboxTable(_adoConnection, DatabaseProvider.Sqlite);
        DapperSchemaBuilder.CreateInboxTable(_adoConnection, DatabaseProvider.Sqlite);
        DapperSchemaBuilder.CreateScheduledMessageTable(_adoConnection, DatabaseProvider.Sqlite);

        // Initialize stores
        _dapperOutboxStore = new DapperOutboxStore(_dapperConnection);
        _dapperInboxStore = new DapperInboxStore(_dapperConnection);
        _dapperScheduledStore = new DapperScheduledStore(_dapperConnection);

        _adoOutboxStore = new AdoOutboxStore(_adoConnection);
        _adoInboxStore = new AdoInboxStore(_adoConnection);
        _adoScheduledStore = new AdoScheduledStore(_adoConnection);

        // Seed test data for both databases
        SeedTestData(_dapperConnection);
        SeedTestData(_adoConnection);

        _existingMessageId = _seededOutboxMessages[0].Id;
        _existingInboxMessageId = _seededInboxMessages[0].MessageId;
    }

    /// <summary>
    /// Global cleanup - disposes resources.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dapperConnection.Dispose();
        _adoConnection.Dispose();
    }

    #region Batch Read Operations

    /// <summary>
    /// Benchmarks GetPendingMessagesAsync (batch reads) - key area where Dapper vs ADO differs.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> BatchRead_OutboxGetPendingMessages()
    {
        if (Provider == "Dapper")
        {
            var messages = await _dapperOutboxStore.GetPendingMessagesAsync(BatchSize, maxRetries: 5);
            return messages.Count();
        }
        else
        {
            var messages = await _adoOutboxStore.GetPendingMessagesAsync(BatchSize, maxRetries: 5);
            return messages.Count();
        }
    }

    /// <summary>
    /// Benchmarks GetDueMessagesAsync - scheduled message batch retrieval.
    /// </summary>
    [Benchmark]
    public async Task<int> BatchRead_ScheduledGetDueMessages()
    {
        if (Provider == "Dapper")
        {
            var messages = await _dapperScheduledStore.GetDueMessagesAsync(BatchSize, maxRetries: 5);
            return messages.Count();
        }
        else
        {
            var messages = await _adoScheduledStore.GetDueMessagesAsync(BatchSize, maxRetries: 5);
            return messages.Count();
        }
    }

    #endregion

    #region Single Write Operations

    /// <summary>
    /// Benchmarks AddAsync for inbox (single insert).
    /// </summary>
    [Benchmark]
    public async Task SingleWrite_InboxAdd()
    {
        var message = BenchmarkEntityFactory.CreateInboxMessage();
        if (Provider == "Dapper")
        {
            await _dapperInboxStore.AddAsync(message);
        }
        else
        {
            await _adoInboxStore.AddAsync(message);
        }
    }

    /// <summary>
    /// Benchmarks AddAsync for outbox (single insert).
    /// </summary>
    [Benchmark]
    public async Task SingleWrite_OutboxAdd()
    {
        var message = BenchmarkEntityFactory.CreateOutboxMessage();
        if (Provider == "Dapper")
        {
            await _dapperOutboxStore.AddAsync(message);
        }
        else
        {
            await _adoOutboxStore.AddAsync(message);
        }
    }

    #endregion

    #region Parameterized Queries

    /// <summary>
    /// Benchmarks GetMessageAsync (parameterized query by ID).
    /// </summary>
    [Benchmark]
    public async Task ParameterizedQuery_InboxGetMessage()
    {
        if (Provider == "Dapper")
        {
            await _dapperInboxStore.GetMessageAsync(_existingInboxMessageId);
        }
        else
        {
            await _adoInboxStore.GetMessageAsync(_existingInboxMessageId);
        }
    }

    #endregion

    #region Status Update Operations

    /// <summary>
    /// Benchmarks MarkAsProcessedAsync (simple update).
    /// </summary>
    [Benchmark]
    public async Task StatusUpdate_OutboxMarkAsProcessed()
    {
        if (Provider == "Dapper")
        {
            await _dapperOutboxStore.MarkAsProcessedAsync(_existingMessageId);
        }
        else
        {
            await _adoOutboxStore.MarkAsProcessedAsync(_existingMessageId);
        }
    }

    /// <summary>
    /// Benchmarks MarkAsFailedAsync (update with multiple parameters).
    /// </summary>
    [Benchmark]
    public async Task StatusUpdate_OutboxMarkAsFailed()
    {
        if (Provider == "Dapper")
        {
            await _dapperOutboxStore.MarkAsFailedAsync(
                _existingMessageId,
                "Benchmark failure",
                DateTime.UtcNow.AddMinutes(5));
        }
        else
        {
            await _adoOutboxStore.MarkAsFailedAsync(
                _existingMessageId,
                "Benchmark failure",
                DateTime.UtcNow.AddMinutes(5));
        }
    }

    #endregion

    #region Raw Dapper vs Raw ADO.NET

    /// <summary>
    /// Raw Dapper query (not using stores).
    /// </summary>
    [Benchmark]
    public async Task<int> RawQuery_Dapper()
    {
        var conn = Provider == "Dapper" ? _dapperConnection : _adoConnection;
        var sql = @"SELECT ""Id"", ""NotificationType"", ""Content"", ""CreatedAtUtc"", ""ProcessedAtUtc"", ""ErrorMessage"", ""RetryCount"", ""NextRetryAtUtc""
                    FROM ""OutboxMessages""
                    WHERE ""ProcessedAtUtc"" IS NULL
                    LIMIT @Limit";
        var messages = await conn.QueryAsync<BenchmarkOutboxMessage>(sql, new { Limit = BatchSize });
        return messages.Count();
    }

    /// <summary>
    /// Raw ADO.NET query (not using stores).
    /// </summary>
    [Benchmark]
    public async Task<int> RawQuery_ADO()
    {
        var conn = Provider == "Dapper" ? _dapperConnection : _adoConnection;
        using var command = conn.CreateCommand();
        command.CommandText = @"SELECT ""Id"", ""NotificationType"", ""Content"", ""CreatedAtUtc"", ""ProcessedAtUtc"", ""ErrorMessage"", ""RetryCount"", ""NextRetryAtUtc""
                                FROM ""OutboxMessages""
                                WHERE ""ProcessedAtUtc"" IS NULL
                                LIMIT @Limit";
        command.Parameters.AddWithValue("@Limit", BatchSize);

        var messages = new List<BenchmarkOutboxMessage>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new BenchmarkOutboxMessage
            {
                Id = Guid.Parse(reader.GetString(0)),
                NotificationType = reader.GetString(1),
                Content = reader.GetString(2),
                CreatedAtUtc = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                ProcessedAtUtc = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                RetryCount = reader.GetInt32(6),
                NextRetryAtUtc = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture)
            });
        }
        return messages.Count;
    }

    #endregion

    #region Helper Methods

    private void SeedTestData(SqliteConnection connection)
    {
        // Seed outbox messages
        _seededOutboxMessages = BenchmarkEntityFactory.CreateOutboxMessages(500);
        foreach (var message in _seededOutboxMessages)
        {
            InsertOutboxMessage(connection, message);
        }

        // Seed inbox messages
        _seededInboxMessages = BenchmarkEntityFactory.CreateInboxMessages(200);
        foreach (var message in _seededInboxMessages)
        {
            InsertInboxMessage(connection, message);
        }

        // Seed scheduled messages (due)
        _seededScheduledMessages = new List<BenchmarkScheduledMessage>();
        for (int i = 0; i < 300; i++)
        {
            var message = BenchmarkEntityFactory.CreateScheduledMessage();
            message.ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10); // Due
            _seededScheduledMessages.Add(message);
            InsertScheduledMessage(connection, message);
        }
    }

    private static void InsertOutboxMessage(SqliteConnection connection, BenchmarkOutboxMessage message)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, ProcessedAtUtc, ErrorMessage, RetryCount, NextRetryAtUtc)
                                VALUES (@Id, @NotificationType, @Content, @CreatedAtUtc, @ProcessedAtUtc, @ErrorMessage, @RetryCount, @NextRetryAtUtc)";
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

    private static void InsertInboxMessage(SqliteConnection connection, BenchmarkInboxMessage message)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO InboxMessages (MessageId, RequestType, ReceivedAtUtc, ProcessedAtUtc, ExpiresAtUtc, Response, ErrorMessage, RetryCount, NextRetryAtUtc, Metadata)
                                VALUES (@MessageId, @RequestType, @ReceivedAtUtc, @ProcessedAtUtc, @ExpiresAtUtc, @Response, @ErrorMessage, @RetryCount, @NextRetryAtUtc, @Metadata)";
        command.Parameters.AddWithValue("@MessageId", message.MessageId);
        command.Parameters.AddWithValue("@RequestType", message.RequestType);
        command.Parameters.AddWithValue("@ReceivedAtUtc", message.ReceivedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@ProcessedAtUtc", message.ProcessedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ExpiresAtUtc", message.ExpiresAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@Response", message.Response ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", message.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", message.RetryCount);
        command.Parameters.AddWithValue("@NextRetryAtUtc", message.NextRetryAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Metadata", (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static void InsertScheduledMessage(SqliteConnection connection, BenchmarkScheduledMessage message)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO ScheduledMessages (Id, RequestType, Content, ScheduledAtUtc, CreatedAtUtc, ProcessedAtUtc, ErrorMessage, RetryCount, NextRetryAtUtc, IsRecurring, CronExpression, LastExecutedAtUtc)
                                VALUES (@Id, @RequestType, @Content, @ScheduledAtUtc, @CreatedAtUtc, @ProcessedAtUtc, @ErrorMessage, @RetryCount, @NextRetryAtUtc, @IsRecurring, @CronExpression, @LastExecutedAtUtc)";
        command.Parameters.AddWithValue("@Id", message.Id.ToString());
        command.Parameters.AddWithValue("@RequestType", message.RequestType);
        command.Parameters.AddWithValue("@Content", message.Content);
        command.Parameters.AddWithValue("@ScheduledAtUtc", message.ScheduledAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@CreatedAtUtc", message.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@ProcessedAtUtc", message.ProcessedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", message.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", message.RetryCount);
        command.Parameters.AddWithValue("@NextRetryAtUtc", message.NextRetryAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@IsRecurring", message.IsRecurring ? 1 : 0);
        command.Parameters.AddWithValue("@CronExpression", message.CronExpression ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LastExecutedAtUtc", message.LastExecutedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    #endregion
}
