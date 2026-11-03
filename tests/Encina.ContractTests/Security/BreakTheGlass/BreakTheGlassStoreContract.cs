using Encina.Security.BreakTheGlass;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace Encina.ContractTests.Security.BreakTheGlass;

/// <summary>
/// The behavior every <see cref="IBreakTheGlassStore"/> must show, whatever its persistence. The in-memory
/// store runs it as a contract test; the integration tests of the persistent stores derive from it and run the
/// same facts against real databases (this file is compiled into both test projects).
/// </summary>
/// <remarks>
/// A derived class supplies a store over an empty table and a second independent store over the same data (a
/// second connection or context, for the concurrency facts). Every instant comes from <see cref="Clock"/>, a
/// <see cref="FakeTimeProvider"/> that starts on a whole second, so every provider (MongoDB keeps milliseconds)
/// round-trips the timestamps exactly.
/// </remarks>
public abstract class BreakTheGlassStoreContract : IAsyncLifetime
{
    private const string Tenant = "tenant-1";
    private const string OtherTenant = "tenant-2";
    private const string User = "clin-1";
    private const string Subject = "patient-1";
    private const string Scope = "clinical-record";
    private const string Justification = "Patient arrived unconscious and the treating psychologist is away.";

    private static readonly DateTimeOffset Start = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly List<IDisposable> _tracked = [];

    /// <summary>The controllable clock every instant of the facts is read from, and that the store under test reads.</summary>
    protected FakeTimeProvider Clock { get; } = new(Start);

    /// <summary>The store under test, created by <see cref="CreateStoreAsync"/>.</summary>
    protected IBreakTheGlassStore Store { get; private set; } = null!;

    /// <summary>Creates the store under test over an empty table, reading <paramref name="timeProvider"/>.</summary>
    protected abstract Task<IBreakTheGlassStore> CreateStoreAsync(TimeProvider timeProvider);

    /// <summary>Creates a second store over the same data (for the concurrency facts).</summary>
    protected abstract IBreakTheGlassStore CreateSecondStore();

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => Store = await CreateStoreAsync(Clock);

    /// <summary>
    /// Registers a connection (or any disposable) created for a store of this test, so that
    /// <see cref="DisposeAsync"/> releases it.
    /// </summary>
    protected T Track<T>(T disposable)
        where T : IDisposable
    {
        _tracked.Add(disposable);
        return disposable;
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        foreach (var disposable in _tracked)
        {
            disposable.Dispose();
        }

        _tracked.Clear();
        return ValueTask.CompletedTask;
    }

    // ---------------------------------------------------------------- add, get

    [Fact]
    public async Task AddThenGet_RoundTripsEveryField()
    {
        var elevation = New(justification: new string('j', BreakTheGlassLimits.JustificationMaxLength - 4) + "ñ€😀");

        Ok(await Store.AddAsync(elevation));

        var stored = await GetRequiredAsync(Tenant, elevation.ElevationId);
        stored.ElevationId.ShouldBe(elevation.ElevationId);
        stored.TenantId.ShouldBe(Tenant);
        stored.UserId.ShouldBe(User);
        stored.SubjectId.ShouldBe(Subject);
        stored.Scope.ShouldBe(Scope);
        stored.Justification.ShouldBe(elevation.Justification);
        stored.OpenedAtUtc.ShouldBe(elevation.OpenedAtUtc);
        stored.ExpiresAtUtc.ShouldBe(elevation.ExpiresAtUtc);
        stored.Status.ShouldBe(BreakTheGlassStatus.Active);
        stored.ClosedAtUtc.ShouldBeNull();
        stored.CloseReason.ShouldBeNull();
        stored.ExpiryRecordedAtUtc.ShouldBeNull();
        stored.ReviewOutcome.ShouldBeNull();
        stored.ReviewedByUserId.ShouldBeNull();
        stored.ReviewedAtUtc.ShouldBeNull();
        stored.ReviewNote.ShouldBeNull();
    }

