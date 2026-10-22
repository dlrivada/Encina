using Encina.Security.BreakTheGlass;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>Unit tests for <see cref="InMemoryBreakTheGlassStore"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class InMemoryBreakTheGlassStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private readonly InMemoryBreakTheGlassStore _sut = new();

    private static T Right<T>(Either<EncinaError, T> either) =>
        either.Match(Right: r => r, Left: e => throw new InvalidOperationException(e.GetCode().IfNone("?")));

    private async Task<BreakTheGlassElevation> AddAsync(Action<BreakTheGlassElevation>? _ = null, string? tenant = "tenant-1", string user = "clin-1", string subject = "patient-1", DateTimeOffset? opened = null, TimeSpan? duration = null)
    {
        var elevation = BreakTheGlassTestKit.Elevation(opened ?? Now, userId: user, subjectId: subject, tenantId: tenant, duration: duration);
        Right(await _sut.AddAsync(elevation));
        return elevation;
    }

    [Fact]
    public async Task GetById_ExistingAndMissing()
    {
        var added = await AddAsync();

        Right(await _sut.GetByIdAsync("tenant-1", added.ElevationId)).IsSome.ShouldBeTrue();
        Right(await _sut.GetByIdAsync("tenant-1", Guid.NewGuid())).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetById_OtherTenant_IsNotVisible()
    {
        var added = await AddAsync(tenant: "tenant-1");

        Right(await _sut.GetByIdAsync("tenant-2", added.ElevationId)).IsNone.ShouldBeTrue();
        Right(await _sut.GetByIdAsync(null, added.ElevationId)).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task FindEffective_MatchesOnlyExactTenantUserSubjectScopeAndTime()
    {
        await AddAsync();

        Right(await _sut.FindEffectiveAsync("tenant-1", "clin-1", "patient-1", BreakTheGlassTestKit.Scope, Now.AddMinutes(1))).IsSome.ShouldBeTrue();
        Right(await _sut.FindEffectiveAsync("tenant-2", "clin-1", "patient-1", BreakTheGlassTestKit.Scope, Now.AddMinutes(1))).IsNone.ShouldBeTrue();
        Right(await _sut.FindEffectiveAsync("tenant-1", "clin-2", "patient-1", BreakTheGlassTestKit.Scope, Now.AddMinutes(1))).IsNone.ShouldBeTrue();
        Right(await _sut.FindEffectiveAsync("tenant-1", "clin-1", "patient-2", BreakTheGlassTestKit.Scope, Now.AddMinutes(1))).IsNone.ShouldBeTrue();
        Right(await _sut.FindEffectiveAsync("tenant-1", "clin-1", "patient-1", "other", Now.AddMinutes(1))).IsNone.ShouldBeTrue();
        Right(await _sut.FindEffectiveAsync("tenant-1", "clin-1", "patient-1", BreakTheGlassTestKit.Scope, Now.AddHours(1))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task FindEffective_ClosedElevation_IsNotFound()
    {
        var added = await AddAsync();
        Right(await _sut.TryCloseAsync("tenant-1", added.ElevationId, BreakTheGlassStatus.Revoked, "r", Now));

        Right(await _sut.FindEffectiveAsync("tenant-1", "clin-1", "patient-1", BreakTheGlassTestKit.Scope, Now)).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task TryClose_AppliesOnce_AndOnlyFromActive()
    {
        var added = await AddAsync();

        Right(await _sut.TryCloseAsync("tenant-1", added.ElevationId, BreakTheGlassStatus.Revoked, "first", Now.AddMinutes(5))).ShouldBeTrue();
        Right(await _sut.TryCloseAsync("tenant-1", added.ElevationId, BreakTheGlassStatus.Expired, "second", Now.AddMinutes(6))).ShouldBeFalse();

        var stored = Right(await _sut.GetByIdAsync("tenant-1", added.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!;
        stored.Status.ShouldBe(BreakTheGlassStatus.Revoked);
        stored.CloseReason.ShouldBe("first");
        stored.ClosedAtUtc.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public async Task TryClose_MissingOrOtherTenant_ReturnsFalse()
    {
        var added = await AddAsync();

        Right(await _sut.TryCloseAsync("tenant-1", Guid.NewGuid(), BreakTheGlassStatus.Revoked, "r", Now)).ShouldBeFalse();
        Right(await _sut.TryCloseAsync("tenant-2", added.ElevationId, BreakTheGlassStatus.Revoked, "r", Now)).ShouldBeFalse();
    }

    [Fact]
    public async Task TryClose_ConcurrentCallers_ExactlyOneWins()
    {
        var added = await AddAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(i =>
            Task.Run(async () => Right(await _sut.TryCloseAsync("tenant-1", added.ElevationId, BreakTheGlassStatus.Expired, $"r{i}", Now)))));

        results.Count(applied => applied).ShouldBe(1);
    }

    [Fact]
    public async Task TryRecordReview_AppliesOnce_AndOnlyWhenUnreviewed()
    {
        var added = await AddAsync();

        Right(await _sut.TryRecordReviewAsync("tenant-1", added.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev-1", "ok", Now)).ShouldBeTrue();
        Right(await _sut.TryRecordReviewAsync("tenant-1", added.ElevationId, BreakTheGlassReviewOutcome.Unjustified, "rev-2", null, Now)).ShouldBeFalse();

        var stored = Right(await _sut.GetByIdAsync("tenant-1", added.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!;
        stored.ReviewOutcome.ShouldBe(BreakTheGlassReviewOutcome.Justified);
        stored.ReviewedByUserId.ShouldBe("rev-1");
        stored.ReviewNote.ShouldBe("ok");
    }

    [Fact]
    public async Task TryRecordReview_MissingRow_ReturnsFalse()
    {
        Right(await _sut.TryRecordReviewAsync("tenant-1", Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, "rev", null, Now)).ShouldBeFalse();
    }

    [Fact]
    public async Task GetExpiredUnrecorded_ReturnsActivePastExpiryAndExpiredWithoutRecord()
    {
        var pastActive = await AddAsync(subject: "p1", duration: TimeSpan.FromMinutes(10));
        var futureActive = await AddAsync(subject: "p2", duration: TimeSpan.FromHours(5));
        var closedUnrecorded = await AddAsync(subject: "p3", duration: TimeSpan.FromMinutes(10));
        var closedRecorded = await AddAsync(subject: "p4", duration: TimeSpan.FromMinutes(10));
        var revoked = await AddAsync(subject: "p5", duration: TimeSpan.FromMinutes(10));
        Right(await _sut.TryCloseAsync("tenant-1", closedUnrecorded.ElevationId, BreakTheGlassStatus.Expired, "expired", Now));
        Right(await _sut.TryCloseAsync("tenant-1", closedRecorded.ElevationId, BreakTheGlassStatus.Expired, "expired", Now));
        Right(await _sut.MarkExpiryRecordedAsync("tenant-1", closedRecorded.ElevationId, Now));
        Right(await _sut.TryCloseAsync("tenant-1", revoked.ElevationId, BreakTheGlassStatus.Revoked, "r", Now));

        var due = Right(await _sut.GetExpiredUnrecordedAsync(Now.AddMinutes(30), 10));

        due.Select(e => e.ElevationId).ShouldBe([pastActive.ElevationId, closedUnrecorded.ElevationId], ignoreOrder: true);
        due.ShouldNotContain(e => e.ElevationId == futureActive.ElevationId);
    }

    [Fact]
    public async Task GetExpiredUnrecorded_SpansTenantsAndHonoursBatchSize()
    {
        await AddAsync(tenant: "t1", subject: "a", duration: TimeSpan.FromMinutes(1));
        await AddAsync(tenant: "t2", subject: "b", duration: TimeSpan.FromMinutes(2));
        await AddAsync(tenant: null, subject: "c", duration: TimeSpan.FromMinutes(3));

        Right(await _sut.GetExpiredUnrecordedAsync(Now.AddHours(1), 10)).Count.ShouldBe(3);
        var first = Right(await _sut.GetExpiredUnrecordedAsync(Now.AddHours(1), 2));
        first.Count.ShouldBe(2);
        first[0].ExpiresAtUtc.ShouldBeLessThan(first[1].ExpiresAtUtc);
        Should.Throw<ArgumentOutOfRangeException>(async () => await _sut.GetExpiredUnrecordedAsync(Now, 0));
    }

    [Fact]
    public async Task MarkExpiryRecorded_OnlyAffectsExpiredRows()
    {
        var active = await AddAsync(subject: "a");
        var expired = await AddAsync(subject: "b");
        Right(await _sut.TryCloseAsync("tenant-1", expired.ElevationId, BreakTheGlassStatus.Expired, "expired", Now));

        Right(await _sut.MarkExpiryRecordedAsync("tenant-1", active.ElevationId, Now.AddMinutes(1)));
        Right(await _sut.MarkExpiryRecordedAsync("tenant-1", expired.ElevationId, Now.AddMinutes(2)));

        Right(await _sut.GetByIdAsync("tenant-1", active.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!.ExpiryRecordedAtUtc.ShouldBeNull();
        Right(await _sut.GetByIdAsync("tenant-1", expired.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!.ExpiryRecordedAtUtc.ShouldBe(Now.AddMinutes(2));
    }

    [Fact]
    public async Task ReviewQueue_UnreviewedOnly_OldestFirst_TenantScoped()
    {
        var newest = await AddAsync(subject: "n", opened: Now.AddMinutes(30));
        var oldest = await AddAsync(subject: "o", opened: Now);
        var middle = await AddAsync(subject: "m", opened: Now.AddMinutes(10));
        var reviewed = await AddAsync(subject: "r", opened: Now.AddMinutes(5));
        await AddAsync(tenant: "tenant-2", subject: "x", opened: Now);
        Right(await _sut.TryRecordReviewAsync("tenant-1", reviewed.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now));

        var page = Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery()));

        page.Items.Select(e => e.ElevationId).ShouldBe([oldest.ElevationId, middle.ElevationId, newest.ElevationId]);
        page.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task ReviewQueue_IncludesClosedUnreviewed_AndFiltersByStatusScopeAndDates()
    {
        var active = await AddAsync(subject: "a", opened: Now);
        var closed = await AddAsync(subject: "b", opened: Now.AddHours(1));
        Right(await _sut.TryCloseAsync("tenant-1", closed.ElevationId, BreakTheGlassStatus.Revoked, "r", Now));

        Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery())).TotalCount.ShouldBe(2);
        Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery { Status = BreakTheGlassStatus.Revoked })).Items.Single().ElevationId.ShouldBe(closed.ElevationId);
        Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery { Scope = "other" })).IsEmpty.ShouldBeTrue();
        Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery { OpenedFromUtc = Now.AddMinutes(30) })).Items.Single().ElevationId.ShouldBe(closed.ElevationId);
        Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery { OpenedToUtc = Now.AddMinutes(30) })).Items.Single().ElevationId.ShouldBe(active.ElevationId);
    }

    [Fact]
    public async Task ReviewQueue_Pages()
    {
        for (var i = 0; i < 5; i++)
        {
            await AddAsync(subject: $"s{i}", opened: Now.AddMinutes(i));
        }

        var second = Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery { PageNumber = 2, PageSize = 2 }));

        second.Count.ShouldBe(2);
        second.TotalCount.ShouldBe(5);
        second.TotalPages.ShouldBe(3);
        second.Items[0].OpenedAtUtc.ShouldBe(Now.AddMinutes(2));
        Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery { PageNumber = 0, PageSize = 0 })).PageSize.ShouldBe(1);
        Right(await _sut.QueryReviewQueueAsync("tenant-1", new BreakTheGlassReviewQuery { PageSize = 9999 })).PageSize.ShouldBe(500);
    }

    [Fact]
    public async Task Purge_RemovesOnlyClosedAndReviewedBeforeCutoff()
    {
        var closedReviewed = await AddAsync(subject: "a");
        var closedUnreviewed = await AddAsync(subject: "b");
        var activeReviewed = await AddAsync(subject: "c");
        var closedReviewedRecent = await AddAsync(subject: "d");
        Right(await _sut.TryCloseAsync("tenant-1", closedReviewed.ElevationId, BreakTheGlassStatus.Revoked, "r", Now));
        Right(await _sut.TryRecordReviewAsync("tenant-1", closedReviewed.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now));
        Right(await _sut.TryCloseAsync("tenant-1", closedUnreviewed.ElevationId, BreakTheGlassStatus.Expired, "e", Now));
        Right(await _sut.TryRecordReviewAsync("tenant-1", activeReviewed.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now));
        Right(await _sut.TryCloseAsync("tenant-1", closedReviewedRecent.ElevationId, BreakTheGlassStatus.Revoked, "r", Now));
        Right(await _sut.TryRecordReviewAsync("tenant-1", closedReviewedRecent.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now.AddDays(10)));

        Right(await _sut.PurgeAsync(Now.AddDays(1))).ShouldBe(1);

        Right(await _sut.GetByIdAsync("tenant-1", closedReviewed.ElevationId)).IsNone.ShouldBeTrue();
        Right(await _sut.GetByIdAsync("tenant-1", closedUnreviewed.ElevationId)).IsSome.ShouldBeTrue();
        Right(await _sut.GetByIdAsync("tenant-1", activeReviewed.ElevationId)).IsSome.ShouldBeTrue();
        Right(await _sut.GetByIdAsync("tenant-1", closedReviewedRecent.ElevationId)).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task Purge_KeepsAnExpiredRowWhoseExpiryWasNeverRecorded_UntilItIs()
    {
        var expired = await AddAsync(subject: "a");
        Right(await _sut.TryCloseAsync("tenant-1", expired.ElevationId, BreakTheGlassStatus.Expired, "e", Now));
        Right(await _sut.TryRecordReviewAsync("tenant-1", expired.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now));

        Right(await _sut.PurgeAsync(Now.AddDays(1))).ShouldBe(0);
        Right(await _sut.MarkExpiryRecordedAsync("tenant-1", expired.ElevationId, Now));
        Right(await _sut.PurgeAsync(Now.AddDays(1))).ShouldBe(1);
    }

    [Fact]
    public async Task Add_WhileAnEffectiveElevationExists_IsRefusedAndWritesNothing()
    {
        var first = await AddAsync();
        var duplicate = BreakTheGlassTestKit.Elevation(Now.AddMinutes(5), userId: "clin-1", subjectId: "patient-1", tenantId: "tenant-1");

        var result = await _sut.AddAsync(duplicate);

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty)).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
        Right(await _sut.GetByIdAsync("tenant-1", duplicate.ElevationId)).IsNone.ShouldBeTrue();
        Right(await _sut.GetByIdAsync("tenant-1", first.ElevationId)).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task Add_AfterExpiryOrForAnotherKey_IsAccepted()
    {
        await AddAsync(duration: TimeSpan.FromMinutes(10));

        Right(await _sut.AddAsync(BreakTheGlassTestKit.Elevation(Now.AddMinutes(10), userId: "clin-1", subjectId: "patient-1", tenantId: "tenant-1")));
        Right(await _sut.AddAsync(BreakTheGlassTestKit.Elevation(Now, userId: "clin-2", subjectId: "patient-1", tenantId: "tenant-1")));
        Right(await _sut.AddAsync(BreakTheGlassTestKit.Elevation(Now, userId: "clin-1", subjectId: "patient-1", tenantId: "tenant-2")));
    }

    [Fact]
    public async Task Add_ConcurrentSameKey_AdmitsExactlyOne()
    {
        var attempts = Enumerable.Range(0, 16)
            .Select(_ => BreakTheGlassTestKit.Elevation(Now, userId: "clin-1", subjectId: "patient-1", tenantId: "tenant-1"))
            .ToList();

        var results = await Task.WhenAll(attempts.Select(e => Task.Run(async () => (await _sut.AddAsync(e)).IsRight)));

        results.Count(r => r).ShouldBe(1);
    }

    [Fact]
    public async Task Probe_ReturnsRight()
    {
        (await _sut.ProbeAsync()).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await _sut.ProbeAsync(cts.Token));
    }
}
