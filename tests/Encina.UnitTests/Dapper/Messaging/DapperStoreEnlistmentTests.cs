using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Dapper.Messaging;

/// <summary>
/// Verifies that the Dapper outbox, saga and scheduled-message stores enlist every command in the transaction the
/// scope shares (#719): a write made while a transaction is active is undone by the owner's rollback, a write
/// without an accessor or without an active transaction autocommits, and the chunked outbox requeue is
/// all-or-nothing and joins an active transaction instead of beginning its own.
/// </summary>
/// <remarks>
/// SQLite in-memory is only a relational stand-in that executes the stores' portable INSERT, UPDATE and DELETE
/// statements; no provider claim is made. The dialect-specific behaviour is covered by the integration tests.
/// </remarks>
public sealed class DapperStoreEnlistmentTests : IAsyncDisposable
{
    // One more identifier than a chunk holds, so the requeue needs two statements.
    private const int IdCount = 1001;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public static TheoryData<string> Providers => ["SqlServer", "MySQL"];

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task OutboxAdd_ActiveTransaction_IsUndoneByTheOwnersRollback()
    {
        await CreateSchemaAsync();
        var accessor = NewAccessor();
        var store = new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(_connection, transactionAccessor: accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        (await store.AddAsync(OutboxRow(), CancellationToken.None)).IsRight.ShouldBeTrue();
        await owner.RollbackAsync();
        await owner.DisposeAsync();

        (await CountAsync("OutboxMessages")).ShouldBe(0);
    }

    [Fact]
    public async Task OutboxAdd_ActiveTransaction_IsKeptByTheOwnersCommit()
    {
        await CreateSchemaAsync();
        var accessor = NewAccessor();
        var store = new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(_connection, transactionAccessor: accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        (await store.AddAsync(OutboxRow(), CancellationToken.None)).IsRight.ShouldBeTrue();
        (await owner.CommitAsync()).IsRight.ShouldBeTrue();
        await owner.DisposeAsync();

        (await CountAsync("OutboxMessages")).ShouldBe(1);
    }

    [Fact]
    public async Task OutboxAdd_WithoutAccessor_Autocommits()
    {
        await CreateSchemaAsync();
        var store = new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(_connection);

        (await store.AddAsync(OutboxRow(), CancellationToken.None)).IsRight.ShouldBeTrue();

        (await CountAsync("OutboxMessages")).ShouldBe(1);
    }

    [Fact]
    public async Task OutboxAdd_AccessorWithoutActiveTransaction_Autocommits()
    {
        await CreateSchemaAsync();
        var store = new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(_connection, transactionAccessor: NewAccessor());

        (await store.AddAsync(OutboxRow(), CancellationToken.None)).IsRight.ShouldBeTrue();

        (await CountAsync("OutboxMessages")).ShouldBe(1);
    }

    [Fact]
    public async Task OutboxMarkAsProcessed_ActiveTransaction_IsUndoneByTheOwnersRollback()
    {
        await CreateSchemaAsync();
        var plain = new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(_connection);
        var row = OutboxRow();
        (await plain.AddAsync(row, CancellationToken.None)).IsRight.ShouldBeTrue();
        var accessor = NewAccessor();
        var store = new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(_connection, transactionAccessor: accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        (await store.MarkAsProcessedAsync(row.Id, CancellationToken.None)).IsRight.ShouldBeTrue();
        await owner.RollbackAsync();
        await owner.DisposeAsync();

        (await CountAsync("OutboxMessages WHERE ProcessedAtUtc IS NOT NULL")).ShouldBe(0);
    }

    [Fact]
    public async Task SagaAddAndUpdate_ActiveTransaction_AreUndoneByTheOwnersRollback()
    {
        await CreateSchemaAsync();
        var accessor = NewAccessor();
        var store = new global::Encina.Dapper.SqlServer.Sagas.SagaStoreDapper(_connection, transactionAccessor: accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());
        var saga = new global::Encina.Dapper.SqlServer.Sagas.SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Order",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        (await store.AddAsync(saga, CancellationToken.None)).IsRight.ShouldBeTrue();
        (await store.UpdateAsync(saga, CancellationToken.None)).IsRight.ShouldBeTrue();
        await owner.RollbackAsync();
        await owner.DisposeAsync();

        (await CountAsync("SagaStates")).ShouldBe(0);
    }

    [Fact]
    public async Task SagaAdd_WithoutAccessor_Autocommits()
    {
        await CreateSchemaAsync();
        var store = new global::Encina.Dapper.SqlServer.Sagas.SagaStoreDapper(_connection);
        var saga = new global::Encina.Dapper.SqlServer.Sagas.SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Order",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        (await store.AddAsync(saga, CancellationToken.None)).IsRight.ShouldBeTrue();

        (await CountAsync("SagaStates")).ShouldBe(1);
    }

    [Fact]
    public async Task ScheduledAddAndCancel_ActiveTransaction_AreUndoneByTheOwnersRollback()
    {
        await CreateSchemaAsync();
        var plain = new global::Encina.Dapper.SqlServer.Scheduling.ScheduledMessageStoreDapper(_connection);
        var kept = ScheduledRow();
        (await plain.AddAsync(kept, CancellationToken.None)).IsRight.ShouldBeTrue();
        var accessor = NewAccessor();
        var store = new global::Encina.Dapper.SqlServer.Scheduling.ScheduledMessageStoreDapper(_connection, transactionAccessor: accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        (await store.AddAsync(ScheduledRow(), CancellationToken.None)).IsRight.ShouldBeTrue();
        (await store.CancelAsync(kept.Id, CancellationToken.None)).IsRight.ShouldBeTrue();
        await owner.RollbackAsync();
        await owner.DisposeAsync();

        (await CountAsync("ScheduledMessages")).ShouldBe(1);
    }

    [Fact]
    public async Task ScheduledMarkAsProcessed_WithoutAccessor_Autocommits()
    {
        await CreateSchemaAsync();
        var store = new global::Encina.Dapper.SqlServer.Scheduling.ScheduledMessageStoreDapper(_connection);
        var row = ScheduledRow();
        (await store.AddAsync(row, CancellationToken.None)).IsRight.ShouldBeTrue();

        (await store.MarkAsProcessedAsync(row.Id, CancellationToken.None)).IsRight.ShouldBeTrue();

        (await CountAsync("ScheduledMessages WHERE ProcessedAtUtc IS NOT NULL")).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Requeue_SecondChunkFails_RollsBackTheFirstChunk(string provider)
    {
        var ids = await SeedExhaustedAsync(poisonLastId: true);
        var store = CreateOutboxStore(provider, accessor: null);

        var result = await store.RequeueExhaustedAsync(3, ids, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        (await CountAsync("OutboxMessages WHERE RetryCount >= 3")).ShouldBe(IdCount);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Requeue_EveryChunkSucceeds_CommitsAllChunks(string provider)
    {
        var ids = await SeedExhaustedAsync(poisonLastId: false);
        var store = CreateOutboxStore(provider, accessor: null);

        var result = await store.RequeueExhaustedAsync(3, ids, CancellationToken.None);

        result.Match(Right: count => count, Left: error => throw new InvalidOperationException(error.Message))
            .ShouldBe(IdCount);
        (await CountAsync("OutboxMessages WHERE RetryCount >= 3")).ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Requeue_AllExhausted_EnlistsInTheActiveTransaction(string provider)
    {
        await SeedExhaustedAsync(poisonLastId: false);
        var accessor = NewAccessor();
        var store = CreateOutboxStore(provider, accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        (await store.RequeueExhaustedAsync(3, messageIds: null, CancellationToken.None)).IsRight.ShouldBeTrue();
        await owner.RollbackAsync();
        await owner.DisposeAsync();

        (await CountAsync("OutboxMessages WHERE RetryCount >= 3")).ShouldBe(IdCount);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Requeue_ByIds_JoinsTheActiveTransactionAndLeavesTheCommitToItsOwner(string provider)
    {
        var ids = await SeedExhaustedAsync(poisonLastId: false);
        var accessor = NewAccessor();
        var store = CreateOutboxStore(provider, accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        (await store.RequeueExhaustedAsync(3, ids, CancellationToken.None)).IsRight.ShouldBeTrue();
        await owner.RollbackAsync();
        await owner.DisposeAsync();

        // The store did not commit: the owner's rollback undid the whole requeue.
        (await CountAsync("OutboxMessages WHERE RetryCount >= 3")).ShouldBe(IdCount);
    }

    private DbTransactionAccessor NewAccessor() =>
        new(_connection, NullLogger<DbTransactionAccessor>.Instance);

    private IOutboxStore CreateOutboxStore(string provider, IDbTransactionAccessor? accessor) => provider switch
    {
        "SqlServer" => new global::Encina.Dapper.SqlServer.Outbox.OutboxStoreDapper(_connection, transactionAccessor: accessor),
        "MySQL" => new global::Encina.Dapper.MySQL.Outbox.OutboxStoreDapper(_connection, transactionAccessor: accessor),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private static global::Encina.Dapper.SqlServer.Outbox.OutboxMessage OutboxRow() => new()
    {
        Id = Guid.NewGuid(),
        NotificationType = "Probe",
        Content = "{}",
        CreatedAtUtc = DateTime.UtcNow
    };

    private static global::Encina.Dapper.SqlServer.Scheduling.ScheduledMessage ScheduledRow() => new()
    {
        Id = Guid.NewGuid(),
        RequestType = "Probe",
        Content = "{}",
        ScheduledAtUtc = DateTime.UtcNow,
        CreatedAtUtc = DateTime.UtcNow
    };

    private async Task CreateSchemaAsync()
    {
        await _connection.OpenAsync();
        await ExecuteAsync(
            "CREATE TABLE OutboxMessages (Id TEXT PRIMARY KEY, NotificationType TEXT NOT NULL, Content TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, ProcessedAtUtc TEXT NULL, ErrorMessage TEXT NULL, RetryCount INTEGER NOT NULL, NextRetryAtUtc TEXT NULL);" +
            "CREATE TABLE SagaStates (SagaId TEXT PRIMARY KEY, SagaType TEXT NOT NULL, Data TEXT NOT NULL, Status TEXT NOT NULL, StartedAtUtc TEXT NOT NULL, LastUpdatedAtUtc TEXT NOT NULL, CompletedAtUtc TEXT NULL, ErrorMessage TEXT NULL, CurrentStep INTEGER NOT NULL, TimeoutAtUtc TEXT NULL);" +
            "CREATE TABLE ScheduledMessages (Id TEXT PRIMARY KEY, RequestType TEXT NOT NULL, Content TEXT NOT NULL, ScheduledAtUtc TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, ProcessedAtUtc TEXT NULL, LastExecutedAtUtc TEXT NULL, ErrorMessage TEXT NULL, RetryCount INTEGER NOT NULL, NextRetryAtUtc TEXT NULL, IsRecurring INTEGER NOT NULL, CronExpression TEXT NULL);");
    }

    private async Task<Guid[]> SeedExhaustedAsync(bool poisonLastId)
    {
        await CreateSchemaAsync();
        var ids = Enumerable.Range(0, IdCount).Select(_ => Guid.NewGuid()).ToArray();

        await using (var transaction = await _connection.BeginTransactionAsync())
        {
            foreach (var id in ids)
            {
                await using var insert = _connection.CreateCommand();
                insert.Transaction = (SqliteTransaction)transaction;
                insert.CommandText = "INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount) VALUES ($id, 'Probe', '{}', '2026-01-01', 3)";
                insert.Parameters.AddWithValue("$id", id);
                await insert.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        if (poisonLastId)
        {
            // The second chunk (the last identifier) fails, like a lock timeout would.
            await ExecuteAsync(
                $"CREATE TRIGGER poison BEFORE UPDATE ON OutboxMessages WHEN OLD.Id = '{ids[^1].ToString().ToUpperInvariant()}' BEGIN SELECT RAISE(ABORT, 'lock wait timeout'); END");
        }

        return ids;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> CountAsync(string fromAndWhere)
    {
        await using var count = _connection.CreateCommand();
        count.CommandText = $"SELECT COUNT(*) FROM {fromAndWhere}";
        return (long)(await count.ExecuteScalarAsync())!;
    }
}
