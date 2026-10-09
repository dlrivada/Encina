using Encina.Messaging.DeadLetter;
using Encina.Testing.Shouldly;
using LanguageExt;
using Shouldly;
using Xunit;

namespace Encina.ContractTests.Messaging.DeadLetter;

/// <summary>
/// The behavior every <see cref="IDeadLetterStore"/> must show, whatever its persistence. The fake store
/// runs it as a contract test; the integration tests of the ten persistent stores derive from it and run
/// the same facts against real databases (this file is compiled into both test projects).
/// </summary>
/// <remarks>
/// A derived class supplies a store over an empty queue, a second independent store over the same data
/// (a second connection or context, for the concurrency facts) and a way to build the provider's message type.
/// Every mutation is followed by <see cref="IDeadLetterStore.SaveChangesAsync"/>, which is a no-op for the
/// stores that write immediately. Timestamps are whole seconds so that every provider (MongoDB keeps milliseconds)
/// round-trips them exactly.
/// </remarks>
public abstract class DeadLetterStoreContract : IAsyncLifetime
{
    private static readonly DateTime Start = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The controllable clock the store under test reads.</summary>
    protected ControllableClock Clock { get; } = new(Start);

    /// <summary>The store under test, created by <see cref="CreateStoreAsync"/>.</summary>
    protected IDeadLetterStore Store { get; private set; } = null!;

    /// <summary>The instant the clock started at.</summary>
    protected static DateTime StartUtc => Start;

    /// <summary>Creates the store under test over an empty queue, reading <paramref name="timeProvider"/>.</summary>
    protected abstract Task<IDeadLetterStore> CreateStoreAsync(TimeProvider timeProvider);

    /// <summary>Creates a second store over the same data (for the concurrency facts).</summary>
    protected abstract IDeadLetterStore CreateSecondStore();

    /// <summary>Builds the provider's message type from <paramref name="data"/>.</summary>
    protected abstract IDeadLetterMessage CreateMessage(DeadLetterData data);

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => Store = await CreateStoreAsync(Clock);

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ---------------------------------------------------------------- add, get

    [Fact]
    public async Task AddThenGet_RoundTripsEveryField()
    {
        var data = Data(
            sourceId: "source-1",
            tenantId: "tenant-a",
            correlationId: "corr-1",
            expiresAtUtc: Start.AddDays(7),
            content: new string('x', 100_000) + "ñ€😀",
            stackTrace: new string('s', 50_000));

        await AddAsync(data);

        var stored = await GetRequiredAsync(data.Id);
        stored.Id.ShouldBe(data.Id);
        stored.RequestType.ShouldBe(data.RequestType);
        stored.RequestContent.ShouldBe(data.RequestContent);
        stored.ErrorCode.ShouldBe(data.ErrorCode);
        stored.ExceptionType.ShouldBe(data.ExceptionType);
        stored.ExceptionStackTrace.ShouldBe(data.ExceptionStackTrace);
        stored.CorrelationId.ShouldBe("corr-1");
        stored.SourcePattern.ShouldBe(data.SourcePattern);
        stored.SourceMessageId.ShouldBe("source-1");
        stored.TenantId.ShouldBe("tenant-a");
        stored.TotalRetryAttempts.ShouldBe(data.TotalRetryAttempts);
        stored.FirstFailedAtUtc.ShouldBe(data.FirstFailedAtUtc);
        stored.DeadLetteredAtUtc.ShouldBe(data.DeadLetteredAtUtc);
        stored.ExpiresAtUtc.ShouldBe(data.ExpiresAtUtc);
        stored.ReplayClaimedAtUtc.ShouldBeNull();
        stored.ReplayedAtUtc.ShouldBeNull();
        stored.ReplayResult.ShouldBeNull();
        stored.IsReplayed.ShouldBeFalse();
    }

