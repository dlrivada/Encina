using System.Net;
using System.Reflection;
using Encina.Messaging.DeadLetter;
using Encina.MongoDB;
using Encina.MongoDB.DeadLetter;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.MongoDB.DeadLetter;

/// <summary>
/// Unit tests for <see cref="DeadLetterStoreMongoDB"/> against substituted driver interfaces.
/// </summary>
public sealed class DeadLetterStoreMongoDBTests
{
    private static readonly DateTimeOffset FakeNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IMongoClient _client = Substitute.For<IMongoClient>();
    private readonly IMongoDatabase _database = Substitute.For<IMongoDatabase>();
    private readonly IMongoCollection<DeadLetterMessage> _collection = Substitute.For<IMongoCollection<DeadLetterMessage>>();
    private readonly FakeTimeProvider _time = new(FakeNow);
    private readonly DeadLetterStoreMongoDB _store;

    public DeadLetterStoreMongoDBTests()
    {
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(_database);
        _database.GetCollection<DeadLetterMessage>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(_collection);
        _store = new DeadLetterStoreMongoDB(
            _client,
            Options.Create(new EncinaMongoDbOptions { DatabaseName = "TestDb" }),
            Substitute.For<ILogger<DeadLetterStoreMongoDB>>(),
            _time);
    }

    #region Constructor