    [Fact]
    public async Task AddThenGet_AtEveryColumnLimit_RoundTrips()
    {
        var elevation = New(
            tenantId: new string('t', BreakTheGlassLimits.TenantIdMaxLength),
            userId: new string('u', BreakTheGlassLimits.UserIdMaxLength),
            subjectId: new string('s', BreakTheGlassLimits.SubjectIdMaxLength),
            scope: new string('c', BreakTheGlassLimits.ScopeMaxLength));
        Ok(await Store.AddAsync(elevation));

        Ok(await Store.TryCloseAsync(elevation.TenantId, elevation.ElevationId, BreakTheGlassStatus.Revoked, new string('r', BreakTheGlassLimits.CloseReasonMaxLength), Now()));
        Ok(await Store.TryRecordReviewAsync(
            elevation.TenantId, elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, new string('v', BreakTheGlassLimits.ReviewedByUserIdMaxLength), new string('n', BreakTheGlassLimits.ReviewNoteMaxLength), Now()));

        var stored = await GetRequiredAsync(elevation.TenantId, elevation.ElevationId);
        stored.TenantId.ShouldBe(elevation.TenantId);
        stored.UserId.ShouldBe(elevation.UserId);
        stored.SubjectId.ShouldBe(elevation.SubjectId);
        stored.Scope.ShouldBe(elevation.Scope);
        stored.CloseReason!.Length.ShouldBe(BreakTheGlassLimits.CloseReasonMaxLength);
        stored.ReviewedByUserId!.Length.ShouldBe(BreakTheGlassLimits.ReviewedByUserIdMaxLength);
        stored.ReviewNote!.Length.ShouldBe(BreakTheGlassLimits.ReviewNoteMaxLength);
    }

    [Fact]
    public async Task Get_NullTenant_RoundTripsAsNull()
    {
        var elevation = New(tenantId: null);
        Ok(await Store.AddAsync(elevation));

        (await GetRequiredAsync(null, elevation.ElevationId)).TenantId.ShouldBeNull();
    }

