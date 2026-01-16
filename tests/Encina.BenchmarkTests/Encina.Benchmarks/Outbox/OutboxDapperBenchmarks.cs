using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.Infrastructure;
using Encina.Dapper.Sqlite.Outbox;
using Microsoft.Data.Sqlite;

namespace Encina.Benchmarks.Outbox;

/// <summary>
/// Benchmarks for Dapper-based Outbox implementation.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class OutboxDapperBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private OutboxStoreDapper _store = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        DapperTypeHandlers.Register();

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        await SqliteSchemaBuilder.CreateOutboxSchemaAsync(_connection);
        _store = new OutboxStoreDapper(_connection);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clean table before each iteration
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM OutboxMessages";
        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = true, Description = "AddAsync single message")]
    public async Task AddAsync_Single()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "BenchmarkEvent",
            Content = "{\"test\":true}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();
    }

    [Benchmark(Description = "AddAsync 10 messages")]
    public async Task AddAsync_Batch10()
    {
        for (var i = 0; i < 10; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"BatchEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _store.SaveChangesAsync();
    }

    [Benchmark(Description = "GetPendingMessagesAsync batch=10")]
    public async Task GetPendingMessages_Batch10()
    {
        // Setup: Add 50 messages
        for (var i = 0; i < 50; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"QueryEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetPendingMessagesAsync(10, 5);
    }

    [Benchmark(Description = "MarkAsProcessedAsync")]
    public async Task MarkAsProcessed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = id,
            NotificationType = "ProcessTest",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsProcessedAsync(id);
        await _store.SaveChangesAsync();
    }

    [Benchmark(Description = "MarkAsFailedAsync")]
    public async Task MarkAsFailed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = id,
            NotificationType = "FailTest",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsFailedAsync(id, "Benchmark error", null);
        await _store.SaveChangesAsync();
    }
}
