using Encina.Security.BreakTheGlass;
using LanguageExt;
using Shouldly;

namespace Encina.ContractTests.Security.BreakTheGlass;

/// <summary>
/// Contract tests: the shipped implementations honour the contracts of <see cref="IBreakTheGlassStore"/>
/// and <see cref="IAccessElevationGate"/>, run against the interface (what any provider store must satisfy).
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private static BreakTheGlassElevation NewElevation(string? tenant = "t", string user = "u", string subject = "s", int minutes = 60, DateTimeOffset? opened = null) => new()
    {
        ElevationId = Guid.NewGuid(),
        TenantId = tenant,
        UserId = user,
        SubjectId = subject,
        Scope = "clinical-record",
        Justification = "justification",
        OpenedAtUtc = opened ?? Now,
        ExpiresAtUtc = (opened ?? Now).AddMinutes(minutes)
    };

    private static T Right<T>(Either<EncinaError, T> value) =>
        value.Match(Right: r => r, Left: e => throw new InvalidOperationException(e.GetCode().IfNone("?")));

    // The tests below use the store only through the contract.
#pragma warning disable CA1859
    private static IBreakTheGlassStore Store() => new InMemoryBreakTheGlassStore();
#pragma warning restore CA1859

    [Fact]
    public async Task Store_AddThenGet_RoundTrips()
    {
        var store = Store();
        var elevation = NewElevation();
        Right(await store.AddAsync(elevation));

        Right(await store.GetByIdAsync("t", elevation.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null).ShouldBe(elevation);
    }

    [Fact]
    public async Task Store_Add_IsConditional_RefusesASecondEffectiveElevationForTheSameKey()
    {
        var store = Store();
        var first = NewElevation();
        var second = NewElevation();
        Right(await store.AddAsync(first));

        var refused = await store.AddAsync(second);

        refused.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty)).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
        Right(await store.GetByIdAsync("t", second.ElevationId)).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Store_IsTenantIsolated()
    {
        var store = Store();
        var elevation = NewElevation(tenant: "a");
        Right(await store.AddAsync(elevation));

        Right(await store.GetByIdAsync("b", elevation.ElevationId)).IsNone.ShouldBeTrue();
        Right(await store.FindEffectiveAsync("b", "u", "s", "clinical-record", Now)).IsNone.ShouldBeTrue();
        Right(await store.QueryReviewQueueAsync("b", new BreakTheGlassReviewQuery())).IsEmpty.ShouldBeTrue();
        Right(await store.TryCloseAsync("b", elevation.ElevationId, BreakTheGlassStatus.Revoked, "r", Now)).ShouldBeFalse();
    }

    [Fact]
    public async Task Store_FindEffective_RespectsExpiryAndStatus()
    {
        var store = Store();
        var elevation = NewElevation(minutes: 10);
        Right(await store.AddAsync(elevation));

        Right(await store.FindEffectiveAsync("t", "u", "s", "clinical-record", Now.AddMinutes(9))).IsSome.ShouldBeTrue();
        Right(await store.FindEffectiveAsync("t", "u", "s", "clinical-record", Now.AddMinutes(10))).IsNone.ShouldBeTrue();
        Right(await store.TryCloseAsync("t", elevation.ElevationId, BreakTheGlassStatus.Revoked, "r", Now));
        Right(await store.FindEffectiveAsync("t", "u", "s", "clinical-record", Now.AddMinutes(1))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Store_ConditionalUpdates_ApplyOnce()
    {
        var store = Store();
        var elevation = NewElevation();
        Right(await store.AddAsync(elevation));

        Right(await store.TryCloseAsync("t", elevation.ElevationId, BreakTheGlassStatus.Expired, "e", Now)).ShouldBeTrue();
        Right(await store.TryCloseAsync("t", elevation.ElevationId, BreakTheGlassStatus.Revoked, "r", Now)).ShouldBeFalse();
        Right(await store.TryRecordReviewAsync("t", elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now)).ShouldBeTrue();
        Right(await store.TryRecordReviewAsync("t", elevation.ElevationId, BreakTheGlassReviewOutcome.Unjustified, "rev2", null, Now)).ShouldBeFalse();
    }

    [Fact]
    public async Task Store_ReviewQueue_OldestFirst_UnreviewedOnly()
    {
        var store = Store();
        var late = NewElevation(subject: "late", opened: Now.AddMinutes(5));
        var early = NewElevation(subject: "early", opened: Now);
        var reviewed = NewElevation(subject: "reviewed", opened: Now.AddMinutes(2));
        foreach (var e in new[] { late, early, reviewed })
        {
            Right(await store.AddAsync(e));
        }

        Right(await store.TryRecordReviewAsync("t", reviewed.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now));

        Right(await store.QueryReviewQueueAsync("t", new BreakTheGlassReviewQuery())).Items.Select(e => e.SubjectId).ShouldBe(["early", "late"]);
    }

    [Fact]
    public async Task Store_Purge_RemovesOnlyClosedAndReviewed()
    {
        var store = Store();
        var closedReviewed = NewElevation(subject: "a");
        var activeReviewed = NewElevation(subject: "b");
        foreach (var e in new[] { closedReviewed, activeReviewed })
        {
            Right(await store.AddAsync(e));
            Right(await store.TryRecordReviewAsync("t", e.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev", null, Now));
        }

        Right(await store.TryCloseAsync("t", closedReviewed.ElevationId, BreakTheGlassStatus.Expired, "e", Now));
        Right(await store.PurgeAsync(Now.AddDays(1))).ShouldBe(0);
        Right(await store.MarkExpiryRecordedAsync("t", closedReviewed.ElevationId, Now));

        Right(await store.PurgeAsync(Now.AddDays(1))).ShouldBe(1);
        Right(await store.GetByIdAsync("t", activeReviewed.ElevationId)).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task Store_Probe_Succeeds()
    {
        (await Store().ProbeAsync()).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_Default_NeverElevates()
    {
#pragma warning disable CA1859 // exercised through the contract on purpose
        IAccessElevationGate gate = new NoAccessElevationGate();
#pragma warning restore CA1859

        var result = await gate.TryElevateAsync("request", RequestContext.Create(), EncinaErrors.Forbidden(), CancellationToken.None);

        Right(result).IsNone.ShouldBeTrue();
    }
}
