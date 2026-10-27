using System.Runtime.CompilerServices;
using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using Encina.Cdc.PostgreSql;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NpgsqlTypes;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.PostgreSql;

/// <summary>
/// Unit tests that drive <see cref="PostgresCdcConnector"/> through a hand-written fake of
/// <see cref="IPostgresReplicationClient"/>: the mapping of replication records to change events,
/// the error paths, cancellation, position restore and the acknowledgement order.
/// </summary>
public sealed class PostgresCdcConnectorMappingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static NpgsqlLogSequenceNumber Lsn(ulong value) => new(value);

    private static Dictionary<string, string?> Row(params (string Column, string? Value)[] columns)
    {
        var row = new Dictionary<string, string?>();
        foreach (var (column, value) in columns)
        {
            row[column] = value;
        }

        return row;
    }

    private static PostgresCdcConnector CreateConnector(
        FakeReplicationClient client,
        PostgresCdcOptions? options = null,
        ICdcPositionStore? store = null,
        TimeProvider? timeProvider = null) =>
        new(
            options ?? new PostgresCdcOptions { CreatePublicationIfNotExists = false },
            store ?? StoreReturning(Option<CdcPosition>.None),
            NullLogger<PostgresCdcConnector>.Instance,
            client,
            timeProvider ?? new FakeTimeProvider(Now));

    private static ICdcPositionStore StoreReturning(Either<EncinaError, Option<CdcPosition>> result)
    {
        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));
        return store;
    }

    private static async Task<List<Either<EncinaError, ChangeEvent>>> CollectAsync(
        PostgresCdcConnector connector,
        CancellationToken cancellationToken)
    {
        var items = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var item in connector.StreamChangesAsync(cancellationToken))
        {
            items.Add(item);
        }

        return items;
    }

    private static ChangeEvent Right(Either<EncinaError, ChangeEvent> item) =>
        item.Match(e => e, Left: error => throw new InvalidOperationException($"Expected Right, got {error}"));

    private static EncinaError LeftOf(Either<EncinaError, ChangeEvent> item) =>
        item.Match(_ => default!, Left: e => e);

    private static string Json(object? element) => ((JsonElement)element!).GetRawText();

    #region Insert

    [Fact]
    public async Task Stream_InsertWithSchema_MapsTableNameAfterAndMetadata()
    {
        var client = new FakeReplicationClient(
            new InsertRecord(Lsn(0x16B3748), "public", "orders", Row(("id", "1"), ("name", "Ana"))));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        items.Count.ShouldBe(1);
        var change = Right(items[0]);
        change.TableName.ShouldBe("public.orders");
        change.Operation.ShouldBe(ChangeOperation.Insert);
        change.Before.ShouldBeNull();
        Json(change.After).ShouldBe("""{"id":"1","name":"Ana"}""");
        change.Metadata.Position.ShouldBeOfType<PostgresCdcPosition>().Lsn.ShouldBe(Lsn(0x16B3748));
        change.Metadata.CapturedAtUtc.ShouldBe(Now.UtcDateTime);
        change.Metadata.SourceSchema.ShouldBe("public");
        change.Metadata.TransactionId.ShouldBeNull();
        change.Metadata.SourceDatabase.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Stream_InsertWithoutSchema_UsesBareTableName(string? schema)
    {
        var client = new FakeReplicationClient(new InsertRecord(Lsn(10), schema, "orders", Row(("id", "1"))));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        Right(items.ShouldHaveSingleItem()).TableName.ShouldBe("orders");
    }

    [Fact]
    public async Task Stream_InsertWithNullColumn_SerializesJsonNull()
    {
        var client = new FakeReplicationClient(
            new InsertRecord(Lsn(10), "public", "orders", Row(("id", "1"), ("note", null))));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        Json(Right(items.ShouldHaveSingleItem()).After).ShouldBe("""{"id":"1","note":null}""");
    }

    [Fact]
    public async Task Stream_CapturedAtUtc_FollowsTheTimeProvider()
    {
        var clock = new FakeTimeProvider(Now);
        var client = new FakeReplicationClient(
            new InsertRecord(Lsn(10), "public", "orders", Row(("id", "1"))),
            new InsertRecord(Lsn(20), "public", "orders", Row(("id", "2"))));
        var connector = CreateConnector(client, timeProvider: clock);

        var items = new List<ChangeEvent>();
        await foreach (var item in connector.StreamChangesAsync(TestContext.Current.CancellationToken))
        {
            items.Add(Right(item));
            clock.Advance(TimeSpan.FromMinutes(5));
        }

        items[0].Metadata.CapturedAtUtc.ShouldBe(Now.UtcDateTime);
        items[1].Metadata.CapturedAtUtc.ShouldBe(Now.UtcDateTime.AddMinutes(5));
    }

    #endregion

    #region Update

    [Fact]
    public async Task Stream_UpdateWithOldImage_MapsBeforeAndAfter()
    {
        var client = new FakeReplicationClient(new UpdateRecord(
            Lsn(30), "public", "orders", Row(("id", "1"), ("name", "Ana")), Row(("id", "1"), ("name", "Bea"))));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        var change = Right(items.ShouldHaveSingleItem());
        change.Operation.ShouldBe(ChangeOperation.Update);
        change.TableName.ShouldBe("public.orders");
        Json(change.Before).ShouldBe("""{"id":"1","name":"Ana"}""");
        Json(change.After).ShouldBe("""{"id":"1","name":"Bea"}""");
        change.Metadata.Position.ShouldBeOfType<PostgresCdcPosition>().Lsn.ShouldBe(Lsn(30));
    }

    [Fact]
    public async Task Stream_UpdateWithoutOldImage_HasNullBefore()
    {
        var client = new FakeReplicationClient(
            new UpdateRecord(Lsn(30), "public", "orders", Before: null, Row(("id", "1"), ("name", "Bea"))));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        var change = Right(items.ShouldHaveSingleItem());
        change.Before.ShouldBeNull();
        Json(change.After).ShouldBe("""{"id":"1","name":"Bea"}""");
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Stream_DeleteWithOldImage_MapsBeforeAndNullAfter()
    {
        var client = new FakeReplicationClient(
            new DeleteRecord(Lsn(40), "sales", "orders", Row(("id", "1"))));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        var change = Right(items.ShouldHaveSingleItem());
        change.Operation.ShouldBe(ChangeOperation.Delete);
        change.TableName.ShouldBe("sales.orders");
        Json(change.Before).ShouldBe("""{"id":"1"}""");
        change.After.ShouldBeNull();
        change.Metadata.SourceSchema.ShouldBe("sales");
    }

    [Fact]
    public async Task Stream_DeleteWithoutOldImage_HasNullBeforeAndAfter()
    {
        var client = new FakeReplicationClient(new DeleteRecord(Lsn(40), "public", "orders", Before: null));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        var change = Right(items.ShouldHaveSingleItem());
        change.Before.ShouldBeNull();
        change.After.ShouldBeNull();
    }

    #endregion

    #region Ignored and failed records

    [Fact]
    public async Task Stream_RelationAndSkippedRecords_YieldNothingButAreAcknowledged()
    {
        var client = new FakeReplicationClient(
            new RelationRecord(Lsn(1)),
            new SkippedRecord(Lsn(2)),
            new InsertRecord(Lsn(3), "public", "orders", Row(("id", "1"))),
            new SkippedRecord(Lsn(4)));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        Right(items.ShouldHaveSingleItem()).Operation.ShouldBe(ChangeOperation.Insert);
        client.Acknowledged.ShouldBe([Lsn(1), Lsn(2), Lsn(3), Lsn(4)]);
    }

    [Fact]
    public async Task Stream_DecodeFailure_YieldsStreamInterruptedWithoutExceptionMessageAndContinues()
    {
        var failure = new InvalidOperationException("secret row content");
        var client = new FakeReplicationClient(
            new DecodeFailureRecord(Lsn(5), failure),
            new InsertRecord(Lsn(6), "public", "orders", Row(("id", "1"))));
        var connector = CreateConnector(client);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        items.Count.ShouldBe(2);
        var error = LeftOf(items[0]);
        error.Message.ShouldContain(CdcErrorCodes.StreamInterrupted);
        error.Message.ShouldNotContain("secret row content");
        error.Exception.IfNone(() => throw new InvalidOperationException("No inner exception")).ShouldBeSameAs(failure);
        Right(items[1]).Operation.ShouldBe(ChangeOperation.Insert);
        client.Acknowledged.ShouldBe([Lsn(5), Lsn(6)]);
    }

    [Fact]
    public async Task Stream_MappingThrows_YieldsStreamInterruptedWithoutExceptionMessage()
    {
        var client = new FakeReplicationClient(new InsertRecord(Lsn(5), "public", "orders", Row(("id", "1"))));
        var connector = CreateConnector(client, timeProvider: new ThrowingTimeProvider());

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        var error = LeftOf(items.ShouldHaveSingleItem());
        error.Message.ShouldContain(CdcErrorCodes.StreamInterrupted);
        error.Message.ShouldNotContain("clock secret");
    }

    #endregion

    #region Publication setup

    [Fact]
    public async Task Stream_PublicationEnabled_PassesNameAndTablesBeforeStreaming()
    {
        var options = new PostgresCdcOptions
        {
            CreatePublicationIfNotExists = true,
            PublicationName = "my_pub",
            PublicationTables = ["public.a", "public.b"],
        };
        var client = new FakeReplicationClient(new InsertRecord(Lsn(1), "public", "a", Row(("id", "1"))));
        var connector = CreateConnector(client, options);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        items.Count.ShouldBe(1);
        client.PublicationCalls.ShouldBe([("my_pub", "public.a,public.b")]);
    }

    [Fact]
    public async Task Stream_PublicationDisabled_DoesNotEnsurePublication()
    {
        var client = new FakeReplicationClient();
        var connector = CreateConnector(client);

        await CollectAsync(connector, TestContext.Current.CancellationToken);

        client.PublicationCalls.ShouldBeEmpty();
        client.StreamCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Stream_PublicationFails_YieldsSingleConnectionFailedLeftAndNeverStreams()
    {
        var options = new PostgresCdcOptions { CreatePublicationIfNotExists = true, PublicationName = "my_pub" };
        var client = new FakeReplicationClient { PublicationFailure = new InvalidOperationException("db secret") };
        var connector = CreateConnector(client, options);

        var items = await CollectAsync(connector, TestContext.Current.CancellationToken);

        var error = LeftOf(items.ShouldHaveSingleItem());
        error.Message.ShouldContain(CdcErrorCodes.ConnectionFailed);
        error.Message.ShouldContain("Failed to ensure publication 'my_pub' exists");
        error.Message.ShouldNotContain("db secret");
        client.StreamCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Stream_PublicationCancelled_PropagatesOperationCanceled()
    {
        var options = new PostgresCdcOptions { CreatePublicationIfNotExists = true };
        var client = new FakeReplicationClient { PublicationFailure = new OperationCanceledException() };
        var connector = CreateConnector(client, options);

        await Should.ThrowAsync<OperationCanceledException>(
            () => CollectAsync(connector, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Client failures and cancellation

    // Pins the behaviour tracked in #2779: a failure of the replication client escapes the
    // iterator instead of yielding a Left. Expect a Left when that issue is fixed.
    [Fact]
    public async Task Stream_ClientThrowsAfterRecords_DeliversEarlierEventsThenThrows()
    {
        var client = new FakeReplicationClient(new InsertRecord(Lsn(1), "public", "orders", Row(("id", "1"))))
        {
            StreamFailure = new InvalidOperationException("replication connection lost"),
        };
        var connector = CreateConnector(client);
        var delivered = new List<Either<EncinaError, ChangeEvent>>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in connector.StreamChangesAsync(TestContext.Current.CancellationToken))
            {
                delivered.Add(item);
            }
        });

        Right(delivered.ShouldHaveSingleItem()).Operation.ShouldBe(ChangeOperation.Insert);
    }

    [Fact]
    public async Task Stream_CancelledAfterFirstEvent_StopsWithoutMappingOrAcknowledgingTheNextRecord()
    {
        var client = new FakeReplicationClient(
            new InsertRecord(Lsn(1), "public", "orders", Row(("id", "1"))),
            new InsertRecord(Lsn(2), "public", "orders", Row(("id", "2"))));
        var connector = CreateConnector(client);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var delivered = new List<Either<EncinaError, ChangeEvent>>();

        await foreach (var item in connector.StreamChangesAsync(cts.Token))
        {
            delivered.Add(item);
            await cts.CancelAsync();
        }

        Right(delivered.ShouldHaveSingleItem()).Metadata.Position
            .ShouldBeOfType<PostgresCdcPosition>().Lsn.ShouldBe(Lsn(1));
        client.Acknowledged.ShouldBe([Lsn(1)]);
    }

    [Fact]
    public async Task Stream_AcknowledgesARecordOnlyAfterTheConsumerTookIt()
    {
        var client = new FakeReplicationClient(
            new InsertRecord(Lsn(1), "public", "orders", Row(("id", "1"))),
            new InsertRecord(Lsn(2), "public", "orders", Row(("id", "2"))));
        var connector = CreateConnector(client);

        await using var enumerator = connector.StreamChangesAsync(TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        client.Acknowledged.ShouldBeEmpty();

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        client.Acknowledged.ShouldBe([Lsn(1)]);

        (await enumerator.MoveNextAsync()).ShouldBeFalse();
        client.Acknowledged.ShouldBe([Lsn(1), Lsn(2)]);
    }

    #endregion

    #region Position restore

    [Fact]
    public async Task Stream_SavedPostgresPosition_StartsTheClientFromThatLsn()
    {
        var saved = new PostgresCdcPosition(Lsn(0x16B3748));
        var client = new FakeReplicationClient();
        var connector = CreateConnector(client, store: StoreReturning(Option<CdcPosition>.Some(saved)));

        await CollectAsync(connector, TestContext.Current.CancellationToken);

        client.StartLsn.ShouldBe(Lsn(0x16B3748));
    }

    [Fact]
    public async Task Stream_NoSavedPosition_StartsTheClientWithoutLsn()
    {
        var client = new FakeReplicationClient();
        var connector = CreateConnector(client);

        await CollectAsync(connector, TestContext.Current.CancellationToken);

        client.StartLsn.ShouldBeNull();
    }

    [Fact]
    public async Task Stream_ForeignSavedPosition_IsIgnored()
    {
        var client = new FakeReplicationClient();
        var foreign = Substitute.For<CdcPosition>();
        var connector = CreateConnector(client, store: StoreReturning(Option<CdcPosition>.Some(foreign)));

        await CollectAsync(connector, TestContext.Current.CancellationToken);

        client.StartLsn.ShouldBeNull();
    }

    [Fact]
    public async Task Stream_PositionStoreReturnsLeft_StartsWithoutLsn()
    {
        var client = new FakeReplicationClient();
        var store = StoreReturning(Either<EncinaError, Option<CdcPosition>>.Left(EncinaError.New("store down")));
        var connector = CreateConnector(client, store: store);

        await CollectAsync(connector, TestContext.Current.CancellationToken);

        client.StartLsn.ShouldBeNull();
        client.StreamCalls.ShouldBe(1);
    }

    #endregion

    #region GetCurrentPositionAsync

    [Fact]
    public async Task GetCurrentPositionAsync_ValidLsn_ReturnsPostgresPosition()
    {
        var client = new FakeReplicationClient { CurrentLsn = "0/16B3748" };
        var connector = CreateConnector(client);

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        var position = result.Match(p => p, Left: e => throw new InvalidOperationException(e.Message));
        position.ShouldBeOfType<PostgresCdcPosition>().Lsn.ShouldBe(Lsn(0x16B3748));
    }

    [Fact]
    public async Task GetCurrentPositionAsync_NullLsn_ReturnsConnectionFailed()
    {
        var client = new FakeReplicationClient { CurrentLsn = null };
        var connector = CreateConnector(client);

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        var error = result.Match(_ => default!, Left: e => e);
        error.Message.ShouldContain(CdcErrorCodes.ConnectionFailed);
        error.Message.ShouldContain("wal_level = logical");
    }

    [Fact]
    public async Task GetCurrentPositionAsync_UnparsableLsn_ReturnsConnectionFailedWithoutExceptionMessage()
    {
        var client = new FakeReplicationClient { CurrentLsn = "not-an-lsn" };
        var connector = CreateConnector(client);

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        var error = result.Match(_ => default!, Left: e => e);
        error.Message.ShouldContain(CdcErrorCodes.ConnectionFailed);
        var exception = error.Exception.IfNone(() => throw new InvalidOperationException("No inner exception"));
        error.Message.ShouldNotContain(exception.Message);
    }

    [Fact]
    public async Task GetCurrentPositionAsync_ClientThrows_ReturnsConnectionFailedWithoutExceptionMessage()
    {
        var client = new FakeReplicationClient { LsnFailure = new InvalidOperationException("db secret") };
        var connector = CreateConnector(client);

        var result = await connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken);

        var error = result.Match(_ => default!, Left: e => e);
        error.Message.ShouldContain("Failed to connect to PostgreSQL for CDC");
        error.Message.ShouldNotContain("db secret");
    }

    [Fact]
    public async Task GetCurrentPositionAsync_ClientCancelled_PropagatesOperationCanceled()
    {
        var client = new FakeReplicationClient { LsnFailure = new OperationCanceledException() };
        var connector = CreateConnector(client);

        await Should.ThrowAsync<OperationCanceledException>(
            () => connector.GetCurrentPositionAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    private sealed class ThrowingTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("clock secret");
    }

    private sealed class FakeReplicationClient : IPostgresReplicationClient
    {
        private readonly ReplicationRecord[] _records;

        public FakeReplicationClient(params ReplicationRecord[] records) => _records = records;

        public string? CurrentLsn { get; init; }

        public Exception? LsnFailure { get; init; }

        public Exception? PublicationFailure { get; init; }

        public Exception? StreamFailure { get; init; }

        public List<(string Name, string Tables)> PublicationCalls { get; } = [];

        public List<NpgsqlLogSequenceNumber> Acknowledged { get; } = [];

        public int StreamCalls { get; private set; }

        public NpgsqlLogSequenceNumber? StartLsn { get; private set; }

        public Task<string?> GetCurrentWalLsnAsync(CancellationToken cancellationToken) =>
            LsnFailure is null ? Task.FromResult(CurrentLsn) : Task.FromException<string?>(LsnFailure);

        public Task EnsurePublicationAsync(string publicationName, string[] tables, CancellationToken cancellationToken)
        {
            PublicationCalls.Add((publicationName, string.Join(",", tables)));
            return PublicationFailure is null ? Task.CompletedTask : Task.FromException(PublicationFailure);
        }

        public async IAsyncEnumerable<ReplicationRecord> StreamAsync(
            NpgsqlLogSequenceNumber? startLsn,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            StreamCalls++;
            StartLsn = startLsn;

            foreach (var record in _records)
            {
                yield return record;

                // Same order as the Npgsql-backed client: acknowledge once the consumer resumed.
                Acknowledged.Add(record.WalEnd);
            }

            await Task.Yield();

            if (StreamFailure is not null)
            {
                throw StreamFailure;
            }
        }
    }
}