    [Fact]
    public void Constructor_NullMongoClient_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterStoreMongoDB(
            null!, Options.Create(new EncinaMongoDbOptions()), Substitute.For<ILogger<DeadLetterStoreMongoDB>>()))
            .ParamName.ShouldBe("mongoClient");
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterStoreMongoDB(
            _client, null!, Substitute.For<ILogger<DeadLetterStoreMongoDB>>()))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new DeadLetterStoreMongoDB(
            _client, Options.Create(new EncinaMongoDbOptions()), null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ResolvesConfiguredDatabaseAndCollection()
    {
        var options = new EncinaMongoDbOptions { DatabaseName = "Custom" };
        options.Collections.DeadLetterMessages = "custom_dlq";

        _ = new DeadLetterStoreMongoDB(_client, Options.Create(options), Substitute.For<ILogger<DeadLetterStoreMongoDB>>());

        _client.Received(1).GetDatabase("Custom", Arg.Any<MongoDatabaseSettings>());
        _database.Received(1).GetCollection<DeadLetterMessage>("custom_dlq", Arg.Any<MongoCollectionSettings>());
    }

    [Fact]
    public async Task Constructor_WithoutTimeProvider_UsesSystemClock()
    {
        var store = new DeadLetterStoreMongoDB(
            _client, Options.Create(new EncinaMongoDbOptions()), Substitute.For<ILogger<DeadLetterStoreMongoDB>>());
        UpdateDefinition<DeadLetterMessage>? update = null;
        _collection.UpdateOneAsync(
                Arg.Any<FilterDefinition<DeadLetterMessage>>(),
                Arg.Do<UpdateDefinition<DeadLetterMessage>>(u => update = u),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(1, 1, null));
        var before = DateTime.UtcNow;

        await store.TryClaimForReplayAsync(Guid.NewGuid(), before);

        var claimed = Render(update!)["$set"]["replayClaimedAtUtc"].ToUniversalTime();
        claimed.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        claimed.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_NullMessage_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => _store.AddAsync(null!));

        ex.ParamName.ShouldBe("message");
    }

    [Fact]
    public async Task AddAsync_EmptySourceMessageId_Throws()
    {
        var message = new DeadLetterMessage { SourceMessageId = string.Empty };

        await Should.ThrowAsync<ArgumentException>(() => _store.AddAsync(message));
    }

    [Fact]
    public async Task AddAsync_DeadLetterMessage_InsertsSameInstanceAndReturnsTrue()
    {
        var message = NewMessage();
        DeadLetterMessage? inserted = null;
        using var cts = new CancellationTokenSource();
        _collection.InsertOneAsync(
                Arg.Do<DeadLetterMessage>(m => inserted = m),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _store.AddAsync(message, cts.Token);

        RightOf(result).ShouldBeTrue();
        inserted.ShouldBeSameAs(message);
        await _collection.Received(1).InsertOneAsync(
            message, Arg.Any<InsertOneOptions>(), cts.Token);
    }

    [Fact]
    public async Task AddAsync_ForeignMessage_InsertsCopyOfEveryField()
    {
        var source = NewMessage();
        source.ReplayClaimedAtUtc = FakeNow.UtcDateTime.AddMinutes(1);
        source.ReplayedAtUtc = FakeNow.UtcDateTime.AddMinutes(2);
        source.ReplayResult = "success";
        var foreign = Substitute.For<IDeadLetterMessage>();
        CopyInto(foreign, source);
        DeadLetterMessage? inserted = null;
        _collection.InsertOneAsync(
                Arg.Do<DeadLetterMessage>(m => inserted = m),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _store.AddAsync(foreign);

        RightOf(result).ShouldBeTrue();
        inserted.ShouldNotBeNull();
        inserted.ShouldNotBeSameAs(source);
        inserted.ToBsonDocument().ShouldBe(source.ToBsonDocument());
    }

    [Fact]
    public async Task AddAsync_DuplicateKey_ReturnsFalse()
    {
        _collection.InsertOneAsync(
                Arg.Any<DeadLetterMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(DuplicateKeyException());

        var result = await _store.AddAsync(NewMessage());

        RightOf(result).ShouldBeFalse();
    }

    [Fact]
    public async Task AddAsync_OtherWriteError_ReturnsLeftStoreFailed()
    {
        _collection.InsertOneAsync(
                Arg.Any<DeadLetterMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(WriteException(ServerErrorCategory.ExecutionTimeout));

        var result = await _store.AddAsync(NewMessage());

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.StoreFailed);
    }

    [Fact]
    public async Task AddAsync_DriverThrows_ReturnsLeftStoreFailed()
    {
        _collection.InsertOneAsync(
                Arg.Any<DeadLetterMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _store.AddAsync(NewMessage());

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.StoreFailed);
    }

    [Fact]
    public async Task AddAsync_Cancelled_Propagates()
    {
        _collection.InsertOneAsync(
                Arg.Any<DeadLetterMessage>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.AddAsync(NewMessage()));
    }

    #endregion

    #region GetAsync

    [Fact]
    public async Task GetAsync_EmptyGuid_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _store.GetAsync(Guid.Empty));

        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task GetAsync_Found_ReturnsSomeAndFiltersById()
    {
        var message = NewMessage();
        var capture = ArrangeFind(message);

        var result = await _store.GetAsync(message.Id);

        var option = RightOf(result);
        option.IsSome.ShouldBeTrue();
        option.IfSome(m => m.ShouldBeSameAs(message));
        Render(capture.Filter!).ToString().ShouldContain(message.Id.ToString());
        capture.Options!.Limit.ShouldBe(1);
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNone()
    {
        ArrangeFind();

        var result = await _store.GetAsync(Guid.NewGuid());

        RightOf(result).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_DriverThrows_ReturnsLeftGetFailed()
    {
        _collection.FindAsync(
                Arg.Any<FilterDefinition<DeadLetterMessage>>(),
                Arg.Any<FindOptions<DeadLetterMessage, DeadLetterMessage>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException());

        var result = await _store.GetAsync(Guid.NewGuid());

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.GetFailed);
    }

    #endregion

    #region GetMessagesAsync

    [Fact]
    public async Task GetMessagesAsync_NegativeSkip_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _store.GetMessagesAsync(skip: -1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(DeadLetterStoreLimits.MaxPageSize + 1)]
    public async Task GetMessagesAsync_TakeOutOfRange_Throws(int take)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _store.GetMessagesAsync(take: take));
    }

    [Fact]
    public async Task GetMessagesAsync_NullFilter_UsesEmptyFilterAscendingOrder()
    {
        var first = NewMessage();
        var second = NewMessage();
        var capture = ArrangeFind(first, second);

        var result = await _store.GetMessagesAsync(skip: 5, take: 20);

        RightOf(result).ToList().ShouldBe([first, second]);
        Render(capture.Filter!).ShouldBe(new BsonDocument());
        capture.Options!.Skip.ShouldBe(5);
        capture.Options.Limit.ShouldBe(20);
        var sort = RenderSort(capture.Options.Sort!);
        sort["deadLetteredAtUtc"].ShouldBe(new BsonInt32(1));
        sort["_id"].ShouldBe(new BsonInt32(1));
    }

    [Fact]
    public async Task GetMessagesAsync_NewestFirst_SortsDescending()
    {
        var capture = ArrangeFind();

        await _store.GetMessagesAsync(newestFirst: true);

        var sort = RenderSort(capture.Options!.Sort!);
        sort["deadLetteredAtUtc"].ShouldBe(new BsonInt32(-1));
        sort["_id"].ShouldBe(new BsonInt32(-1));
    }

    [Fact]
    public async Task GetMessagesAsync_AllFilterFields_AreSentToTheDriver()
    {
        var capture = ArrangeFind();
        var after = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var expires = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = new DeadLetterFilter
        {
            SourcePattern = "Outbox",
            RequestType = "Orders.Create",
            ErrorCode = "orders.failed",
            CorrelationId = "corr-1",
            TenantId = "tenant-1",
            SourceMessageId = "src-1",
            ExcludeReplayed = true,
            DeadLetteredAfterUtc = after,
            DeadLetteredBeforeUtc = before,
            ExpiresAtOrBeforeUtc = expires
        };

        await _store.GetMessagesAsync(filter);

        var json = Render(capture.Filter!).ToString();
        json.ShouldContain("\"sourcePattern\" : \"Outbox\"");
        json.ShouldContain("\"requestType\" : \"Orders.Create\"");
        json.ShouldContain("\"errorCode\" : \"orders.failed\"");
        json.ShouldContain("\"correlationId\" : \"corr-1\"");
        json.ShouldContain("\"tenantId\" : \"tenant-1\"");
        json.ShouldContain("\"sourceMessageId\" : \"src-1\"");
        json.ShouldContain("\"replayedAtUtc\" : null");
        json.ShouldContain("\"$gte\"");
        json.ShouldContain("\"$lte\"");
        json.ShouldContain("\"$ne\" : null");
        json.ShouldContain("deadLetteredAtUtc");
        json.ShouldContain("expiresAtUtc");
    }

    [Fact]
    public async Task GetMessagesAsync_ExcludeReplayedFalse_FiltersReplayedOnly()
    {
        var capture = ArrangeFind();

        await _store.GetMessagesAsync(new DeadLetterFilter { ExcludeReplayed = false });

        Render(capture.Filter!).ShouldBe(
            new BsonDocument("replayedAtUtc", new BsonDocument("$ne", BsonNull.Value)));
    }

    [Fact]
    public async Task GetMessagesAsync_EmptyFilterValues_MatchEverything()
    {
        var capture = ArrangeFind();

        await _store.GetMessagesAsync(new DeadLetterFilter { SourcePattern = "", TenantId = null });

        Render(capture.Filter!).ShouldBe(new BsonDocument());
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public async Task GetMessagesAsync_InstantOfAnyKind_IsSentAsUtc(DateTimeKind kind)
    {
        var capture = ArrangeFind();
        var utc = new DateTime(2026, 1, 1, 8, 30, 0, DateTimeKind.Utc);
        var instant = kind switch
        {
            DateTimeKind.Utc => utc,
            DateTimeKind.Local => utc.ToLocalTime(),
            _ => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)
        };

        await _store.GetMessagesAsync(new DeadLetterFilter { DeadLetteredAfterUtc = instant });

        Render(capture.Filter!)["deadLetteredAtUtc"]["$gte"].ToUniversalTime().ShouldBe(utc);
    }

    [Fact]
    public async Task GetMessagesAsync_DriverThrows_ReturnsLeftQueryFailed()
    {
        _collection.FindAsync(
                Arg.Any<FilterDefinition<DeadLetterMessage>>(),
                Arg.Any<FindOptions<DeadLetterMessage, DeadLetterMessage>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        var result = await _store.GetMessagesAsync();

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.QueryFailed);
    }

    #endregion

    #region GetCountAsync

    [Fact]
    public async Task GetCountAsync_ReturnsDriverCountForFilter()
    {
        FilterDefinition<DeadLetterMessage>? sent = null;
        _collection.CountDocumentsAsync(
                Arg.Do<FilterDefinition<DeadLetterMessage>>(f => sent = f),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(7L);

        var result = await _store.GetCountAsync(new DeadLetterFilter { SourcePattern = "Inbox" });

        RightOf(result).ShouldBe(7);
        Render(sent!).ShouldBe(new BsonDocument("sourcePattern", "Inbox"));
    }

    [Fact]
    public async Task GetCountAsync_NullFilter_SendsEmptyFilter()
    {
        FilterDefinition<DeadLetterMessage>? sent = null;
        _collection.CountDocumentsAsync(
                Arg.Do<FilterDefinition<DeadLetterMessage>>(f => sent = f),
                Arg.Any<CountOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(0L);

        var result = await _store.GetCountAsync();

        RightOf(result).ShouldBe(0);
        Render(sent!).ShouldBe(new BsonDocument());
    }

    [Fact]
    public async Task GetCountAsync_DriverThrows_ReturnsLeftQueryFailed()
    {
        _collection.CountDocumentsAsync(
                Arg.Any<FilterDefinition<DeadLetterMessage>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        var result = await _store.GetCountAsync();

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.QueryFailed);
    }

    #endregion

    #region TryClaimForReplayAsync

    [Fact]
    public async Task TryClaimForReplayAsync_EmptyGuid_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => _store.TryClaimForReplayAsync(Guid.Empty, FakeNow.UtcDateTime));
    }

    [Fact]
    public async Task TryClaimForReplayAsync_Matched_ReturnsTrueAndSetsClaimToNow()
    {
        var id = Guid.NewGuid();
        var staleBefore = FakeNow.UtcDateTime.AddMinutes(-5);
        var capture = ArrangeUpdate(matched: 1);

        var result = await _store.TryClaimForReplayAsync(id, staleBefore);

        RightOf(result).ShouldBeTrue();
        var filter = Render(capture.Filter!).ToString();
        filter.ShouldContain(id.ToString());
        filter.ShouldContain("\"replayedAtUtc\" : null");
        filter.ShouldContain("\"replayClaimedAtUtc\" : null");
        filter.ShouldContain("\"$lte\"");
        Render(capture.Update!)["$set"]["replayClaimedAtUtc"].ToUniversalTime().ShouldBe(FakeNow.UtcDateTime);
    }

    [Fact]
    public async Task TryClaimForReplayAsync_AdvancedClock_ClaimsAtTheNewInstant()
    {
        var capture = ArrangeUpdate(matched: 1);
        _time.Advance(TimeSpan.FromMinutes(10));

        await _store.TryClaimForReplayAsync(Guid.NewGuid(), FakeNow.UtcDateTime);

        Render(capture.Update!)["$set"]["replayClaimedAtUtc"].ToUniversalTime()
            .ShouldBe(FakeNow.UtcDateTime.AddMinutes(10));
    }

    [Fact]
    public async Task TryClaimForReplayAsync_NotMatched_ReturnsFalse()
    {
        ArrangeUpdate(matched: 0);

        var result = await _store.TryClaimForReplayAsync(Guid.NewGuid(), FakeNow.UtcDateTime);

        RightOf(result).ShouldBeFalse();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_DriverThrows_ReturnsLeftClaimFailed()
    {
        ArrangeUpdateThrows(new InvalidOperationException());

        var result = await _store.TryClaimForReplayAsync(Guid.NewGuid(), FakeNow.UtcDateTime);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.ClaimFailed);
    }

    #endregion

    #region MarkAsReplayedAsync

    [Fact]
    public async Task MarkAsReplayedAsync_EmptyGuid_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _store.MarkAsReplayedAsync(Guid.Empty, "success"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task MarkAsReplayedAsync_BlankResult_Throws(string result)
    {
        await Should.ThrowAsync<ArgumentException>(() => _store.MarkAsReplayedAsync(Guid.NewGuid(), result));
    }

    [Fact]
    public async Task MarkAsReplayedAsync_Matched_ReturnsTrueAndSetsReplayState()
    {
        var id = Guid.NewGuid();
        var capture = ArrangeUpdate(matched: 1);

        var result = await _store.MarkAsReplayedAsync(id, "success");

        RightOf(result).ShouldBeTrue();
        var filter = Render(capture.Filter!).ToString();
        filter.ShouldContain(id.ToString());
        filter.ShouldContain("\"replayedAtUtc\" : null");
        var set = Render(capture.Update!)["$set"];
        set["replayedAtUtc"].ToUniversalTime().ShouldBe(FakeNow.UtcDateTime);
        set["replayResult"].AsString.ShouldBe("success");
    }

    [Fact]
    public async Task MarkAsReplayedAsync_NotMatched_ReturnsFalse()
    {
        ArrangeUpdate(matched: 0);

        var result = await _store.MarkAsReplayedAsync(Guid.NewGuid(), "success");

        RightOf(result).ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsReplayedAsync_DriverThrows_ReturnsLeftMarkReplayedFailed()
    {
        ArrangeUpdateThrows(new InvalidOperationException());

        var result = await _store.MarkAsReplayedAsync(Guid.NewGuid(), "success");

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.MarkReplayedFailed);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_EmptyGuid_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _store.DeleteAsync(Guid.Empty));
    }

    [Theory]
    [InlineData(1L, true)]
    [InlineData(0L, false)]
    public async Task DeleteAsync_ReturnsWhetherADocumentWasDeleted(long deleted, bool expected)
    {
        var id = Guid.NewGuid();
        FilterDefinition<DeadLetterMessage>? sent = null;
        using var cts = new CancellationTokenSource();
        _collection.DeleteOneAsync(Arg.Do<FilterDefinition<DeadLetterMessage>>(f => sent = f), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult.Acknowledged(deleted));

        var result = await _store.DeleteAsync(id, cts.Token);

        RightOf(result).ShouldBe(expected);
        Render(sent!).ToString().ShouldContain(id.ToString());
        await _collection.Received(1).DeleteOneAsync(Arg.Any<FilterDefinition<DeadLetterMessage>>(), cts.Token);
    }

    [Fact]
    public async Task DeleteAsync_DriverThrows_ReturnsLeftDeleteFailed()
    {
        _collection.DeleteOneAsync(Arg.Any<FilterDefinition<DeadLetterMessage>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        var result = await _store.DeleteAsync(Guid.NewGuid());

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.DeleteFailed);
    }

    [Fact]
    public async Task DeleteAsync_Cancelled_Propagates()
    {
        _collection.DeleteOneAsync(Arg.Any<FilterDefinition<DeadLetterMessage>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _store.DeleteAsync(Guid.NewGuid()));
    }

    #endregion

    #region DeleteManyAsync

    [Fact]
    public async Task DeleteManyAsync_NullFilter_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => _store.DeleteManyAsync(null!));

        ex.ParamName.ShouldBe("filter");
    }

    [Fact]
    public async Task DeleteManyAsync_ReturnsDeletedCountForFilter()
    {
        FilterDefinition<DeadLetterMessage>? sent = null;
        _collection.DeleteManyAsync(Arg.Do<FilterDefinition<DeadLetterMessage>>(f => sent = f), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult.Acknowledged(3));

        var result = await _store.DeleteManyAsync(new DeadLetterFilter { ErrorCode = "x" });

        RightOf(result).ShouldBe(3);
        Render(sent!).ShouldBe(new BsonDocument("errorCode", "x"));
    }

    [Fact]
    public async Task DeleteManyAsync_DriverThrows_ReturnsLeftDeleteFailed()
    {
        _collection.DeleteManyAsync(Arg.Any<FilterDefinition<DeadLetterMessage>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        var result = await _store.DeleteManyAsync(DeadLetterFilter.All);

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.DeleteFailed);
    }

    #endregion

    #region DeleteExpiredAsync

    [Fact]
    public async Task DeleteExpiredAsync_DeletesMessagesExpiringAtOrBeforeNow()
    {
        FilterDefinition<DeadLetterMessage>? sent = null;
        _collection.DeleteManyAsync(Arg.Do<FilterDefinition<DeadLetterMessage>>(f => sent = f), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult.Acknowledged(2));

        var result = await _store.DeleteExpiredAsync();

        RightOf(result).ShouldBe(2);
        var expires = Render(sent!).ToString();
        expires.ShouldContain("\"$ne\" : null");
        expires.ShouldContain("\"$lte\"");
        expires.ShouldContain("expiresAtUtc");
        _time.Advance(TimeSpan.FromHours(1));
        await _store.DeleteExpiredAsync();
        Render(sent!).ToString().ShouldNotBe(expires);
    }

    [Fact]
    public async Task DeleteExpiredAsync_DriverThrows_ReturnsLeftCleanupFailed()
    {
        _collection.DeleteManyAsync(Arg.Any<FilterDefinition<DeadLetterMessage>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        var result = await _store.DeleteExpiredAsync();

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.CleanupFailed);
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_ReturnsUnitWithoutTouchingTheCollection()
    {
        var result = await _store.SaveChangesAsync();

        result.IsRight.ShouldBeTrue();
        _collection.ReceivedCalls().ShouldBeEmpty();
    }

    #endregion

    #region Helpers

    private sealed class FindCapture
    {
        public FilterDefinition<DeadLetterMessage>? Filter { get; set; }

        public FindOptions<DeadLetterMessage, DeadLetterMessage>? Options { get; set; }
    }

    private sealed class UpdateCapture
    {
        public FilterDefinition<DeadLetterMessage>? Filter { get; set; }

        public UpdateDefinition<DeadLetterMessage>? Update { get; set; }
    }

    private FindCapture ArrangeFind(params DeadLetterMessage[] documents)
    {
        var capture = new FindCapture();
        var cursor = Substitute.For<IAsyncCursor<DeadLetterMessage>>();
        var consumed = false;
        cursor.Current.Returns(_ => documents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var hasMore = !consumed && documents.Length > 0;
            consumed = true;
            return hasMore;
        });
        _collection.FindAsync(
                Arg.Do<FilterDefinition<DeadLetterMessage>>(f => capture.Filter = f),
                Arg.Do<FindOptions<DeadLetterMessage, DeadLetterMessage>>(o => capture.Options = o),
                Arg.Any<CancellationToken>())
            .Returns(cursor);
        return capture;
    }

    private UpdateCapture ArrangeUpdate(long matched)
    {
        var capture = new UpdateCapture();
        _collection.UpdateOneAsync(
                Arg.Do<FilterDefinition<DeadLetterMessage>>(f => capture.Filter = f),
                Arg.Do<UpdateDefinition<DeadLetterMessage>>(u => capture.Update = u),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateResult.Acknowledged(matched, matched, null));
        return capture;
    }

    private void ArrangeUpdateThrows(Exception exception)
        => _collection.UpdateOneAsync(
                Arg.Any<FilterDefinition<DeadLetterMessage>>(),
                Arg.Any<UpdateDefinition<DeadLetterMessage>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

    private static DeadLetterMessage NewMessage() => new()
    {
        Id = Guid.NewGuid(),
        RequestType = "Orders.Create",
        RequestContent = "{}",
        ErrorCode = "orders.failed",
        ExceptionType = "System.Exception",
        ExceptionStackTrace = "stack",
        CorrelationId = "corr-1",
        SourcePattern = "Outbox",
        SourceMessageId = Guid.NewGuid().ToString(),
        TenantId = "tenant-1",
        TotalRetryAttempts = 3,
        FirstFailedAtUtc = FakeNow.UtcDateTime.AddHours(-1),
        DeadLetteredAtUtc = FakeNow.UtcDateTime,
        ExpiresAtUtc = FakeNow.UtcDateTime.AddDays(7)
    };

    private static void CopyInto(IDeadLetterMessage target, DeadLetterMessage source)
    {
        target.Id.Returns(source.Id);
        target.RequestType.Returns(source.RequestType);
        target.RequestContent.Returns(source.RequestContent);
        target.ErrorCode.Returns(source.ErrorCode);
        target.ExceptionType.Returns(source.ExceptionType);
        target.ExceptionStackTrace.Returns(source.ExceptionStackTrace);
        target.CorrelationId.Returns(source.CorrelationId);
        target.SourcePattern.Returns(source.SourcePattern);
        target.SourceMessageId.Returns(source.SourceMessageId);
        target.TenantId.Returns(source.TenantId);
        target.TotalRetryAttempts.Returns(source.TotalRetryAttempts);
        target.FirstFailedAtUtc.Returns(source.FirstFailedAtUtc);
        target.DeadLetteredAtUtc.Returns(source.DeadLetteredAtUtc);
        target.ExpiresAtUtc.Returns(source.ExpiresAtUtc);
        target.ReplayClaimedAtUtc.Returns(source.ReplayClaimedAtUtc);
        target.ReplayedAtUtc.Returns(source.ReplayedAtUtc);
        target.ReplayResult.Returns(source.ReplayResult);
    }

    private static BsonDocument Render(FilterDefinition<DeadLetterMessage> filter)
    {
        var registry = BsonSerializer.SerializerRegistry;
        return filter.Render(new RenderArgs<DeadLetterMessage>(registry.GetSerializer<DeadLetterMessage>(), registry));
    }

    private static BsonDocument Render(UpdateDefinition<DeadLetterMessage> update)
    {
        var registry = BsonSerializer.SerializerRegistry;
        return update.Render(new RenderArgs<DeadLetterMessage>(registry.GetSerializer<DeadLetterMessage>(), registry)).AsBsonDocument;
    }

    private static BsonDocument RenderSort(SortDefinition<DeadLetterMessage> sort)
    {
        var registry = BsonSerializer.SerializerRegistry;
        return sort.Render(new RenderArgs<DeadLetterMessage>(registry.GetSerializer<DeadLetterMessage>(), registry));
    }

    private static T RightOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: value => value, Left: error => throw new InvalidOperationException($"Expected Right, got {error.GetCode()}"));

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left, got Right"),
            Left: error => error.GetCode().IfNone(string.Empty));

    private static MongoWriteException DuplicateKeyException() => WriteException(ServerErrorCategory.DuplicateKey);

    private static MongoWriteException WriteException(ServerErrorCategory category)
    {
        // WriteError has no public constructor; the driver builds it from a server reply.
        var writeError = (WriteError)Activator.CreateInstance(
            typeof(WriteError),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [category, 11000, "duplicate key", new BsonDocument()],
            culture: null)!;
        var connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)));
        return new MongoWriteException(connectionId, writeError, null, null);
    }

    #endregion
}
