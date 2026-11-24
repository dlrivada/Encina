using System.Data.Common;
using Encina.Messaging.DeadLetter;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;
using Store = global::Encina.ADO.MySQL.DeadLetter.DeadLetterStoreADO;
using StoreMessage = global::Encina.ADO.MySQL.DeadLetter.DeadLetterMessage;

namespace Encina.UnitTests.ADO.MySQL.DeadLetter;

/// <summary>
/// Runs the MySQL ADO.NET dead letter store over a SQLite in-memory stand-in for the statements that are
/// portable (<c>LIMIT/OFFSET</c> is valid SQLite too), and over a connection that throws for the <c>Left</c>
/// paths. SQLite is a relational stand-in only: the <c>MySqlException</c> duplicate-key catch of <c>AddAsync</c>
/// cannot run on it and is covered by the ADO-MySQL integration collection.
/// </summary>
public sealed class DeadLetterStoreADOTests : IAsyncDisposable
{
    private static readonly DateTime Now = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task AddAsync_NewMessage_PersistsEveryField()
    {
        var store = await CreateStoreAsync();
        var message = Message("a", "Outbox", Now.AddDays(-1));
        message.ExceptionType = "System.TimeoutException";
        message.ExceptionStackTrace = "at Handler";
        message.CorrelationId = "corr-a";
        message.TenantId = "tenant-a";
        message.ExpiresAtUtc = Now.AddDays(3);
        message.ReplayClaimedAtUtc = Now.AddHours(-3);
        message.ReplayedAtUtc = Now.AddHours(-2);
        message.ReplayResult = "success";

        Right(await store.AddAsync(message)).ShouldBeTrue();

        var stored = Right(await store.GetAsync(message.Id)).ShouldBeSome();
        stored.Id.ShouldBe(message.Id);
        stored.RequestType.ShouldBe("OrderCommand");
        stored.RequestContent.ShouldBe("{}");
        stored.ErrorCode.ShouldBe("order.failed");
        stored.ExceptionType.ShouldBe("System.TimeoutException");
        stored.ExceptionStackTrace.ShouldBe("at Handler");
        stored.CorrelationId.ShouldBe("corr-a");
        stored.SourcePattern.ShouldBe("Outbox");
        stored.SourceMessageId.ShouldBe("a");
        stored.TenantId.ShouldBe("tenant-a");
        stored.TotalRetryAttempts.ShouldBe(3);
        stored.FirstFailedAtUtc.ShouldBe(Now.AddDays(-1).AddHours(-1));
        stored.DeadLetteredAtUtc.ShouldBe(Now.AddDays(-1));
        stored.ExpiresAtUtc.ShouldBe(Now.AddDays(3));
        stored.ReplayClaimedAtUtc.ShouldBe(Now.AddHours(-3));
        stored.ReplayedAtUtc.ShouldBe(Now.AddHours(-2));
        stored.ReplayResult.ShouldBe("success");
        stored.DeadLetteredAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public async Task AddAsync_MessageWithoutOptionalFields_ReadsBackNulls()
    {
        var store = await CreateStoreAsync();
        var message = Message("a", "Outbox", Now);

        await store.AddAsync(message);

        var stored = Right(await store.GetAsync(message.Id)).ShouldBeSome();
        stored.ExceptionType.ShouldBeNull();
        stored.CorrelationId.ShouldBeNull();
        stored.TenantId.ShouldBeNull();
        stored.ExpiresAtUtc.ShouldBeNull();
        stored.ReplayClaimedAtUtc.ShouldBeNull();
        stored.ReplayedAtUtc.ShouldBeNull();
        stored.ReplayResult.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_CustomTableName_UsesThatTable()
    {
        var store = await CreateStoreAsync(tableName: "main.CustomLetters");
        var message = Message("a", "Outbox", Now);

        Right(await store.AddAsync(message)).ShouldBeTrue();

        await using var count = _connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM CustomLetters";
        ((long)(await count.ExecuteScalarAsync())!).ShouldBe(1);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNone()
    {
        var store = await CreateStoreAsync();

        Right(await store.GetAsync(Guid.NewGuid())).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCountAsync_WithoutFilter_CountsEveryMessage()
    {
        var store = await CreateStoreAsync();
        await SeedAsync(store);

        Right(await store.GetCountAsync()).ShouldBe(3);
    }

    public static TheoryData<string, Func<DeadLetterFilter>, int> Filters => new()
    {
        { "SourcePattern", () => new DeadLetterFilter { SourcePattern = "Outbox" }, 2 },
        { "RequestType", () => new DeadLetterFilter { RequestType = "PayCommand" }, 1 },
        { "ErrorCode", () => new DeadLetterFilter { ErrorCode = "order.failed" }, 2 },
        { "CorrelationId", () => new DeadLetterFilter { CorrelationId = "corr-b" }, 1 },
        { "TenantId", () => new DeadLetterFilter { TenantId = "tenant-a" }, 1 },
        { "SourceMessageId", () => new DeadLetterFilter { SourceMessageId = "c" }, 1 },
        { "ExcludeReplayed", () => new DeadLetterFilter { ExcludeReplayed = true }, 2 },
        { "OnlyReplayed", () => new DeadLetterFilter { ExcludeReplayed = false }, 1 },
        { "DeadLetteredAfter", () => new DeadLetterFilter { DeadLetteredAfterUtc = Now.AddDays(-2) }, 2 },
        { "DeadLetteredBefore", () => new DeadLetterFilter { DeadLetteredBeforeUtc = Now.AddDays(-2) }, 2 },
        { "ExpiresAtOrBefore", () => new DeadLetterFilter { ExpiresAtOrBeforeUtc = Now }, 1 },
        { "EmptyValueMatchesEverything", () => new DeadLetterFilter { SourcePattern = string.Empty }, 3 },
        { "Combined", () => new DeadLetterFilter { SourcePattern = "Outbox", ErrorCode = "order.failed", ExcludeReplayed = true }, 2 },
        { "NoMatch", () => new DeadLetterFilter { SourcePattern = "Missing" }, 0 }
    };

    [Theory]
    [MemberData(nameof(Filters))]
    public async Task GetCountAsync_Filter_CountsOnlyMatchingMessages(string name, Func<DeadLetterFilter> filter, int expected)
    {
        name.ShouldNotBeNullOrEmpty();
        var store = await CreateStoreAsync();
        await SeedAsync(store);

        Right(await store.GetCountAsync(filter())).ShouldBe(expected);
    }

    [Fact]
    public async Task DeleteManyAsync_Filter_DeletesOnlyMatchingMessages()
    {
        var store = await CreateStoreAsync();
        await SeedAsync(store);

        Right(await store.DeleteManyAsync(new DeadLetterFilter { SourcePattern = "Outbox" })).ShouldBe(2);

        Right(await store.GetCountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAndMissing_ReportWhetherARowWasRemoved()
    {
        var store = await CreateStoreAsync();
        var message = Message("a", "Outbox", Now);
        await store.AddAsync(message);

        Right(await store.DeleteAsync(message.Id)).ShouldBeTrue();
        Right(await store.DeleteAsync(message.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteExpiredAsync_DeletesOnlyMessagesExpiredAtTheProviderClock()
    {
        var store = await CreateStoreAsync();
        await SeedAsync(store);

        // Only message "a" (expires an hour ago) is expired at Now; "c" expires an hour from now.
        Right(await store.DeleteExpiredAsync()).ShouldBe(1);
        _time.Advance(TimeSpan.FromHours(2));
        Right(await store.DeleteExpiredAsync()).ShouldBe(1);

        Right(await store.GetCountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ClaimsOnceUntilTheClaimExpires()
    {
        var store = await CreateStoreAsync();
        var message = Message("a", "Outbox", Now);
        await store.AddAsync(message);

        Right(await store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(-5))).ShouldBeTrue();
        Right(await store.GetAsync(message.Id)).ShouldBeSome().ReplayClaimedAtUtc.ShouldBe(Now);

        // A live claim (claimed after the cutoff) is not taken again; a stale one is.
        Right(await store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(-5))).ShouldBeFalse();
        _time.Advance(TimeSpan.FromMinutes(10));
        Right(await store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(5))).ShouldBeTrue();
        Right(await store.GetAsync(message.Id)).ShouldBeSome().ReplayClaimedAtUtc.ShouldBe(Now.AddMinutes(10));
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ReplayedOrUnknownMessage_IsNotClaimed()
    {
        var store = await CreateStoreAsync();
        var message = Message("a", "Outbox", Now);
        message.ReplayedAtUtc = Now.AddMinutes(-1);
        await store.AddAsync(message);

        Right(await store.TryClaimForReplayAsync(message.Id, Now.AddDays(1))).ShouldBeFalse();
        Right(await store.TryClaimForReplayAsync(Guid.NewGuid(), Now.AddDays(1))).ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsReplayedAsync_MarksOnceAtTheProviderClock()
    {
        var store = await CreateStoreAsync();
        var message = Message("a", "Outbox", Now);
        await store.AddAsync(message);

        Right(await store.MarkAsReplayedAsync(message.Id, "success")).ShouldBeTrue();
        Right(await store.MarkAsReplayedAsync(message.Id, "again")).ShouldBeFalse();

        var stored = Right(await store.GetAsync(message.Id)).ShouldBeSome();
        stored.IsReplayed.ShouldBeTrue();
        stored.ReplayedAtUtc.ShouldBe(Now);
        stored.ReplayResult.ShouldBe("success");
    }

    [Fact]
    public async Task GetMessagesAsync_PagesInDeadLetteredOrder()
    {
        var store = await CreateStoreAsync();
        await SeedAsync(store);

        var oldestFirst = Right(await store.GetMessagesAsync(skip: 1, take: 2)).Select(m => m.SourceMessageId);
        var newestFirst = Right(await store.GetMessagesAsync(newestFirst: true, take: 2)).Select(m => m.SourceMessageId);

        oldestFirst.ShouldBe(["b", "c"]);
        newestFirst.ShouldBe(["c", "b"]);
    }

    [Fact]
    public async Task GetMessagesAsync_Filter_ReturnsOnlyMatchingMessages()
    {
        var store = await CreateStoreAsync();
        await SeedAsync(store);

        var messages = Right(await store.GetMessagesAsync(new DeadLetterFilter { SourcePattern = "Outbox" }));

        messages.Select(m => m.SourceMessageId).ShouldBe(["a", "c"]);
    }

    [Fact]
    public async Task AddAsync_LocalTimestamp_IsStoredAsTheUtcInstant()
    {
        var store = await CreateStoreAsync();
        var message = Message("a", "Outbox", Now.ToLocalTime());

        await store.AddAsync(message);

        Right(await store.GetAsync(message.Id)).ShouldBeSome().DeadLetteredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public async Task SaveChangesAsync_IsANoOpThatSucceeds()
    {
        var store = await CreateStoreAsync();

        (await store.SaveChangesAsync()).IsRight.ShouldBeTrue();
    }

    public static TheoryData<string, Func<Store, Task<string>>, string> FailingOperations => new()
    {
        { "AddAsync", async s => CodeOf(await s.AddAsync(Message("a", "Outbox", Now))), DeadLetterErrorCodes.StoreFailed },
        { "GetAsync", async s => CodeOf(await s.GetAsync(Guid.NewGuid())), DeadLetterErrorCodes.GetFailed },
        { "GetMessagesAsync", async s => CodeOf(await s.GetMessagesAsync()), DeadLetterErrorCodes.QueryFailed },
        { "GetCountAsync", async s => CodeOf(await s.GetCountAsync()), DeadLetterErrorCodes.QueryFailed },
        { "TryClaimForReplayAsync", async s => CodeOf(await s.TryClaimForReplayAsync(Guid.NewGuid(), Now)), DeadLetterErrorCodes.ClaimFailed },
        { "MarkAsReplayedAsync", async s => CodeOf(await s.MarkAsReplayedAsync(Guid.NewGuid(), "x")), DeadLetterErrorCodes.MarkReplayedFailed },
        { "DeleteAsync", async s => CodeOf(await s.DeleteAsync(Guid.NewGuid())), DeadLetterErrorCodes.DeleteFailed },
        { "DeleteManyAsync", async s => CodeOf(await s.DeleteManyAsync(new DeadLetterFilter())), DeadLetterErrorCodes.DeleteFailed },
        { "DeleteExpiredAsync", async s => CodeOf(await s.DeleteExpiredAsync()), DeadLetterErrorCodes.CleanupFailed }
    };

    [Theory]
    [MemberData(nameof(FailingOperations))]
    public async Task Operation_ConnectionFailsToOpen_ReturnsLeftWithTheOperationErrorCode(
        string name, Func<Store, Task<string>> run, string expectedCode)
    {
        name.ShouldNotBeNullOrEmpty();
        var connection = Substitute.For<DbConnection>();
        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("database down"));

        (await run(new Store(connection, timeProvider: _time))).ShouldBe(expectedCode);
    }

    [Fact]
    public async Task Operation_CancelledWhileOpening_PropagatesTheCancellation()
    {
        var connection = Substitute.For<DbConnection>();
        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new OperationCanceledException());
        var store = new Store(connection, timeProvider: _time);

        await Should.ThrowAsync<OperationCanceledException>(() => store.GetCountAsync());
    }

    [Fact]
    public async Task GetCountAsync_MissingTable_ReturnsQueryFailed()
    {
        var store = await CreateStoreAsync(createTable: false);

        CodeOf(await store.GetCountAsync()).ShouldBe(DeadLetterErrorCodes.QueryFailed);
    }

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(
            Right: _ => throw new InvalidOperationException("Expected a Left result."),
            Left: error => error.GetCode().IfNone(string.Empty));

    private static T Right<T>(Either<EncinaError, T> result)
        => result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException("Expected a Right result: " + error.GetCode().IfNone(string.Empty)));

    private static StoreMessage Message(string sourceMessageId, string sourcePattern, DateTime deadLetteredAtUtc) => new()
    {
        Id = Guid.NewGuid(),
        RequestType = "OrderCommand",
        RequestContent = "{}",
        ErrorCode = "order.failed",
        SourcePattern = sourcePattern,
        SourceMessageId = sourceMessageId,
        TotalRetryAttempts = 3,
        FirstFailedAtUtc = deadLetteredAtUtc.AddHours(-1),
        DeadLetteredAtUtc = deadLetteredAtUtc
    };

    // a: Outbox, tenant-a, expired an hour ago; b: Inbox, replayed, never expires; c: Outbox, expires in an hour.
    private static async Task SeedAsync(Store store)
    {
        var a = Message("a", "Outbox", Now.AddDays(-3));
        a.TenantId = "tenant-a";
        a.ExpiresAtUtc = Now.AddHours(-1);

        var b = Message("b", "Inbox", Now.AddDays(-2));
        b.RequestType = "PayCommand";
        b.ErrorCode = "pay.failed";
        b.CorrelationId = "corr-b";
        b.ReplayedAtUtc = Now.AddDays(-1);

        var c = Message("c", "Outbox", Now.AddDays(-1));
        c.ExpiresAtUtc = Now.AddHours(1);

        foreach (var message in new[] { a, b, c })
        {
            (await store.AddAsync(message)).IsRight.ShouldBeTrue();
        }
    }

    private async Task<Store> CreateStoreAsync(string tableName = "DeadLetterMessages", bool createTable = true)
    {
        await _connection.OpenAsync();
        if (!createTable)
        {
            return new Store(_connection, tableName, _time);
        }

        await using var create = _connection.CreateCommand();
        create.CommandText = $@"CREATE TABLE IF NOT EXISTS {tableName.Split('.')[^1]} (
            Id TEXT PRIMARY KEY, RequestType TEXT NOT NULL, RequestContent TEXT NOT NULL, ErrorCode TEXT NOT NULL,
            ExceptionType TEXT NULL, ExceptionStackTrace TEXT NULL, CorrelationId TEXT NULL,
            SourcePattern TEXT NOT NULL, SourceMessageId TEXT NOT NULL, TenantId TEXT NULL,
            TotalRetryAttempts INTEGER NOT NULL, FirstFailedAtUtc TEXT NOT NULL, DeadLetteredAtUtc TEXT NOT NULL,
            ExpiresAtUtc TEXT NULL, ReplayClaimedAtUtc TEXT NULL, ReplayedAtUtc TEXT NULL, ReplayResult TEXT NULL,
            UNIQUE (SourcePattern, SourceMessageId))";
        await create.ExecuteNonQueryAsync();

        return new Store(_connection, tableName, _time);
    }
}

internal static class OptionTestExtensions
{
    public static T ShouldBeSome<T>(this LanguageExt.Option<T> option)
        => option.Match(Some: value => value, None: () => throw new ShouldAssertException("Expected Some but was None."));
}
