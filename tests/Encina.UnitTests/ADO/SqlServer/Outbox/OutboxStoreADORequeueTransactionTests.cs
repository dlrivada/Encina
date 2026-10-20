using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.ADO.SqlServer.Outbox;

/// <summary>
/// Verifies that requeueing exhausted outbox messages by identifier is all-or-nothing on the ADO.NET stores that
/// split the identifiers into several statements (SQL Server and MySQL): every chunk runs in one transaction that
/// is committed only when every chunk succeeded (#1167), and that the requeue joins the transaction of the scope
/// instead of beginning (and committing) its own (#718).
/// </summary>
/// <remarks>
/// SQLite in-memory is only a relational stand-in that executes the stores' portable UPDATE statement; no
/// provider claim is made. The dialect-specific behaviour is covered by the integration tests.
/// </remarks>
public sealed class OutboxStoreADORequeueTransactionTests : IAsyncDisposable
{
    // One more identifier than a chunk holds, so the requeue needs two statements.
    private const int IdCount = 1001;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Guid[] _ids = Enumerable.Range(0, IdCount).Select(_ => Guid.NewGuid()).ToArray();

    public static TheoryData<string> Providers => ["ADO.SqlServer", "ADO.MySQL"];

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task RequeueExhaustedAsync_SecondChunkFails_RollsBackTheFirstChunk(string provider)
    {
        await SeedAsync(poisonLastId: true);
        var store = CreateStore(provider, accessor: null);

        var result = await store.RequeueExhaustedAsync(3, _ids, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        (await ExhaustedCountAsync()).ShouldBe(IdCount);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task RequeueExhaustedAsync_EveryChunkSucceeds_CommitsAllChunks(string provider)
    {
        await SeedAsync(poisonLastId: false);
        var store = CreateStore(provider, accessor: null);

        var result = await store.RequeueExhaustedAsync(3, _ids, CancellationToken.None);

        result.Match(Right: count => count, Left: error => throw new InvalidOperationException(error.Message))
            .ShouldBe(IdCount);
        (await ExhaustedCountAsync()).ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task RequeueExhaustedAsync_ActiveTransaction_JoinsItAndLeavesTheCommitToItsOwner(string provider)
    {
        await SeedAsync(poisonLastId: false);
        var accessor = new DbTransactionAccessor(_connection, NullLogger<DbTransactionAccessor>.Instance);
        var store = CreateStore(provider, accessor);
        var owner = (await accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        var result = await store.RequeueExhaustedAsync(3, _ids, CancellationToken.None);
        result.IsRight.ShouldBeTrue();
        await owner.RollbackAsync();
        await owner.DisposeAsync();

        // The store did not commit: the owner's rollback undid the whole requeue.
        (await ExhaustedCountAsync()).ShouldBe(IdCount);
    }

    private IOutboxStore CreateStore(string provider, IDbTransactionAccessor? accessor) => provider switch
    {
        "ADO.SqlServer" => new global::Encina.ADO.SqlServer.Outbox.OutboxStoreADO(_connection, transactionAccessor: accessor),
        "ADO.MySQL" => new global::Encina.ADO.MySQL.Outbox.OutboxStoreADO(_connection, transactionAccessor: accessor),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private async Task SeedAsync(bool poisonLastId)
    {
        await _connection.OpenAsync();
        await using (var create = _connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE OutboxMessages (Id TEXT PRIMARY KEY, ProcessedAtUtc TEXT NULL, RetryCount INTEGER NOT NULL, NextRetryAtUtc TEXT NULL, ErrorMessage TEXT NULL)";
            await create.ExecuteNonQueryAsync();
        }

        await using (var transaction = await _connection.BeginTransactionAsync())
        {
            foreach (var id in _ids)
            {
                await using var insert = _connection.CreateCommand();
                insert.Transaction = (SqliteTransaction)transaction;
                insert.CommandText = "INSERT INTO OutboxMessages (Id, RetryCount) VALUES ($id, 3)";
                insert.Parameters.AddWithValue("$id", id);
                await insert.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        if (poisonLastId)
        {
            // The second chunk (the last identifier) fails, like a lock timeout would.
            await using var trigger = _connection.CreateCommand();
            trigger.CommandText =
                $"CREATE TRIGGER poison BEFORE UPDATE ON OutboxMessages WHEN OLD.Id = '{_ids[^1].ToString().ToUpperInvariant()}' BEGIN SELECT RAISE(ABORT, 'lock wait timeout'); END";
            await trigger.ExecuteNonQueryAsync();
        }
    }

    private async Task<long> ExhaustedCountAsync()
    {
        await using var count = _connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM OutboxMessages WHERE RetryCount >= 3";
        return (long)(await count.ExecuteScalarAsync())!;
    }
}
