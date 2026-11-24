using System.Data;
using System.Data.Common;
using Encina.Dapper.MySQL.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.Testing.Shouldly;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Dapper.MySQL.DeadLetter;

/// <summary>
/// Unit tests for the Dapper MySQL <see cref="DeadLetterStoreDapper"/> (#2879).
/// </summary>
/// <remarks>
/// SQLite in-memory is only a relational stand-in that runs the statements the store shares with SQLite
/// (<c>LIMIT/OFFSET</c>); no provider claim is made. The MySQL duplicate key error (1062), the
/// <c>utf8mb4_bin</c> collation and <c>DATETIME(6)</c> behaviour are covered by the integration tests.
/// </remarks>
public sealed class DeadLetterStoreDapperTests : IAsyncDisposable
{
    private static readonly DateTime Start = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(Start));

    public static IEnumerable<object[]> FailingOperations() =>
    [
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.AddAsync(NewMessage()))), DeadLetterErrorCodes.StoreFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.GetAsync(Guid.NewGuid()))), DeadLetterErrorCodes.GetFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.GetCountAsync())), DeadLetterErrorCodes.QueryFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.GetMessagesAsync())), DeadLetterErrorCodes.QueryFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.TryClaimForReplayAsync(Guid.NewGuid(), Start))), DeadLetterErrorCodes.ClaimFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.MarkAsReplayedAsync(Guid.NewGuid(), "ok"))), DeadLetterErrorCodes.MarkReplayedFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.DeleteAsync(Guid.NewGuid()))), DeadLetterErrorCodes.DeleteFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.DeleteManyAsync(new DeadLetterFilter()))), DeadLetterErrorCodes.DeleteFailed],
        [(Func<DeadLetterStoreDapper, Task<string>>)(s => Code(s.DeleteExpiredAsync())), DeadLetterErrorCodes.CleanupFailed],
    ];

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    private static async Task<string> Code<T>(Task<Either<EncinaError, T>> task) =>
        (await task).Match(_ => string.Empty, error => error.GetCode().IfNone(string.Empty));

    private async Task<DeadLetterStoreDapper> CreateStoreAsync()
    {
        await _connection.OpenAsync();
        await using var command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE DeadLetterMessages (
                Id TEXT PRIMARY KEY, RequestType TEXT NOT NULL, RequestContent TEXT NOT NULL, ErrorCode TEXT NOT NULL,
                ExceptionType TEXT NULL, ExceptionStackTrace TEXT NULL, CorrelationId TEXT NULL,
                SourcePattern TEXT NOT NULL, SourceMessageId TEXT NOT NULL, TenantId TEXT NULL,
                TotalRetryAttempts INTEGER NOT NULL, FirstFailedAtUtc TEXT NOT NULL, DeadLetteredAtUtc TEXT NOT NULL,
                ExpiresAtUtc TEXT NULL, ReplayClaimedAtUtc TEXT NULL, ReplayedAtUtc TEXT NULL, ReplayResult TEXT NULL,
                UNIQUE (SourcePattern, SourceMessageId));
            """;
        await command.ExecuteNonQueryAsync();
        return new DeadLetterStoreDapper(_connection, timeProvider: _clock);
    }

    private static DeadLetterMessage NewMessage(
        string sourceMessageId = "src-1",
        DateTime? expiresAtUtc = null,
        DateTime? deadLetteredAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        RequestType = "Orders.PlaceOrder",
        RequestContent = "{}",
        ErrorCode = "orders.failed",
        ExceptionType = "System.InvalidOperationException",
        CorrelationId = "corr-1",
        SourcePattern = "outbox",
        SourceMessageId = sourceMessageId,
        TenantId = "tenant-1",
        TotalRetryAttempts = 3,
        FirstFailedAtUtc = Start.AddHours(-2),
        DeadLetteredAtUtc = deadLetteredAtUtc ?? Start.AddHours(-1),
        ExpiresAtUtc = expiresAtUtc
    };

    private static IDeadLetterMessage Stored(Either<EncinaError, Option<IDeadLetterMessage>> result) =>
        result.ShouldBeRight().IfNone(() => throw new InvalidOperationException("The message was not found."));

    private static DbConnection FailingOpenConnection()
    {
        var connection = Substitute.For<DbConnection>();
        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("boom")));
        return connection;
    }

    #region Constructor

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new DeadLetterStoreDapper(null!));

        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_ConnectionNotDerivedFromDbConnection_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new DeadLetterStoreDapper(Substitute.For<IDbConnection>()));

        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_InvalidTableName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new DeadLetterStoreDapper(_connection, "Bad;Table"));
    }

    [Fact]
    public void Constructor_DefaultTimeProvider_CreatesInstance()
    {
        new DeadLetterStoreDapper(_connection).ShouldNotBeNull();
    }

    #endregion

    #region Argument checks

    [Fact]
    public async Task Operations_InvalidArguments_Throw()
    {
        var store = await CreateStoreAsync();

        await Should.ThrowAsync<ArgumentNullException>(() => store.AddAsync(null!));
        await Should.ThrowAsync<ArgumentException>(() => store.AddAsync(NewMessage(sourceMessageId: "")));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.GetAsync(Guid.Empty));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.TryClaimForReplayAsync(Guid.Empty, Start));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.MarkAsReplayedAsync(Guid.Empty, "ok"));
        await Should.ThrowAsync<ArgumentException>(() => store.MarkAsReplayedAsync(Guid.NewGuid(), " "));
        await Should.ThrowAsync<ArgumentNullException>(() => store.MarkAsReplayedAsync(Guid.NewGuid(), null!));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.DeleteAsync(Guid.Empty));
        await Should.ThrowAsync<ArgumentNullException>(() => store.DeleteManyAsync(null!));
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, DeadLetterStoreLimits.MaxPageSize + 1)]
    public async Task GetMessagesAsync_OutOfRangePaging_ThrowsArgumentOutOfRangeException(int skip, int take)
    {
        var store = await CreateStoreAsync();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => store.GetMessagesAsync(skip: skip, take: take));
    }

    #endregion

    #region Failures

    [Theory]
    [MemberData(nameof(FailingOperations))]
    public async Task Operation_ConnectionFailsToOpen_ReturnsItsErrorCode(Func<DeadLetterStoreDapper, Task<string>> operation, string expectedCode)
    {
        var store = new DeadLetterStoreDapper(FailingOpenConnection());

        (await operation(store)).ShouldBe(expectedCode);
    }

    [Fact]
    public async Task GetAsync_CancelledOpen_PropagatesOperationCanceledException()
    {
        var connection = Substitute.For<DbConnection>();
        connection.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.FromCanceled(new CancellationToken(true)));
        var store = new DeadLetterStoreDapper(connection);

        await Should.ThrowAsync<OperationCanceledException>(() => store.GetAsync(Guid.NewGuid(), new CancellationToken(true)));
    }

    [Fact]
    public async Task AddAsync_DuplicateSource_ReturnsStoreFailedOnTheStandIn()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(NewMessage());

        (await store.AddAsync(NewMessage())).ShouldBeErrorWithCode(DeadLetterErrorCodes.StoreFailed);
    }

    [Fact]
    public async Task ClosedConnection_IsOpenedByTheStoreBeforeTheFirstCommand()
    {
        await using var closed = new SqliteConnection("DataSource=:memory:");
        var store = new DeadLetterStoreDapper(closed, timeProvider: _clock);

        // A fresh in-memory database has no table, so the query fails, but only after OpenAsync ran.
        (await store.GetCountAsync()).ShouldBeErrorWithCode(DeadLetterErrorCodes.QueryFailed);
        closed.State.ShouldBe(ConnectionState.Open);
    }

    #endregion

    #region Add, get, page, count

    [Fact]
    public async Task AddAsync_NewMessage_IsInsertedAndReadBackAsUtc()
    {
        var store = await CreateStoreAsync();
        var message = NewMessage(expiresAtUtc: Start.AddDays(1));

        (await store.AddAsync(message)).ShouldBeRight().ShouldBeTrue();

        var stored = Stored(await store.GetAsync(message.Id));
        stored.SourceMessageId.ShouldBe("src-1");
        stored.FirstFailedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        stored.DeadLetteredAtUtc.ShouldBe(Start.AddHours(-1));
        stored.ExpiresAtUtc.ShouldBe(Start.AddDays(1));
        stored.ExpiresAtUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        stored.ReplayedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNone()
    {
        var store = await CreateStoreAsync();

        (await store.GetAsync(Guid.NewGuid())).ShouldBeRight().IsNone.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, new[] { "a", "b", "c" })]
    [InlineData(true, new[] { "c", "b", "a" })]
    public async Task GetMessagesAsync_OrdersByDeadLetteredAtAndPages(bool newestFirst, string[] expectedOrder)
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(NewMessage("b", deadLetteredAtUtc: Start.AddMinutes(-20)));
        await store.AddAsync(NewMessage("c", deadLetteredAtUtc: Start.AddMinutes(-10)));
        await store.AddAsync(NewMessage("a", deadLetteredAtUtc: Start.AddMinutes(-30)));

        var all = (await store.GetMessagesAsync(newestFirst: newestFirst)).ShouldBeRight();
        var page = (await store.GetMessagesAsync(null, 1, 1, newestFirst)).ShouldBeRight();

        all.Select(m => m.SourceMessageId).ShouldBe(expectedOrder);
        page.Select(m => m.SourceMessageId).ShouldBe(["b"]);
    }

    [Fact]
    public async Task GetCountAsync_WithEveryFilterField_CountsOnlyTheMatchingMessage()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(NewMessage("a", expiresAtUtc: Start.AddDays(1)));
        await store.AddAsync(NewMessage("b"));
        var filter = new DeadLetterFilter
        {
            SourcePattern = "outbox",
            RequestType = "Orders.PlaceOrder",
            ErrorCode = "orders.failed",
            CorrelationId = "corr-1",
            TenantId = "tenant-1",
            SourceMessageId = "a",
            ExcludeReplayed = true,
            DeadLetteredAfterUtc = Start.AddHours(-2),
            DeadLetteredBeforeUtc = Start,
            ExpiresAtOrBeforeUtc = Start.AddDays(2)
        };

        (await store.GetCountAsync()).ShouldBeRight().ShouldBe(2);
        (await store.GetCountAsync(filter)).ShouldBeRight().ShouldBe(1);
    }

    #endregion

    #region Replay, delete, expiry

    [Fact]
    public async Task TryClaimForReplayAsync_ClaimsAtTheClockTimeAndHoldsUntilTheClaimExpires()
    {
        var store = await CreateStoreAsync();
        var message = NewMessage();
        await store.AddAsync(message);

        (await store.TryClaimForReplayAsync(message.Id, Start.AddMinutes(-5))).ShouldBeRight().ShouldBeTrue();
        Stored(await store.GetAsync(message.Id)).ReplayClaimedAtUtc.ShouldBe(Start);

        _clock.Advance(TimeSpan.FromMinutes(10));
        (await store.TryClaimForReplayAsync(message.Id, Start.AddMinutes(-5))).ShouldBeRight().ShouldBeFalse();
        (await store.TryClaimForReplayAsync(message.Id, Start.AddMinutes(5))).ShouldBeRight().ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsReplayedAsync_StampsTheClockTimeOnlyOnce()
    {
        var store = await CreateStoreAsync();
        var message = NewMessage();
        await store.AddAsync(message);
        _clock.Advance(TimeSpan.FromHours(1));

        (await store.MarkAsReplayedAsync(message.Id, "success")).ShouldBeRight().ShouldBeTrue();
        (await store.MarkAsReplayedAsync(message.Id, "again")).ShouldBeRight().ShouldBeFalse();

        var stored = Stored(await store.GetAsync(message.Id));
        stored.ReplayedAtUtc.ShouldBe(Start.AddHours(1));
        stored.ReplayResult.ShouldBe("success");
        (await store.GetCountAsync(new DeadLetterFilter { ExcludeReplayed = false })).ShouldBeRight().ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_ExistingMessage_DeletesItOnce()
    {
        var store = await CreateStoreAsync();
        var message = NewMessage();
        await store.AddAsync(message);

        (await store.DeleteAsync(message.Id)).ShouldBeRight().ShouldBeTrue();
        (await store.DeleteAsync(message.Id)).ShouldBeRight().ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_FilterBySourceMessage_DeletesOnlyTheMatchingMessages()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(NewMessage("a"));
        await store.AddAsync(NewMessage("b"));

        (await store.DeleteManyAsync(new DeadLetterFilter { SourceMessageId = "a" })).ShouldBeRight().ShouldBe(1);
        (await store.GetCountAsync()).ShouldBeRight().ShouldBe(1);
    }

    [Fact]
    public async Task DeleteExpiredAsync_DeletesOnlyMessagesExpiredAtTheClockTime()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(NewMessage("expired", expiresAtUtc: Start.AddMinutes(30)));
        await store.AddAsync(NewMessage("later", expiresAtUtc: Start.AddDays(1)));
        await store.AddAsync(NewMessage("never"));

        (await store.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(0);
        _clock.Advance(TimeSpan.FromHours(1));

        (await store.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(1);
        (await store.GetCountAsync()).ShouldBeRight().ShouldBe(2);
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsUnit()
    {
        var store = await CreateStoreAsync();

        (await store.SaveChangesAsync()).IsRight.ShouldBeTrue();
    }

    #endregion
}
