using Encina.EntityFrameworkCore.DeadLetter;
using Encina.Messaging.DeadLetter;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.UnitTests.EntityFrameworkCore.DeadLetter;

/// <summary>
/// Unit tests for <see cref="DeadLetterStoreEF"/> on an in-memory SQLite database. The EF InMemory provider cannot
/// run <c>ExecuteUpdateAsync</c> and <c>ExecuteDeleteAsync</c>, so the relational SQLite provider is used. What SQLite
/// does not reproduce (SQL Server, PostgreSQL and MySQL translation, collations, transactions) stays with the
/// EF integration collections. Argument checks are covered by <c>EntityFrameworkDeadLetterStoreGuardTests</c>.
/// </summary>
public sealed class DeadLetterStoreEFTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));
    private readonly DbContextOptions<DeadLetterTestDbContext> _options;
    private readonly DeadLetterTestDbContext _context;
    private readonly DeadLetterStoreEF _store;

    public DeadLetterStoreEFTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<DeadLetterTestDbContext>().UseSqlite(_connection).Options;
        _context = new DeadLetterTestDbContext(_options);
        _context.Database.EnsureCreated();
        _store = new DeadLetterStoreEF(_context, _time);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // A store over a context whose table was never created: every query fails inside the provider.
    private static (DeadLetterStoreEF Store, Action Cleanup) BrokenStore()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<DeadLetterTestDbContext>().UseSqlite(connection).Options;
        var context = new DeadLetterTestDbContext(options);
        return (new DeadLetterStoreEF(context), () =>
        {
            context.Dispose();
            connection.Dispose();
        }
        );
    }

    private static async Task<string> LeftCode<T>(Task<Either<EncinaError, T>> call)
        => (await call).ShouldBeLeft().GetCode().IfNone(string.Empty);

    private static DeadLetterMessage NewMessage(
        string source = "src-1",
        string pattern = "Outbox",
        DateTime? deadLetteredAt = null,
        Action<DeadLetterMessage>? customize = null)
    {
        var message = new DeadLetterMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Orders.PlaceOrder",
            RequestContent = "{}",
            ErrorCode = "orders.failed",
            SourcePattern = pattern,
            SourceMessageId = source,
            TotalRetryAttempts = 3,
            FirstFailedAtUtc = Now.AddHours(-2),
            DeadLetteredAtUtc = deadLetteredAt ?? Now.AddHours(-1)
        };
        customize?.Invoke(message);
        return message;
    }

    // Seeds through a separate context on the same connection so the store's context stays clean.
    private async Task<DeadLetterMessage[]> SeedAsync(params DeadLetterMessage[] messages)
    {
        await using var context = new DeadLetterTestDbContext(_options);
        context.DeadLetterMessages.AddRange(messages);
        await context.SaveChangesAsync();
        return messages;
    }

    private async Task<List<DeadLetterMessage>> ReadAllAsync()
    {
        await using var context = new DeadLetterTestDbContext(_options);
        return await context.DeadLetterMessages.AsNoTracking().ToListAsync();
    }

    private async Task<DeadLetterMessage> ReadAsync(Guid id) => (await ReadAllAsync()).Single(m => m.Id == id);

    // ---- Construction ----

    [Fact]
    public async Task Constructor_NoTimeProvider_UsesTheSystemClock()
    {
        var store = new DeadLetterStoreEF(_context);
        var message = (await SeedAsync(NewMessage()))[0];
        var before = DateTime.UtcNow;

        (await store.TryClaimForReplayAsync(message.Id, before.AddMinutes(-5))).ShouldBeRight().ShouldBeTrue();

        var claimedAt = (await ReadAsync(message.Id)).ReplayClaimedAtUtc.ShouldNotBeNull();
        claimedAt.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        claimedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    // ---- AddAsync / SaveChangesAsync ----

    [Fact]
    public async Task AddAsync_NewMessage_ReturnsTrueAndPersistsOnlyAfterSaveChanges()
    {
        var message = NewMessage();

        (await _store.AddAsync(message)).ShouldBeRight().ShouldBeTrue();
        (await ReadAllAsync()).ShouldBeEmpty();

        (await _store.SaveChangesAsync()).ShouldBeRight();

        var stored = (await ReadAllAsync()).ShouldHaveSingleItem();
        stored.Id.ShouldBe(message.Id);
        stored.SourceMessageId.ShouldBe("src-1");
    }

    [Fact]
    public async Task AddAsync_SameSourceTwiceInOneUnitOfWork_CapturesOnce()
    {
        (await _store.AddAsync(NewMessage())).ShouldBeRight().ShouldBeTrue();
        (await _store.AddAsync(NewMessage())).ShouldBeRight().ShouldBeFalse();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        (await ReadAllAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddAsync_SourceAlreadyPersisted_ReturnsFalseAndAddsNothing()
    {
        await SeedAsync(NewMessage());

        (await _store.AddAsync(NewMessage())).ShouldBeRight().ShouldBeFalse();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        (await ReadAllAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddAsync_SameSourceIdUnderAnotherPattern_IsADifferentCapture()
    {
        await SeedAsync(NewMessage("src-1", "Outbox"));

        (await _store.AddAsync(NewMessage("src-1", "Inbox"))).ShouldBeRight().ShouldBeTrue();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        (await ReadAllAsync()).Count.ShouldBe(2);
    }

    [Fact]
    public async Task AddAsync_SamePatternAnotherSourceId_IsADifferentCapture()
    {
        await SeedAsync(NewMessage("src-1"));

        (await _store.AddAsync(NewMessage("src-2"))).ShouldBeRight().ShouldBeTrue();
    }

    [Fact]
    public async Task AddAsync_MessageOfAnotherImplementation_ReturnsInvalidMessageType()
    {
        var foreign = Substitute.For<IDeadLetterMessage>();
        foreign.SourceMessageId.Returns("src-1");

        var error = (await _store.AddAsync(foreign)).ShouldBeLeft();

        error.GetCode().IfNone(string.Empty).ShouldBe(DeadLetterErrorCodes.InvalidMessageType);
    }

    [Fact]
    public async Task AddAsync_StoreFailure_ReturnsStoreFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.AddAsync(NewMessage()))).ShouldBe(DeadLetterErrorCodes.StoreFailed);
    }

    [Fact]
    public async Task AddAsync_CancelledToken_PropagatesTheCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await ShouldThrowCancellation(() => _store.AddAsync(NewMessage(), cts.Token));
    }

    [Fact]
    public async Task SaveChangesAsync_NothingTracked_ReturnsRight()
    {
        (await _store.SaveChangesAsync()).ShouldBeRight();
    }

    [Fact]
    public async Task SaveChangesAsync_LostRaceOnTheSourceKey_ReturnsLeftAndDetachesThePendingInsert()
    {
        (await _store.AddAsync(NewMessage())).ShouldBeRight().ShouldBeTrue();
        await SeedAsync(NewMessage()); // another host captures the same source after the check

        (await LeftCode(_store.SaveChangesAsync())).ShouldBe(DeadLetterErrorCodes.StoreFailed);

        _context.ChangeTracker.Entries<DeadLetterMessage>().ShouldBeEmpty();
        (await _store.SaveChangesAsync()).ShouldBeRight();
        (await ReadAllAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChangesAsync_CancelledToken_PropagatesTheCancellation()
    {
        (await _store.AddAsync(NewMessage())).ShouldBeRight();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await ShouldThrowCancellation(() => _store.SaveChangesAsync(cts.Token));
    }

    // ---- GetAsync ----

    [Fact]
    public async Task GetAsync_ExistingMessage_ReturnsSomeWithItsFields()
    {
        var seeded = (await SeedAsync(NewMessage(customize: m =>
        {
            m.TenantId = "tenant-1";
            m.ExceptionType = "System.Exception";
        })))[0];

        var option = (await _store.GetAsync(seeded.Id)).ShouldBeRight();

        var message = SomeOf(option);
        message.Id.ShouldBe(seeded.Id);
        message.TenantId.ShouldBe("tenant-1");
        message.ExceptionType.ShouldBe("System.Exception");
        message.RequestType.ShouldBe("Orders.PlaceOrder");
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNone()
    {
        await SeedAsync(NewMessage());

        (await _store.GetAsync(Guid.NewGuid())).ShouldBeRight().IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_ReadsInstantsBackAsUtc()
    {
        var seeded = (await SeedAsync(NewMessage(customize: m =>
            m.FirstFailedAtUtc = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Unspecified))))[0];

        var message = SomeOf((await _store.GetAsync(seeded.Id)).ShouldBeRight());

        message.FirstFailedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        message.FirstFailedAtUtc.ShouldBe(new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetAsync_StoreFailure_ReturnsGetFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.GetAsync(Guid.NewGuid()))).ShouldBe(DeadLetterErrorCodes.GetFailed);
    }

    // ---- GetMessagesAsync / GetCountAsync ----

    private async Task<DeadLetterMessage[]> SeedCatalogAsync() => await SeedAsync(
        NewMessage("a", "Outbox", Now.AddHours(-5), m => { m.TenantId = "t1"; m.CorrelationId = "c1"; }),
        NewMessage("b", "Inbox", Now.AddHours(-4), m => { m.TenantId = "t2"; m.RequestType = "Orders.Cancel"; }),
        NewMessage("c", "Outbox", Now.AddHours(-3), m => { m.ErrorCode = "orders.timeout"; m.ReplayedAtUtc = Now.AddHours(-2); }),
        NewMessage("d", "Outbox", Now.AddHours(-2), m => m.ExpiresAtUtc = Now.AddDays(1)),
        NewMessage("e", "Saga", Now.AddHours(-1), m => m.ExpiresAtUtc = Now.AddDays(10)));

    private async Task<string[]> SourcesAsync(DeadLetterFilter? filter, bool newestFirst = false, int skip = 0, int take = 100)
    {
        var page = (await _store.GetMessagesAsync(filter, skip, take, newestFirst)).ShouldBeRight();
        return page.Select(m => m.SourceMessageId).ToArray();
    }

    [Fact]
    public async Task GetMessagesAsync_NoFilter_ReturnsOldestFirstByDefault()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(null)).ShouldBe(["a", "b", "c", "d", "e"]);
    }

    [Fact]
    public async Task GetMessagesAsync_NewestFirst_ReversesTheOrder()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(null, newestFirst: true)).ShouldBe(["e", "d", "c", "b", "a"]);
    }

    [Fact]
    public async Task GetMessagesAsync_SkipAndTake_ReturnAPage()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(null, skip: 1, take: 2)).ShouldBe(["b", "c"]);
    }

    [Fact]
    public async Task GetMessagesAsync_SameInstant_OrdersByIdAsTieBreaker()
    {
        var instant = Now.AddHours(-1);
        var seeded = await SeedAsync(
            NewMessage("x1", deadLetteredAt: instant),
            NewMessage("x2", deadLetteredAt: instant),
            NewMessage("x3", deadLetteredAt: instant));

        var expectedAscending = seeded.OrderBy(m => m.Id).Select(m => m.SourceMessageId).ToArray();

        (await SourcesAsync(null)).ShouldBe(expectedAscending);
        (await SourcesAsync(null, newestFirst: true)).ShouldBe(expectedAscending.Reverse().ToArray());
    }

    [Fact]
    public async Task GetMessagesAsync_EmptyFilter_MatchesEverything()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { SourcePattern = string.Empty, TenantId = string.Empty })).Length.ShouldBe(5);
    }

    [Fact]
    public async Task GetMessagesAsync_BySourcePattern_ReturnsOnlyThatPattern()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { SourcePattern = "Outbox" })).ShouldBe(["a", "c", "d"]);
    }

    [Fact]
    public async Task GetMessagesAsync_FromSourceFactory_ReturnsThePendingMessagesOfThatPattern()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(DeadLetterFilter.FromSource("Outbox"))).ShouldBe(["a", "d"]);
    }

    [Fact]
    public async Task GetMessagesAsync_ByRequestType_ReturnsOnlyThatType()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { RequestType = "Orders.Cancel" })).ShouldBe(["b"]);
    }

    [Fact]
    public async Task GetMessagesAsync_ByErrorCode_ReturnsOnlyThatCode()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { ErrorCode = "orders.timeout" })).ShouldBe(["c"]);
    }

    [Fact]
    public async Task GetMessagesAsync_ByCorrelationId_ReturnsOnlyThatCorrelation()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(DeadLetterFilter.ByCorrelationId("c1"))).ShouldBe(["a"]);
    }

    [Fact]
    public async Task GetMessagesAsync_ByTenantId_ReturnsOnlyThatTenant()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { TenantId = "t2" })).ShouldBe(["b"]);
    }

    [Fact]
    public async Task GetMessagesAsync_BySourceMessageId_ReturnsOnlyThatSource()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { SourceMessageId = "d" })).ShouldBe(["d"]);
    }

    [Fact]
    public async Task GetMessagesAsync_ExcludeReplayedTrue_ReturnsPendingOnly()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { ExcludeReplayed = true })).ShouldBe(["a", "b", "d", "e"]);
    }

    [Fact]
    public async Task GetMessagesAsync_ExcludeReplayedFalse_ReturnsReplayedOnly()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { ExcludeReplayed = false })).ShouldBe(["c"]);
    }

    [Fact]
    public async Task GetMessagesAsync_DeadLetteredAfter_IsInclusive()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { DeadLetteredAfterUtc = Now.AddHours(-3) })).ShouldBe(["c", "d", "e"]);
    }

    [Fact]
    public async Task GetMessagesAsync_DeadLetteredBefore_IsInclusive()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { DeadLetteredBeforeUtc = Now.AddHours(-4) })).ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task GetMessagesAsync_ExpiresAtOrBefore_ExcludesMessagesWithoutExpiry()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(new DeadLetterFilter { ExpiresAtOrBeforeUtc = Now.AddDays(1) })).ShouldBe(["d"]);
        (await SourcesAsync(new DeadLetterFilter { ExpiresAtOrBeforeUtc = Now.AddDays(10) })).ShouldBe(["d", "e"]);
    }

    [Fact]
    public async Task GetMessagesAsync_FilterWithUnspecifiedKind_IsReadAsUtc()
    {
        await SeedCatalogAsync();
        var unspecified = DateTime.SpecifyKind(Now.AddHours(-3), DateTimeKind.Unspecified);

        (await SourcesAsync(new DeadLetterFilter { DeadLetteredAfterUtc = unspecified })).ShouldBe(["c", "d", "e"]);
    }

    [Fact]
    public async Task GetMessagesAsync_FilterWithLocalKind_IsConvertedToUtc()
    {
        await SeedCatalogAsync();
        var local = Now.AddHours(-3).ToLocalTime();

        (await SourcesAsync(new DeadLetterFilter { DeadLetteredAfterUtc = local })).ShouldBe(["c", "d", "e"]);
    }

    [Fact]
    public async Task GetMessagesAsync_SinceFactory_ReturnsThePendingMessagesInTheWindow()
    {
        await SeedCatalogAsync();

        (await SourcesAsync(DeadLetterFilter.Since(Now.AddHours(-3)))).ShouldBe(["d", "e"]);
    }

    [Fact]
    public async Task GetMessagesAsync_CombinedFilters_AreAnded()
    {
        await SeedCatalogAsync();

        var filter = new DeadLetterFilter { SourcePattern = "Outbox", ExcludeReplayed = true };

        (await SourcesAsync(filter)).ShouldBe(["a", "d"]);
    }

    [Fact]
    public async Task GetMessagesAsync_StoreFailure_ReturnsQueryFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.GetMessagesAsync())).ShouldBe(DeadLetterErrorCodes.QueryFailed);
    }

    [Fact]
    public async Task GetCountAsync_NoFilter_CountsEverything()
    {
        await SeedCatalogAsync();

        (await _store.GetCountAsync()).ShouldBeRight().ShouldBe(5);
    }

    [Fact]
    public async Task GetCountAsync_WithFilter_CountsOnlyMatches()
    {
        await SeedCatalogAsync();

        (await _store.GetCountAsync(new DeadLetterFilter { SourcePattern = "Outbox" })).ShouldBeRight().ShouldBe(3);
    }

    [Fact]
    public async Task GetCountAsync_StoreFailure_ReturnsQueryFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.GetCountAsync())).ShouldBe(DeadLetterErrorCodes.QueryFailed);
    }

    // ---- TryClaimForReplayAsync ----

    [Fact]
    public async Task TryClaimForReplayAsync_Unclaimed_ClaimsWithTheClockTime()
    {
        var message = (await SeedAsync(NewMessage()))[0];

        (await _store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(-5))).ShouldBeRight().ShouldBeTrue();

        var stored = await ReadAsync(message.Id);
        stored.ReplayClaimedAtUtc.ShouldBe(Now);
        stored.ReplayedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ClaimStillValid_ReturnsFalseAndKeepsTheClaim()
    {
        var claimedAt = Now.AddMinutes(-1);
        var message = (await SeedAsync(NewMessage(customize: m => m.ReplayClaimedAtUtc = claimedAt)))[0];

        (await _store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(-5))).ShouldBeRight().ShouldBeFalse();

        (await ReadAsync(message.Id)).ReplayClaimedAtUtc.ShouldBe(claimedAt);
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ClaimExpired_TakesItOverWithTheClockTime()
    {
        var message = (await SeedAsync(NewMessage(customize: m => m.ReplayClaimedAtUtc = Now.AddMinutes(-10))))[0];

        (await _store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(-5))).ShouldBeRight().ShouldBeTrue();

        (await ReadAsync(message.Id)).ReplayClaimedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ClaimExactlyAtTheExpiryBoundary_IsExpired()
    {
        var boundary = Now.AddMinutes(-5);
        var message = (await SeedAsync(NewMessage(customize: m => m.ReplayClaimedAtUtc = boundary)))[0];

        (await _store.TryClaimForReplayAsync(message.Id, boundary)).ShouldBeRight().ShouldBeTrue();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_AlreadyReplayed_ReturnsFalse()
    {
        var message = (await SeedAsync(NewMessage(customize: m => m.ReplayedAtUtc = Now.AddMinutes(-1))))[0];

        (await _store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(-5))).ShouldBeRight().ShouldBeFalse();

        (await ReadAsync(message.Id)).ReplayClaimedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_UnknownId_ReturnsFalse()
    {
        await SeedAsync(NewMessage());

        (await _store.TryClaimForReplayAsync(Guid.NewGuid(), Now)).ShouldBeRight().ShouldBeFalse();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_TwoClaims_OnlyTheFirstWins()
    {
        var message = (await SeedAsync(NewMessage()))[0];
        var expiredBefore = Now.AddMinutes(-5);

        (await _store.TryClaimForReplayAsync(message.Id, expiredBefore)).ShouldBeRight().ShouldBeTrue();
        (await _store.TryClaimForReplayAsync(message.Id, expiredBefore)).ShouldBeRight().ShouldBeFalse();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ClockAdvancedPastTheLease_AllowsAReclaim()
    {
        var message = (await SeedAsync(NewMessage()))[0];
        (await _store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(-5))).ShouldBeRight().ShouldBeTrue();
        _time.Advance(TimeSpan.FromMinutes(10));

        (await _store.TryClaimForReplayAsync(message.Id, Now.AddMinutes(5))).ShouldBeRight().ShouldBeTrue();

        (await ReadAsync(message.Id)).ReplayClaimedAtUtc.ShouldBe(Now.AddMinutes(10));
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ExpiryOfUnspecifiedKind_IsReadAsUtc()
    {
        var message = (await SeedAsync(NewMessage(customize: m => m.ReplayClaimedAtUtc = Now.AddMinutes(-10))))[0];
        var unspecified = DateTime.SpecifyKind(Now.AddMinutes(-5), DateTimeKind.Unspecified);

        (await _store.TryClaimForReplayAsync(message.Id, unspecified)).ShouldBeRight().ShouldBeTrue();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_ExpiryOfLocalKind_IsConvertedToUtc()
    {
        var message = (await SeedAsync(NewMessage(customize: m => m.ReplayClaimedAtUtc = Now.AddMinutes(-10))))[0];
        var local = Now.AddMinutes(-5).ToLocalTime();

        (await _store.TryClaimForReplayAsync(message.Id, local)).ShouldBeRight().ShouldBeTrue();
    }

    [Fact]
    public async Task TryClaimForReplayAsync_StoreFailure_ReturnsClaimFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.TryClaimForReplayAsync(Guid.NewGuid(), Now))).ShouldBe(DeadLetterErrorCodes.ClaimFailed);
    }

    // ---- MarkAsReplayedAsync ----

    [Fact]
    public async Task MarkAsReplayedAsync_Pending_RecordsTheClockTimeAndTheOutcome()
    {
        var message = (await SeedAsync(NewMessage()))[0];

        (await _store.MarkAsReplayedAsync(message.Id, "success")).ShouldBeRight().ShouldBeTrue();

        var stored = await ReadAsync(message.Id);
        stored.ReplayedAtUtc.ShouldBe(Now);
        stored.ReplayResult.ShouldBe("success");
        stored.IsReplayed.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsReplayedAsync_AlreadyReplayed_ReturnsFalseAndKeepsTheFirstOutcome()
    {
        var firstReplay = Now.AddHours(-1);
        var message = (await SeedAsync(NewMessage(customize: m =>
        {
            m.ReplayedAtUtc = firstReplay;
            m.ReplayResult = "first";
        })))[0];

        (await _store.MarkAsReplayedAsync(message.Id, "second")).ShouldBeRight().ShouldBeFalse();

        var stored = await ReadAsync(message.Id);
        stored.ReplayResult.ShouldBe("first");
        stored.ReplayedAtUtc.ShouldBe(firstReplay);
    }

    [Fact]
    public async Task MarkAsReplayedAsync_UnknownId_ReturnsFalse()
    {
        await SeedAsync(NewMessage());

        (await _store.MarkAsReplayedAsync(Guid.NewGuid(), "success")).ShouldBeRight().ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsReplayedAsync_StoreFailure_ReturnsMarkReplayedFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.MarkAsReplayedAsync(Guid.NewGuid(), "success"))).ShouldBe(DeadLetterErrorCodes.MarkReplayedFailed);
    }

    // ---- DeleteAsync / DeleteManyAsync / DeleteExpiredAsync ----

    [Fact]
    public async Task DeleteAsync_Existing_RemovesOnlyThatMessage()
    {
        var seeded = await SeedAsync(NewMessage("a"), NewMessage("b"));

        (await _store.DeleteAsync(seeded[0].Id)).ShouldBeRight().ShouldBeTrue();

        (await ReadAllAsync()).Select(m => m.Id).ShouldBe([seeded[1].Id]);
    }

    [Fact]
    public async Task DeleteAsync_Unknown_ReturnsFalse()
    {
        await SeedAsync(NewMessage());

        (await _store.DeleteAsync(Guid.NewGuid())).ShouldBeRight().ShouldBeFalse();

        (await ReadAllAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_StoreFailure_ReturnsDeleteFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.DeleteAsync(Guid.NewGuid()))).ShouldBe(DeadLetterErrorCodes.DeleteFailed);
    }

    [Fact]
    public async Task DeleteManyAsync_Filter_DeletesAndCountsOnlyMatches()
    {
        await SeedCatalogAsync();

        (await _store.DeleteManyAsync(new DeadLetterFilter { SourcePattern = "Outbox" })).ShouldBeRight().ShouldBe(3);

        (await ReadAllAsync()).Select(m => m.SourceMessageId).OrderBy(s => s).ShouldBe(["b", "e"]);
    }

    [Fact]
    public async Task DeleteManyAsync_FilterWithoutMatch_DeletesNothing()
    {
        await SeedCatalogAsync();

        (await _store.DeleteManyAsync(DeadLetterFilter.FromSource("Nope"))).ShouldBeRight().ShouldBe(0);

        (await ReadAllAsync()).Count.ShouldBe(5);
    }

    [Fact]
    public async Task DeleteManyAsync_StoreFailure_ReturnsDeleteFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.DeleteManyAsync(DeadLetterFilter.All))).ShouldBe(DeadLetterErrorCodes.DeleteFailed);
    }

    [Fact]
    public async Task DeleteExpiredAsync_RemovesExpiredAndKeepsFutureAndUnexpiringMessages()
    {
        await SeedAsync(
            NewMessage("past", customize: m => m.ExpiresAtUtc = Now.AddMinutes(-1)),
            NewMessage("boundary", customize: m => m.ExpiresAtUtc = Now),
            NewMessage("future", customize: m => m.ExpiresAtUtc = Now.AddMinutes(1)),
            NewMessage("never"));

        (await _store.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(2);

        (await ReadAllAsync()).Select(m => m.SourceMessageId).OrderBy(s => s).ShouldBe(["future", "never"]);
    }

    [Fact]
    public async Task DeleteExpiredAsync_ClockAdvanced_RemovesWhatExpiredInTheMeantime()
    {
        await SeedAsync(NewMessage("soon", customize: m => m.ExpiresAtUtc = Now.AddMinutes(30)));

        (await _store.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(0);
        _time.Advance(TimeSpan.FromMinutes(31));
        (await _store.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(1);
    }

    [Fact]
    public async Task DeleteExpiredAsync_StoreFailure_ReturnsCleanupFailedLeft()
    {
        var (store, cleanup) = BrokenStore();
        using var _ = new ActionDisposable(cleanup);

        (await LeftCode(store.DeleteExpiredAsync())).ShouldBe(DeadLetterErrorCodes.CleanupFailed);
    }

    // ---- helpers ----

    private static T SomeOf<T>(Option<T> option)
    {
        option.IsSome.ShouldBeTrue();
        return option.Match(value => value, () => throw new InvalidOperationException("Option is None."));
    }

    private static async Task ShouldThrowCancellation<T>(Func<Task<T>> call)
    {
        var thrown = await Should.ThrowAsync<Exception>(call);
        thrown.ShouldBeAssignableTo<OperationCanceledException>();
    }

    private sealed class ActionDisposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