    [Fact]
    public async Task AddThenGet_WithOptionalFieldsNull_RoundTripsNulls()
    {
        var data = Data(tenantId: null, correlationId: null, expiresAtUtc: null, exceptionType: null, stackTrace: null);

        await AddAsync(data);

        var stored = await GetRequiredAsync(data.Id);
        stored.TenantId.ShouldBeNull();
        stored.CorrelationId.ShouldBeNull();
        stored.ExpiresAtUtc.ShouldBeNull();
        stored.ExceptionType.ShouldBeNull();
        stored.ExceptionStackTrace.ShouldBeNull();
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNone()
    {
        var result = (await Store.GetAsync(Guid.NewGuid())).ShouldBeRight();

        result.IsNone.ShouldBeTrue();
    }

    // ---------------------------------------------------------------- duplicates and case

    [Fact]
    public async Task Add_SameSourceKey_ReturnsFalseAndKeepsOneRow()
    {
        var first = Data(sourceId: "dup");
        var second = Data(sourceId: "dup");

        (await AddAsync(first)).ShouldBeTrue();
        (await AddAsync(second)).ShouldBeFalse();

        (await CountAsync(DeadLetterFilter.All)).ShouldBe(1);
        (await Store.GetAsync(second.Id)).ShouldBeRight().IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Add_SameSourceMessageIdUnderAnotherPattern_IsAnotherCapture()
    {
        (await AddAsync(Data(source: "Outbox", sourceId: "same"))).ShouldBeTrue();
        (await AddAsync(Data(source: "Inbox", sourceId: "same"))).ShouldBeTrue();

        (await CountAsync(DeadLetterFilter.All)).ShouldBe(2);
    }

    [Fact]
    public async Task Add_SourceIdsDifferingOnlyInCase_AreDistinct()
    {
        var upper = Data(sourceId: "Order-ABC");
        var lower = Data(sourceId: "order-abc");

        (await AddAsync(upper)).ShouldBeTrue();
        (await AddAsync(lower)).ShouldBeTrue();

        (await CountAsync(DeadLetterFilter.All)).ShouldBe(2);
        (await CountAsync(new DeadLetterFilter { SourceMessageId = "Order-ABC" })).ShouldBe(1);
        (await CountAsync(new DeadLetterFilter { SourceMessageId = "order-abc" })).ShouldBe(1);
    }

    [Fact]
    public async Task Filter_TenantIdsDifferingOnlyInCase_AreDistinct()
    {
        await AddAsync(Data(tenantId: "Tenant-A"));
        await AddAsync(Data(tenantId: "tenant-a"));

        (await CountAsync(new DeadLetterFilter { TenantId = "Tenant-A" })).ShouldBe(1);
    }

    [Fact]
    public async Task Add_ConcurrentCapturesOfOneSource_KeepExactlyOneRow()
    {
        var other = CreateSecondStore();
        var first = CreateMessage(Data(sourceId: "race"));
        var second = CreateMessage(Data(sourceId: "race"));

        var results = await Task.WhenAll(CaptureAsync(Store, first), CaptureAsync(other, second));

        results.Count(captured => captured).ShouldBe(1);
        (await CountAsync(DeadLetterFilter.All)).ShouldBe(1);
    }

    // ---------------------------------------------------------------- order and paging

    [Fact]
    public async Task GetMessages_ReturnsOldestFirstThenNewestFirst()
    {
        var oldest = Data(deadLetteredAtUtc: Start.AddMinutes(1));
        var middle = Data(deadLetteredAtUtc: Start.AddMinutes(2));
        var newest = Data(deadLetteredAtUtc: Start.AddMinutes(3));
        await AddAsync(middle);
        await AddAsync(newest);
        await AddAsync(oldest);

        var ascending = await ListAsync(null);
        var descending = await ListAsync(null, newestFirst: true);

        ascending.Select(m => m.Id).ShouldBe(new[] { oldest.Id, middle.Id, newest.Id });
        descending.Select(m => m.Id).ShouldBe(new[] { newest.Id, middle.Id, oldest.Id });
    }

    [Fact]
    public async Task GetMessages_PagesAreSortedDisjointAndSkipTake()
    {
        var ids = new List<Guid>();
        for (var i = 0; i < 7; i++)
        {
            var data = Data(deadLetteredAtUtc: Start.AddMinutes(i));
            ids.Add(data.Id);
            await AddAsync(data);
        }

        var pageOne = await ListAsync(null, skip: 0, take: 3);
        var pageTwo = await ListAsync(null, skip: 3, take: 3);
        var pageThree = await ListAsync(null, skip: 6, take: 3);

        pageOne.Select(m => m.Id).ShouldBe(ids.Take(3));
        pageTwo.Select(m => m.Id).ShouldBe(ids.Skip(3).Take(3));
        pageThree.Select(m => m.Id).ShouldBe(ids.Skip(6));
    }

    [Fact]
    public async Task GetCount_EqualsTheRowsPagedAcrossMoreThanOnePage()
    {
        var total = DeadLetterStoreLimits.MaxPageSize + 5;
        for (var i = 0; i < total; i++)
        {
            (await Store.AddAsync(CreateMessage(Data(deadLetteredAtUtc: Start.AddSeconds(i))))).ShouldBeRight();
        }

        (await Store.SaveChangesAsync()).ShouldBeRight();

        var paged = new List<Guid>();
        while (true)
        {
            var page = await ListAsync(null, skip: paged.Count, take: DeadLetterStoreLimits.MaxPageSize);
            paged.AddRange(page.Select(m => m.Id));
            if (page.Count < DeadLetterStoreLimits.MaxPageSize)
            {
                break;
            }
        }

        paged.Count.ShouldBe(total);
        paged.Distinct().Count().ShouldBe(total);
        (await CountAsync(DeadLetterFilter.All)).ShouldBe(total);
    }

    // ---------------------------------------------------------------- filters

    [Fact]
    public async Task Filter_ByEachIdentityField_MatchesOnlyThoseRows()
    {
        var a = Data(source: "Outbox", sourceId: "s-a", requestType: "T.A", errorCode: "e.a", correlationId: "c-a", tenantId: "t-a");
        var b = Data(source: "Inbox", sourceId: "s-b", requestType: "T.B", errorCode: "e.b", correlationId: "c-b", tenantId: "t-b");
        await AddAsync(a);
        await AddAsync(b);

        await AssertMatchesAsync(new DeadLetterFilter { SourcePattern = "Outbox" }, a);
        await AssertMatchesAsync(new DeadLetterFilter { RequestType = "T.B" }, b);
        await AssertMatchesAsync(new DeadLetterFilter { ErrorCode = "e.a" }, a);
        await AssertMatchesAsync(new DeadLetterFilter { CorrelationId = "c-b" }, b);
        await AssertMatchesAsync(new DeadLetterFilter { TenantId = "t-a" }, a);
        await AssertMatchesAsync(new DeadLetterFilter { SourceMessageId = "s-b" }, b);
        await AssertMatchesAsync(new DeadLetterFilter { SourcePattern = "Outbox", TenantId = "t-b" });
    }

    [Fact]
    public async Task Filter_ByReplayStateAndTimeWindow_MatchesOnlyThoseRows()
    {
        var early = Data(deadLetteredAtUtc: Start.AddHours(1));
        var late = Data(deadLetteredAtUtc: Start.AddHours(3));
        await AddAsync(early);
        await AddAsync(late);
        (await Store.MarkAsReplayedAsync(early.Id, "success")).ShouldBeRight().ShouldBeTrue();
        await SaveAsync();

        await AssertMatchesAsync(new DeadLetterFilter { ExcludeReplayed = true }, late);
        await AssertMatchesAsync(new DeadLetterFilter { ExcludeReplayed = false }, early);
        await AssertMatchesAsync(new DeadLetterFilter { DeadLetteredAfterUtc = Start.AddHours(1) }, early, late);
        await AssertMatchesAsync(new DeadLetterFilter { DeadLetteredAfterUtc = Start.AddHours(2) }, late);
        await AssertMatchesAsync(new DeadLetterFilter { DeadLetteredBeforeUtc = Start.AddHours(3) }, early, late);
        await AssertMatchesAsync(new DeadLetterFilter { DeadLetteredBeforeUtc = Start.AddHours(2) }, early);
    }

    [Fact]
    public async Task Filter_ByExpiry_MatchesOnlySetExpiriesAtOrBefore()
    {
        var soon = Data(expiresAtUtc: Start.AddDays(1));
        var later = Data(expiresAtUtc: Start.AddDays(5));
        var never = Data(expiresAtUtc: null);
        await AddAsync(soon);
        await AddAsync(later);
        await AddAsync(never);

        await AssertMatchesAsync(new DeadLetterFilter { ExpiresAtOrBeforeUtc = Start.AddDays(1) }, soon);
        await AssertMatchesAsync(new DeadLetterFilter { ExpiresAtOrBeforeUtc = Start.AddDays(9) }, soon, later);
        await AssertMatchesAsync(new DeadLetterFilter { ExpiresAtOrBeforeUtc = Start });
    }

    [Fact]
    public async Task Filter_AllTenantsFlagIsIgnoredByTheStore()
    {
        await AddAsync(Data(tenantId: "t-a"));
        await AddAsync(Data(tenantId: "t-b"));

        (await CountAsync(new DeadLetterFilter { AllTenants = true })).ShouldBe(2);
        (await CountAsync(new DeadLetterFilter { AllTenants = true, TenantId = "t-a" })).ShouldBe(1);
    }

    // ---------------------------------------------------------------- replay

    [Fact]
    public async Task MarkAsReplayed_IsTrueOnceThenFalse_AndStoresTheOutcomeCode()
    {
        var data = Data();
        await AddAsync(data);
        Clock.Advance(TimeSpan.FromMinutes(5));
        var outcome = new string('o', DeadLetterStoreLimits.ReplayResultMaxLength);

        (await Store.MarkAsReplayedAsync(data.Id, outcome)).ShouldBeRight().ShouldBeTrue();
        await SaveAsync();
        (await Store.MarkAsReplayedAsync(data.Id, "second")).ShouldBeRight().ShouldBeFalse();
        await SaveAsync();

        var stored = await GetRequiredAsync(data.Id);
        stored.IsReplayed.ShouldBeTrue();
        stored.ReplayedAtUtc.ShouldBe(Start.AddMinutes(5));
        stored.ReplayResult.ShouldBe(outcome);
    }

    [Fact]
    public async Task MarkAsReplayed_UnknownId_ReturnsFalse()
    {
        (await Store.MarkAsReplayedAsync(Guid.NewGuid(), "success")).ShouldBeRight().ShouldBeFalse();
    }

    [Fact]
    public async Task TryClaimForReplay_IsWonOnceAndCanBeWonAgainAfterTheClaimExpires()
    {
        var data = Data();
        await AddAsync(data);
        var claimedAt = Start;

        (await Store.TryClaimForReplayAsync(data.Id, claimedAt.AddMinutes(-5))).ShouldBeRight().ShouldBeTrue();
        (await Store.TryClaimForReplayAsync(data.Id, claimedAt.AddMinutes(-5))).ShouldBeRight().ShouldBeFalse();
        (await GetRequiredAsync(data.Id)).ReplayClaimedAtUtc.ShouldBe(claimedAt);

        Clock.Advance(TimeSpan.FromMinutes(10));
        (await Store.TryClaimForReplayAsync(data.Id, Start.AddMinutes(5))).ShouldBeRight().ShouldBeTrue();
        (await GetRequiredAsync(data.Id)).ReplayClaimedAtUtc.ShouldBe(Start.AddMinutes(10));
    }

    [Fact]
    public async Task TryClaimForReplay_ReplayedOrUnknownMessage_ReturnsFalse()
    {
        var data = Data();
        await AddAsync(data);
        (await Store.MarkAsReplayedAsync(data.Id, "success")).ShouldBeRight().ShouldBeTrue();
        await SaveAsync();

        (await Store.TryClaimForReplayAsync(data.Id, Start.AddDays(1))).ShouldBeRight().ShouldBeFalse();
        (await Store.TryClaimForReplayAsync(Guid.NewGuid(), Start.AddDays(1))).ShouldBeRight().ShouldBeFalse();
    }

    [Fact]
    public async Task TryClaimForReplay_TwoConcurrentCallers_ExactlyOneWins()
    {
        var data = Data();
        await AddAsync(data);
        var other = CreateSecondStore();

        var wins = await Task.WhenAll(
            Store.TryClaimForReplayAsync(data.Id, Start.AddMinutes(-5)),
            other.TryClaimForReplayAsync(data.Id, Start.AddMinutes(-5)));

        wins.Count(result => result.ShouldBeRight()).ShouldBe(1);
    }

    // ---------------------------------------------------------------- delete

    [Fact]
    public async Task Delete_IsTrueOnceThenFalse()
    {
        var data = Data();
        await AddAsync(data);

        (await Store.DeleteAsync(data.Id)).ShouldBeRight().ShouldBeTrue();
        (await Store.DeleteAsync(data.Id)).ShouldBeRight().ShouldBeFalse();
        (await Store.GetAsync(data.Id)).ShouldBeRight().IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteMany_RemovesExactlyTheMatchingRows()
    {
        await AddAsync(Data(tenantId: "t-a"));
        await AddAsync(Data(tenantId: "t-a"));
        await AddAsync(Data(tenantId: "t-b"));

        var deleted = (await Store.DeleteManyAsync(new DeadLetterFilter { TenantId = "t-a" })).ShouldBeRight();

        deleted.ShouldBe(2);
        (await CountAsync(DeadLetterFilter.All)).ShouldBe(1);
        (await CountAsync(new DeadLetterFilter { TenantId = "t-b" })).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteMany_WithAllFilter_DeletesTheWholeQueue()
    {
        await AddAsync(Data());
        await AddAsync(Data());
        await AddAsync(Data(tenantId: "t-a"));

        (await Store.DeleteManyAsync(DeadLetterFilter.All)).ShouldBeRight().ShouldBe(3);

        (await CountAsync(DeadLetterFilter.All)).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteExpired_RemovesRowsExpiredAtOrBeforeNowOnly()
    {
        var expired = Data(expiresAtUtc: Start.AddMinutes(-1));
        var expiresNow = Data(expiresAtUtc: Start);
        var future = Data(expiresAtUtc: Start.AddSeconds(1));
        var never = Data(expiresAtUtc: null);
        await AddAsync(expired);
        await AddAsync(expiresNow);
        await AddAsync(future);
        await AddAsync(never);

        (await Store.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(2);

        var remaining = (await ListAsync(null)).Select(m => m.Id).ToHashSet();
        remaining.ShouldBe(new[] { future.Id, never.Id }, ignoreOrder: true);

        Clock.Advance(TimeSpan.FromSeconds(1));
        (await Store.DeleteExpiredAsync()).ShouldBeRight().ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_WithNothingPending_ReturnsRight()
    {
        (await Store.SaveChangesAsync()).ShouldBeRight();
    }

    // ---------------------------------------------------------------- arguments (validated before any I/O)

    [Fact]
    public async Task GetMessages_InvalidSkipOrTake_Throws()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await Store.GetMessagesAsync(null, skip: -1));
        await Should.ThrowAsync<ArgumentException>(async () => await Store.GetMessagesAsync(null, take: 0));
        await Should.ThrowAsync<ArgumentException>(async () => await Store.GetMessagesAsync(null, take: DeadLetterStoreLimits.MaxPageSize + 1));
    }

    [Fact]
    public async Task EmptyMessageId_Throws()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await Store.GetAsync(Guid.Empty));
        await Should.ThrowAsync<ArgumentException>(async () => await Store.DeleteAsync(Guid.Empty));
        await Should.ThrowAsync<ArgumentException>(async () => await Store.TryClaimForReplayAsync(Guid.Empty, Start));
        await Should.ThrowAsync<ArgumentException>(async () => await Store.MarkAsReplayedAsync(Guid.Empty, "success"));
    }

    [Fact]
    public async Task MarkAsReplayed_BlankOutcome_Throws()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await Store.MarkAsReplayedAsync(Guid.NewGuid(), " "));
        await Should.ThrowAsync<ArgumentException>(async () => await Store.MarkAsReplayedAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () => await Store.AddAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(async () => await Store.DeleteManyAsync(null!));
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Builds a record with unique ids and timestamps in whole seconds.</summary>
    protected static DeadLetterData Data(
        string source = "Outbox",
        string? sourceId = null,
        string requestType = "App.Orders.PlaceOrder",
        string errorCode = "outbox.max_retries",
        string? correlationId = "corr",
        string? tenantId = "tenant",
        DateTime? deadLetteredAtUtc = null,
        DateTime? expiresAtUtc = null,
        string? exceptionType = "System.InvalidOperationException",
        string? stackTrace = "at App.Handler()",
        string content = "{\"orderId\":1}")
    {
        var id = Guid.NewGuid();
        var at = deadLetteredAtUtc ?? Start;

        return new DeadLetterData(
            Id: id,
            RequestType: requestType,
            RequestContent: content,
            ErrorCode: errorCode,
            SourcePattern: source,
            SourceMessageId: sourceId ?? id.ToString("D"),
            TotalRetryAttempts: 5,
            FirstFailedAtUtc: at.AddMinutes(-10),
            DeadLetteredAtUtc: at,
            ExpiresAtUtc: expiresAtUtc,
            CorrelationId: correlationId,
            ExceptionType: exceptionType,
            ExceptionStackTrace: stackTrace,
            TenantId: tenantId);
    }

    private async Task<bool> AddAsync(DeadLetterData data)
    {
        var added = (await Store.AddAsync(CreateMessage(data))).ShouldBeRight();
        await SaveAsync();

        return added;
    }

    private async Task SaveAsync() => (await Store.SaveChangesAsync()).ShouldBeRight();

    private static async Task<bool> CaptureAsync(IDeadLetterStore store, IDeadLetterMessage message)
    {
        var added = await store.AddAsync(message);
        if (!added.IsRight || !added.ShouldBeRight())
        {
            return false;
        }

        // A race that the unique index decides at write time surfaces as a Left from SaveChanges (EF Core).
        return (await store.SaveChangesAsync()).IsRight;
    }

    private async Task<IDeadLetterMessage> GetRequiredAsync(Guid id)
    {
        var found = (await Store.GetAsync(id)).ShouldBeRight();

        return found.Match(message => message, () => throw new Xunit.Sdk.XunitException($"Message {id} was not found."));
    }

    private async Task<List<IDeadLetterMessage>> ListAsync(DeadLetterFilter? filter, int skip = 0, int take = 100, bool newestFirst = false)
        => (await Store.GetMessagesAsync(filter, skip, take, newestFirst)).ShouldBeRight().ToList();

    private async Task<int> CountAsync(DeadLetterFilter filter) => (await Store.GetCountAsync(filter)).ShouldBeRight();

    private async Task AssertMatchesAsync(DeadLetterFilter filter, params DeadLetterData[] expected)
    {
        var rows = await ListAsync(filter);

        rows.Select(m => m.Id).ShouldBe(expected.Select(e => e.Id), ignoreOrder: true);
        (await CountAsync(filter)).ShouldBe(expected.Length);
    }

    /// <summary>A <see cref="TimeProvider"/> that only moves when the test advances it.</summary>
    protected sealed class ControllableClock : TimeProvider
    {
        private DateTime _utcNow;

        public ControllableClock(DateTime utcNow) => _utcNow = utcNow;

        public void Advance(TimeSpan by) => _utcNow += by;

        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