    [Fact]
    public async Task Get_MissingIdOrOtherTenant_IsNone_AndNullMatchesOnlyNull()
    {
        var tenanted = New();
        var untenanted = New(subjectId: "patient-2", tenantId: null);
        Ok(await Store.AddAsync(tenanted));
        Ok(await Store.AddAsync(untenanted));

        Ok(await Store.GetByIdAsync(Tenant, Guid.NewGuid())).IsNone.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(OtherTenant, tenanted.ElevationId)).IsNone.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(null, tenanted.ElevationId)).IsNone.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(Tenant, untenanted.ElevationId)).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_ABlankOrWhitespaceTenantIsTheSameTenantAsNull()
    {
        // Every provider normalises a blank tenant to null, so the active key COALESCE(TenantId,'') is unambiguous.
        var blank = New(tenantId: string.Empty);
        Ok(await Store.AddAsync(blank));

        Ok(await Store.GetByIdAsync(string.Empty, blank.ElevationId)).IsSome.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(" ", blank.ElevationId)).IsSome.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(null, blank.ElevationId)).IsSome.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(null, User, Subject, Scope, Now())).IsSome.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(" ", User, Subject, Scope, Now())).IsSome.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task EveryTenantTakingMethod_TreatsABlankTenantAsNull(string blank)
    {
        var added = New(tenantId: blank);
        Ok(await Store.AddAsync(added));
        (await GetRequiredAsync(null, added.ElevationId)).TenantId.ShouldBeNull();

        Ok(await Store.QueryReviewQueueAsync(blank, new BreakTheGlassReviewQuery())).TotalCount.ShouldBe(1);
        Ok(await Store.TryCloseAsync(blank, added.ElevationId, BreakTheGlassStatus.Expired, "expired", Now())).ShouldBeTrue();
        Ok(await Store.MarkExpiryRecordedAsync(blank, added.ElevationId, Now()));
        (await GetRequiredAsync(null, added.ElevationId)).ExpiryRecordedAtUtc.ShouldNotBeNull();
        Ok(await Store.TryRecordReviewAsync(blank, added.ElevationId, BreakTheGlassReviewOutcome.Justified, "reviewer", null, Now())).ShouldBeTrue();
    }

    [Fact]
    public async Task Add_ABlankTenantAfterANullTenantForTheSameKey_IsAlreadyActive()
    {
        Ok(await Store.AddAsync(New(tenantId: null)));

        var result = await Store.AddAsync(New(tenantId: string.Empty));

        CodeOf(result).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
    }

    // ---------------------------------------------------------------- atomic conditional add

    [Fact]
    public async Task Add_WhileAnEffectiveElevationExists_IsAlreadyActive_AndWritesNothing()
    {
        var first = New();
        Ok(await Store.AddAsync(first));
        Clock.Advance(TimeSpan.FromMinutes(5));
        var duplicate = New();

        var result = await Store.AddAsync(duplicate);

        CodeOf(result).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
        Ok(await Store.GetByIdAsync(Tenant, duplicate.ElevationId)).IsNone.ShouldBeTrue();
        (await GetRequiredAsync(Tenant, first.ElevationId)).Status.ShouldBe(BreakTheGlassStatus.Active);
    }

    [Fact]
    public async Task Add_ConcurrentSameKey_AdmitsExactlyOne()
    {
        var second = CreateSecondStore();
        var attempts = Enumerable.Range(0, 16).Select(_ => New()).ToList();

        var results = await Task.WhenAll(attempts.Select((elevation, index) =>
            Task.Run(async () => (await (index % 2 == 0 ? Store : second).AddAsync(elevation)).IsRight)));

        results.Count(added => added).ShouldBe(1);
        var effective = Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, Scope, Now()));
        effective.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task Add_ConcurrentSameKeyWithNullTenant_AdmitsExactlyOne()
    {
        var second = CreateSecondStore();
        var attempts = Enumerable.Range(0, 16).Select(_ => New(tenantId: null)).ToList();

        var results = await Task.WhenAll(attempts.Select((elevation, index) =>
            Task.Run(async () => (await (index % 2 == 0 ? Store : second).AddAsync(elevation)).IsRight)));

        results.Count(added => added).ShouldBe(1);
    }

    [Fact]
    public async Task Add_ConcurrentDifferentKeys_AdmitsAll()
    {
        var second = CreateSecondStore();
        var attempts = Enumerable.Range(0, 8).Select(i => New(subjectId: $"patient-{i}")).ToList();

        var results = await Task.WhenAll(attempts.Select((elevation, index) =>
            Task.Run(async () => (await (index % 2 == 0 ? Store : second).AddAsync(elevation)).IsRight)));

        results.ShouldAllBe(added => added);
    }

    [Fact]
    public async Task Add_AfterTheActiveRowExpired_ClosesItAsExpiredAndInserts_KeepingExpiryUnrecorded()
    {
        var stale = New(duration: TimeSpan.FromMinutes(10));
        Ok(await Store.AddAsync(stale));
        Clock.Advance(TimeSpan.FromMinutes(30));
        var fresh = New();

        Ok(await Store.AddAsync(fresh));

        var closed = await GetRequiredAsync(Tenant, stale.ElevationId);
        closed.Status.ShouldBe(BreakTheGlassStatus.Expired);
        closed.CloseReason.ShouldBe("expired");
        closed.ClosedAtUtc.ShouldBe(fresh.OpenedAtUtc);
        closed.ExpiryRecordedAtUtc.ShouldBeNull();
        (await GetRequiredAsync(Tenant, fresh.ElevationId)).Status.ShouldBe(BreakTheGlassStatus.Active);

        // The sweeper must still audit the stale row's expiry.
        var due = Ok(await Store.GetExpiredUnrecordedAsync(Now(), 10));
        due.Select(row => row.ElevationId).ShouldContain(stale.ElevationId);
        Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, Scope, Now())).Match(Some: row => row.ElevationId, None: () => Guid.Empty).ShouldBe(fresh.ElevationId);
    }

    [Fact]
    public async Task Add_AfterTheActiveRowWasClosed_IsAccepted()
    {
        var first = New();
        Ok(await Store.AddAsync(first));
        Ok(await Store.TryCloseAsync(Tenant, first.ElevationId, BreakTheGlassStatus.Revoked, "revoked", Now()));

        Ok(await Store.AddAsync(New()));
    }

    [Fact]
    public async Task Add_ForAnotherUserSubjectScopeOrTenant_IsAccepted()
    {
        Ok(await Store.AddAsync(New()));

        Ok(await Store.AddAsync(New(userId: "clin-2")));
        Ok(await Store.AddAsync(New(subjectId: "patient-2")));
        Ok(await Store.AddAsync(New(scope: "billing")));
        Ok(await Store.AddAsync(New(tenantId: OtherTenant)));
        Ok(await Store.AddAsync(New(tenantId: null)));
    }

    [Fact]
    public async Task Add_KeyPartsAreCaseSensitive()
    {
        Ok(await Store.AddAsync(New(scope: "Clinical-Record")));

        Ok(await Store.AddAsync(New(scope: "clinical-record")));
        Ok(await Store.AddAsync(New(userId: "CLIN-1")));
        Ok(await Store.AddAsync(New(subjectId: "PATIENT-1")));
    }

    [Fact]
    public async Task Add_KeyPartsDoNotBleedIntoEachOther()
    {
        // ("a", "bc") and ("ab", "c") concatenate to the same text; the key must keep the parts apart.
        Ok(await Store.AddAsync(New(tenantId: "a", userId: "bc")));

        Ok(await Store.AddAsync(New(tenantId: "ab", userId: "c")));

        // The same holds between the user and the subject, and between the subject and the scope.
        Ok(await Store.AddAsync(New(userId: "a", subjectId: "bc")));
        Ok(await Store.AddAsync(New(userId: "ab", subjectId: "c")));
        Ok(await Store.AddAsync(New(subjectId: "a", scope: "bc")));
        Ok(await Store.AddAsync(New(subjectId: "ab", scope: "c")));
    }

    // ---------------------------------------------------------------- find effective

    [Fact]
    public async Task FindEffective_MatchesOnlyExactTenantUserSubjectScopeAndTime()
    {
        Ok(await Store.AddAsync(New(duration: TimeSpan.FromHours(1))));
        var inside = Now().AddMinutes(1);

        Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, Scope, inside)).IsSome.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(OtherTenant, User, Subject, Scope, inside)).IsNone.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(null, User, Subject, Scope, inside)).IsNone.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(Tenant, "clin-2", Subject, Scope, inside)).IsNone.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(Tenant, User, "patient-2", Scope, inside)).IsNone.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, "other", inside)).IsNone.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, "CLINICAL-RECORD", inside)).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task FindEffective_ExpiryInstantIsExclusive_AndFollowsTheClock()
    {
        var elevation = New(duration: TimeSpan.FromMinutes(10));
        Ok(await Store.AddAsync(elevation));

        Clock.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));
        Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, Scope, Now())).IsSome.ShouldBeTrue();

        Clock.Advance(TimeSpan.FromSeconds(1));
        Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, Scope, Now())).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task FindEffective_ClosedElevation_IsNone_AndNullTenantMatchesOnlyNull()
    {
        var closed = New();
        var untenanted = New(subjectId: "patient-2", tenantId: null);
        Ok(await Store.AddAsync(closed));
        Ok(await Store.AddAsync(untenanted));
        Ok(await Store.TryCloseAsync(Tenant, closed.ElevationId, BreakTheGlassStatus.Revoked, "revoked", Now()));

        Ok(await Store.FindEffectiveAsync(Tenant, User, Subject, Scope, Now())).IsNone.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(null, User, "patient-2", Scope, Now())).IsSome.ShouldBeTrue();
        Ok(await Store.FindEffectiveAsync(Tenant, User, "patient-2", Scope, Now())).IsNone.ShouldBeTrue();
    }

    // ---------------------------------------------------------------- close

    [Fact]
    public async Task TryClose_AppliesOnce_AndOnlyFromActive()
    {
        var elevation = New();
        Ok(await Store.AddAsync(elevation));
        var firstAt = Now().AddMinutes(5);

        Ok(await Store.TryCloseAsync(Tenant, elevation.ElevationId, BreakTheGlassStatus.Revoked, "first", firstAt)).ShouldBeTrue();
        Ok(await Store.TryCloseAsync(Tenant, elevation.ElevationId, BreakTheGlassStatus.Expired, "second", firstAt.AddMinutes(1))).ShouldBeFalse();

        var stored = await GetRequiredAsync(Tenant, elevation.ElevationId);
        stored.Status.ShouldBe(BreakTheGlassStatus.Revoked);
        stored.CloseReason.ShouldBe("first");
        stored.ClosedAtUtc.ShouldBe(firstAt);
    }

    [Fact]
    public async Task TryClose_MissingOrOtherTenant_ReturnsFalse_AndChangesNothing()
    {
        var elevation = New();
        Ok(await Store.AddAsync(elevation));

        Ok(await Store.TryCloseAsync(Tenant, Guid.NewGuid(), BreakTheGlassStatus.Revoked, "r", Now())).ShouldBeFalse();
        Ok(await Store.TryCloseAsync(OtherTenant, elevation.ElevationId, BreakTheGlassStatus.Revoked, "r", Now())).ShouldBeFalse();
        Ok(await Store.TryCloseAsync(null, elevation.ElevationId, BreakTheGlassStatus.Revoked, "r", Now())).ShouldBeFalse();

        (await GetRequiredAsync(Tenant, elevation.ElevationId)).Status.ShouldBe(BreakTheGlassStatus.Active);
    }

    [Fact]
    public async Task TryClose_ConcurrentCallers_ExactlyOneWins()
    {
        var second = CreateSecondStore();
        var elevation = New();
        Ok(await Store.AddAsync(elevation));

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(i =>
            Task.Run(async () => Ok(await (i % 2 == 0 ? Store : second).TryCloseAsync(Tenant, elevation.ElevationId, BreakTheGlassStatus.Expired, $"r{i}", Now())))));

        results.Count(applied => applied).ShouldBe(1);
    }

    // ---------------------------------------------------------------- sweeper

    [Fact]
    public async Task GetExpiredUnrecorded_ReturnsActivePastExpiryAndExpiredWithoutRecord()
    {
        var pastActive = New(subjectId: "p1", duration: TimeSpan.FromMinutes(10));
        var futureActive = New(subjectId: "p2", duration: TimeSpan.FromHours(5));
        var closedUnrecorded = New(subjectId: "p3", duration: TimeSpan.FromMinutes(10));
        var closedRecorded = New(subjectId: "p4", duration: TimeSpan.FromMinutes(10));
        var revoked = New(subjectId: "p5", duration: TimeSpan.FromMinutes(10));
        foreach (var elevation in new[] { pastActive, futureActive, closedUnrecorded, closedRecorded, revoked })
        {
            Ok(await Store.AddAsync(elevation));
        }

        Ok(await Store.TryCloseAsync(Tenant, closedUnrecorded.ElevationId, BreakTheGlassStatus.Expired, "expired", Now()));
        Ok(await Store.TryCloseAsync(Tenant, closedRecorded.ElevationId, BreakTheGlassStatus.Expired, "expired", Now()));
        Ok(await Store.MarkExpiryRecordedAsync(Tenant, closedRecorded.ElevationId, Now()));
        Ok(await Store.TryCloseAsync(Tenant, revoked.ElevationId, BreakTheGlassStatus.Revoked, "revoked", Now()));
        Clock.Advance(TimeSpan.FromMinutes(30));

        var due = Ok(await Store.GetExpiredUnrecordedAsync(Now(), 10));

        due.Select(row => row.ElevationId).ToList().ShouldBe([pastActive.ElevationId, closedUnrecorded.ElevationId], ignoreOrder: true);
    }

    [Fact]
    public async Task GetExpiredUnrecorded_SpansTenants_OrdersByExpiry_AndHonoursBatchSize()
    {
        var third = New(tenantId: null, subjectId: "c", duration: TimeSpan.FromMinutes(3));
        var first = New(tenantId: "t1", subjectId: "a", duration: TimeSpan.FromMinutes(1));
        var second = New(tenantId: "t2", subjectId: "b", duration: TimeSpan.FromMinutes(2));
        foreach (var elevation in new[] { third, first, second })
        {
            Ok(await Store.AddAsync(elevation));
        }

        Clock.Advance(TimeSpan.FromHours(1));

        var all = Ok(await Store.GetExpiredUnrecordedAsync(Now(), 10));
        all.Select(row => row.ElevationId).ToList().ShouldBe([first.ElevationId, second.ElevationId, third.ElevationId]);
        Ok(await Store.GetExpiredUnrecordedAsync(Now(), 2)).Select(row => row.ElevationId).ToList().ShouldBe([first.ElevationId, second.ElevationId]);
    }

    [Fact]
    public async Task GetExpiredUnrecorded_ExpiryInstantIsInclusive()
    {
        var elevation = New(duration: TimeSpan.FromMinutes(10));
        Ok(await Store.AddAsync(elevation));

        Ok(await Store.GetExpiredUnrecordedAsync(elevation.ExpiresAtUtc.AddSeconds(-1), 10)).ShouldBeEmpty();
        Ok(await Store.GetExpiredUnrecordedAsync(elevation.ExpiresAtUtc, 10)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task MarkExpiryRecorded_OnlyAffectsExpiredRows()
    {
        var active = New(subjectId: "a");
        var expired = New(subjectId: "b");
        Ok(await Store.AddAsync(active));
        Ok(await Store.AddAsync(expired));
        Ok(await Store.TryCloseAsync(Tenant, expired.ElevationId, BreakTheGlassStatus.Expired, "expired", Now()));
        var recordedAt = Now().AddMinutes(2);

        Ok(await Store.MarkExpiryRecordedAsync(Tenant, active.ElevationId, recordedAt));
        Ok(await Store.MarkExpiryRecordedAsync(Tenant, expired.ElevationId, recordedAt));
        Ok(await Store.MarkExpiryRecordedAsync(OtherTenant, expired.ElevationId, recordedAt.AddMinutes(1)));
        Ok(await Store.MarkExpiryRecordedAsync(Tenant, Guid.NewGuid(), recordedAt));

        (await GetRequiredAsync(Tenant, active.ElevationId)).ExpiryRecordedAtUtc.ShouldBeNull();
        (await GetRequiredAsync(Tenant, expired.ElevationId)).ExpiryRecordedAtUtc.ShouldBe(recordedAt);
    }

    // ---------------------------------------------------------------- review

    [Fact]
    public async Task TryRecordReview_AppliesOnce_AndOnlyWhenUnreviewed()
    {
        var elevation = New();
        Ok(await Store.AddAsync(elevation));
        var reviewedAt = Now().AddMinutes(3);

        Ok(await Store.TryRecordReviewAsync(Tenant, elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev-1", "ok", reviewedAt)).ShouldBeTrue();
        Ok(await Store.TryRecordReviewAsync(Tenant, elevation.ElevationId, BreakTheGlassReviewOutcome.Unjustified, "rev-2", null, reviewedAt.AddMinutes(1))).ShouldBeFalse();

        var stored = await GetRequiredAsync(Tenant, elevation.ElevationId);
        stored.ReviewOutcome.ShouldBe(BreakTheGlassReviewOutcome.Justified);
        stored.ReviewedByUserId.ShouldBe("rev-1");
        stored.ReviewNote.ShouldBe("ok");
        stored.ReviewedAtUtc.ShouldBe(reviewedAt);
    }

    [Fact]
    public async Task TryRecordReview_NullNote_StaysNull()
    {
        var elevation = New();
        Ok(await Store.AddAsync(elevation));

        Ok(await Store.TryRecordReviewAsync(Tenant, elevation.ElevationId, BreakTheGlassReviewOutcome.Unjustified, "rev-1", null, Now())).ShouldBeTrue();

        (await GetRequiredAsync(Tenant, elevation.ElevationId)).ReviewNote.ShouldBeNull();
    }

    [Fact]
    public async Task TryRecordReview_MissingRowOrOtherTenant_ReturnsFalse()
    {
        var elevation = New();
        Ok(await Store.AddAsync(elevation));

        Ok(await Store.TryRecordReviewAsync(Tenant, Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, "rev", null, Now())).ShouldBeFalse();
        Ok(await Store.TryRecordReviewAsync(OtherTenant, elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now())).ShouldBeFalse();
        (await GetRequiredAsync(Tenant, elevation.ElevationId)).ReviewOutcome.ShouldBeNull();
    }

    [Fact]
    public async Task TryRecordReview_ConcurrentCallers_ExactlyOneWins()
    {
        var second = CreateSecondStore();
        var elevation = New();
        Ok(await Store.AddAsync(elevation));

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(i =>
            Task.Run(async () => Ok(await (i % 2 == 0 ? Store : second).TryRecordReviewAsync(
                Tenant, elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, $"rev-{i}", null, Now())))));

        results.Count(applied => applied).ShouldBe(1);
    }

    [Fact]
    public async Task ReviewQueue_UnreviewedOnly_OldestFirst_TenantScoped()
    {
        var newest = New(subjectId: "n", at: Now().AddMinutes(30));
        var oldest = New(subjectId: "o", at: Now());
        var middle = New(subjectId: "m", at: Now().AddMinutes(10));
        var reviewed = New(subjectId: "r", at: Now().AddMinutes(5));
        foreach (var elevation in new[] { newest, oldest, middle, reviewed, New(tenantId: OtherTenant, subjectId: "x"), New(tenantId: null, subjectId: "y") })
        {
            Ok(await Store.AddAsync(elevation));
        }

        Ok(await Store.TryRecordReviewAsync(Tenant, reviewed.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now()));

        var page = Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery()));

        page.Items.Select(row => row.ElevationId).ToList().ShouldBe([oldest.ElevationId, middle.ElevationId, newest.ElevationId]);
        page.TotalCount.ShouldBe(3);
        Ok(await Store.QueryReviewQueueAsync(null, new BreakTheGlassReviewQuery())).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReviewQueue_IncludesClosedUnreviewed_AndFiltersByStatusScopeAndDates()
    {
        var active = New(subjectId: "a", at: Now());
        var closed = New(subjectId: "b", at: Now().AddHours(1));
        var otherScope = New(subjectId: "c", scope: "billing", at: Now().AddHours(2));
        foreach (var elevation in new[] { active, closed, otherScope })
        {
            Ok(await Store.AddAsync(elevation));
        }

        Ok(await Store.TryCloseAsync(Tenant, closed.ElevationId, BreakTheGlassStatus.Revoked, "revoked", Now()));

        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery())).TotalCount.ShouldBe(3);
        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { Status = BreakTheGlassStatus.Revoked })).Items.Single().ElevationId.ShouldBe(closed.ElevationId);
        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { Scope = "billing" })).Items.Single().ElevationId.ShouldBe(otherScope.ElevationId);
        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { Scope = "unknown" })).IsEmpty.ShouldBeTrue();
        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { OpenedFromUtc = Now().AddMinutes(30), OpenedToUtc = Now().AddMinutes(90) })).Items.Single().ElevationId.ShouldBe(closed.ElevationId);
        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { OpenedToUtc = Now().AddHours(1) })).Items.Single().ElevationId.ShouldBe(active.ElevationId);
    }

    [Fact]
    public async Task ReviewQueue_Pages_WithATotalOrderThatSurvivesEqualOpenedInstants()
    {
        var sameInstant = Now();
        var rows = Enumerable.Range(0, 5).Select(i => New(subjectId: $"s{i}", at: sameInstant)).ToList();
        rows.Add(New(subjectId: "later", at: sameInstant.AddMinutes(1)));
        foreach (var row in rows)
        {
            Ok(await Store.AddAsync(row));
        }

        var whole = Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { PageSize = 100 }));
        var paged = new List<Guid>();
        for (var number = 1; number <= 3; number++)
        {
            var page = Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { PageNumber = number, PageSize = 2 }));
            page.TotalCount.ShouldBe(6);
            page.TotalPages.ShouldBe(3);
            page.Items.Count.ShouldBe(2);
            paged.AddRange(page.Items.Select(row => row.ElevationId));
        }

        paged.ShouldBe(whole.Items.Select(row => row.ElevationId).ToList());
        paged.Distinct().Count().ShouldBe(6);
        paged[^1].ShouldBe(rows[^1].ElevationId);
        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { PageNumber = 4, PageSize = 2 })).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReviewQueue_ClampsPageNumberAndSize()
    {
        Ok(await Store.AddAsync(New()));

        var low = Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { PageNumber = 0, PageSize = 0 }));
        low.PageNumber.ShouldBe(1);
        low.PageSize.ShouldBe(1);
        Ok(await Store.QueryReviewQueueAsync(Tenant, new BreakTheGlassReviewQuery { PageSize = 9999 })).PageSize.ShouldBe(BreakTheGlassReviewQuery.MaxPageSize);
    }

    // ---------------------------------------------------------------- purge, probe

    [Fact]
    public async Task Purge_RemovesOnlyClosedReviewedAndExpiryRecordedRowsOlderThanTheCutoff_AcrossTenants()
    {
        var closedReviewed = New(subjectId: "a");
        var closedReviewedOtherTenant = New(tenantId: OtherTenant, subjectId: "a2");
        var closedUnreviewed = New(subjectId: "b");
        var activeReviewed = New(subjectId: "c");
        var closedReviewedRecent = New(subjectId: "d");
        var closedReviewedAtCutoff = New(subjectId: "e");
        foreach (var elevation in new[] { closedReviewed, closedReviewedOtherTenant, closedUnreviewed, activeReviewed, closedReviewedRecent, closedReviewedAtCutoff })
        {
            Ok(await Store.AddAsync(elevation));
        }

        var cutoff = Now().AddDays(1);
        await CloseAndReviewAsync(closedReviewed, reviewedAt: Now());
        await CloseAndReviewAsync(closedReviewedOtherTenant, reviewedAt: Now());
        await CloseAndReviewAsync(closedReviewedRecent, reviewedAt: Now().AddDays(10));
        await CloseAndReviewAsync(closedReviewedAtCutoff, reviewedAt: cutoff);
        Ok(await Store.TryCloseAsync(Tenant, closedUnreviewed.ElevationId, BreakTheGlassStatus.Expired, "expired", Now()));
        Ok(await Store.MarkExpiryRecordedAsync(Tenant, closedUnreviewed.ElevationId, Now()));
        Ok(await Store.TryRecordReviewAsync(Tenant, activeReviewed.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now()));

        Ok(await Store.PurgeAsync(cutoff)).ShouldBe(2);

        Ok(await Store.GetByIdAsync(Tenant, closedReviewed.ElevationId)).IsNone.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(OtherTenant, closedReviewedOtherTenant.ElevationId)).IsNone.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(Tenant, closedUnreviewed.ElevationId)).IsSome.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(Tenant, activeReviewed.ElevationId)).IsSome.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(Tenant, closedReviewedRecent.ElevationId)).IsSome.ShouldBeTrue();
        Ok(await Store.GetByIdAsync(Tenant, closedReviewedAtCutoff.ElevationId)).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task Purge_KeepsAnExpiredRowWhoseExpiryWasNeverRecorded_UntilItIs()
    {
        var expired = New();
        Ok(await Store.AddAsync(expired));
        Ok(await Store.TryCloseAsync(Tenant, expired.ElevationId, BreakTheGlassStatus.Expired, "expired", Now()));
        Ok(await Store.TryRecordReviewAsync(Tenant, expired.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now()));
        var cutoff = Now().AddDays(1);

        Ok(await Store.PurgeAsync(cutoff)).ShouldBe(0);
        Ok(await Store.MarkExpiryRecordedAsync(Tenant, expired.ElevationId, Now()));
        Ok(await Store.PurgeAsync(cutoff)).ShouldBe(1);
    }

    [Fact]
    public async Task Probe_ReturnsRight()
    {
        (await Store.ProbeAsync()).IsRight.ShouldBeTrue();
    }

    // ---------------------------------------------------------------- helpers

    private DateTimeOffset Now() => Clock.GetUtcNow();

    private BreakTheGlassElevation New(
        string? tenantId = Tenant,
        string userId = User,
        string subjectId = Subject,
        string scope = Scope,
        string justification = Justification,
        TimeSpan? duration = null,
        DateTimeOffset? at = null)
    {
        var opened = at ?? Now();
        return new BreakTheGlassElevation
        {
            ElevationId = Guid.CreateVersion7(opened),
            TenantId = tenantId,
            UserId = userId,
            SubjectId = subjectId,
            Scope = scope,
            Justification = justification,
            OpenedAtUtc = opened,
            ExpiresAtUtc = opened + (duration ?? TimeSpan.FromHours(1))
        };
    }

    private async Task CloseAndReviewAsync(BreakTheGlassElevation elevation, DateTimeOffset reviewedAt)
    {
        Ok(await Store.TryCloseAsync(elevation.TenantId, elevation.ElevationId, BreakTheGlassStatus.Revoked, "revoked", Now()));
        Ok(await Store.TryRecordReviewAsync(elevation.TenantId, elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, reviewedAt));
    }

    private async Task<BreakTheGlassElevation> GetRequiredAsync(string? tenantId, Guid elevationId) =>
        Ok(await Store.GetByIdAsync(tenantId, elevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)
        ?? throw new InvalidOperationException("The elevation was not found.");

    private static T Ok<T>(Either<EncinaError, T> result) =>
        result.Match(Right: value => value, Left: error => throw new InvalidOperationException($"The store returned Left ({error.GetCode().IfNone("no code")})."));

    private static string CodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => string.Empty, Left: error => error.GetCode().IfNone(string.Empty));
}
