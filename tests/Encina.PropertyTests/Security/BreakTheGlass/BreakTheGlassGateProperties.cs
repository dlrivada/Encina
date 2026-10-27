using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.PropertyTests.Security.BreakTheGlass;

[BreakTheGlassEligible("clinical-record", SubjectIdProperty = nameof(PatientId))]
public sealed record PropertyGateRequest(string PatientId);

/// <summary>
/// FsCheck properties of <see cref="BreakTheGlassAccessGate"/>: for any duration and any check time,
/// the gate grants only when the check time is before the expiry and the status is Active.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassGateProperties
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private static IRequestContext Context()
    {
        var context = NSubstitute.Substitute.For<IRequestContext>();
        context.Identity.Returns(TestIdentity.User("clin-1"));
        context.TenantId.Returns("tenant-1");
        context.CorrelationId.Returns("corr-1");
        return context;
    }

    private static async Task<bool> GrantsAsync(int durationSeconds, int checkOffsetSeconds, BreakTheGlassStatus status)
    {
        var store = new InMemoryBreakTheGlassStore();
        var elevation = new BreakTheGlassElevation
        {
            ElevationId = Guid.NewGuid(),
            TenantId = "tenant-1",
            UserId = "clin-1",
            SubjectId = "patient-1",
            Scope = "clinical-record",
            Justification = "justification",
            OpenedAtUtc = OpenedAt,
            ExpiresAtUtc = OpenedAt.AddSeconds(durationSeconds),
            Status = status
        };
        await store.AddAsync(elevation);

        var time = new FakeTimeProvider(OpenedAt.AddSeconds(checkOffsetSeconds));
        var gate = new BreakTheGlassAccessGate(
            store,
            new InMemoryOperationAuditStore(),
            new ReadAuditContext(),
            new BreakTheGlassRequestGrants(),
            Options.Create(new BreakTheGlassOptions()),
            time,
            NullLogger<BreakTheGlassAccessGate>.Instance);

        var result = await gate.TryElevateAsync(new PropertyGateRequest("patient-1"), Context(), EncinaErrors.Forbidden(), CancellationToken.None);
        return result.Match(Right: option => option.IsSome, Left: _ => false);
    }

    [Property(MaxTest = 200)]
    public bool Grants_OnlyWhenBeforeExpiryAndActive(PositiveInt duration, int checkOffset)
    {
        var durationSeconds = duration.Get % 86_400;
        var offset = checkOffset % 200_000;

        var granted = GrantsAsync(durationSeconds, offset, BreakTheGlassStatus.Active).GetAwaiter().GetResult();

        return granted == (offset < durationSeconds);
    }

    // The expiry sweeper never makes an expired elevation effective again, however often it runs, and it
    // records the expiry exactly once.
    private static async Task<bool> SweptElevationStaysDeadAsync(int durationSeconds, int sweeperRuns)
    {
        var time = new FakeTimeProvider(OpenedAt);
        var store = new InMemoryBreakTheGlassStore();
        var audit = new InMemoryOperationAuditStore();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton<IBreakTheGlassStore>(store);
        services.AddSingleton<IOperationAuditStore>(audit);
        services.AddLogging();
        services.AddEncinaBreakTheGlass(options =>
        {
            options.DeclareScope("clinical-record", scope => scope.OpenerRoles.Add("clinician"));
            options.ReviewerRoles.Add("reviewer");
        });
        await using var provider = services.BuildServiceProvider();

        var elevation = new BreakTheGlassElevation
        {
            ElevationId = Guid.NewGuid(),
            TenantId = "tenant-1",
            UserId = "clin-1",
            SubjectId = "patient-1",
            Scope = "clinical-record",
            Justification = "justification",
            OpenedAtUtc = OpenedAt,
            ExpiresAtUtc = OpenedAt.AddSeconds(durationSeconds)
        };
        await store.AddAsync(elevation);

        time.Advance(TimeSpan.FromSeconds(durationSeconds + 1));
        var sweeper = new BreakTheGlassExpiryService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IInternalRequestContextScopeFactory>(),
            Options.Create(new BreakTheGlassOptions()),
            new BreakTheGlassSweepState(),
            time,
            NullLogger<BreakTheGlassExpiryService>.Instance);

        for (var run = 0; run < sweeperRuns; run++)
        {
            if (!await sweeper.SweepOnceAsync(CancellationToken.None))
            {
                return false;
            }

            time.Advance(TimeSpan.FromSeconds(30));
            var effective = await store.FindEffectiveAsync("tenant-1", "clin-1", "patient-1", "clinical-record", time.GetUtcNow());
            if (effective.Match(Right: found => found.IsSome, Left: _ => true))
            {
                return false;
            }
        }

        return audit.GetAllEntries().Count(e => e.Action == "break_glass.expired") == 1;
    }

    [Property(MaxTest = 30)]
    public bool ExpiredElevation_IsNeverHonouredAgain_AndRecordedOnce_AcrossSweeperRuns(PositiveInt duration, PositiveInt runs) =>
        SweptElevationStaysDeadAsync(duration.Get % 3_600 + 1, runs.Get % 4 + 1).GetAwaiter().GetResult();

    private static BreakTheGlassAccessGate GateOver(IBreakTheGlassStore store, BreakTheGlassOptions options, FakeTimeProvider time) =>
        new(store, new InMemoryOperationAuditStore(), new ReadAuditContext(), new BreakTheGlassRequestGrants(), Options.Create(options), time, NullLogger<BreakTheGlassAccessGate>.Instance);

    private static BreakTheGlassElevation ActiveElevation(int durationSeconds) => new()
    {
        ElevationId = Guid.NewGuid(),
        TenantId = null,
        UserId = "clin-1",
        SubjectId = "patient-1",
        Scope = "clinical-record",
        Justification = "justification",
        OpenedAtUtc = OpenedAt,
        ExpiresAtUtc = OpenedAt.AddSeconds(durationSeconds)
    };

    // Fail closed: when a tenant is required, a request without one never gets an elevation, even a tenant-less one.
    [Property(MaxTest = 50)]
    public bool RequireTenant_NeverGrants_ARequestWithoutATenant(PositiveInt duration, bool blank) =>
        RequireTenantDeniesAsync(duration.Get % 86_400 + 60, blank).GetAwaiter().GetResult();

    private static async Task<bool> RequireTenantDeniesAsync(int durationSeconds, bool blank)
    {
        var store = new InMemoryBreakTheGlassStore();
        await store.AddAsync(ActiveElevation(durationSeconds));
        var gate = GateOver(store, new BreakTheGlassOptions { RequireTenant = true }, new FakeTimeProvider(OpenedAt));
        var context = Context();
        context.TenantId.Returns(blank ? "  " : null);

        var result = await gate.TryElevateAsync(new PropertyGateRequest("patient-1"), context, EncinaErrors.Forbidden(), CancellationToken.None);

        return result.Match(Right: option => option.IsNone, Left: _ => false);
    }

    // A failing store lookup never turns into a grant and never into an error: the denial stays.
    [Property(MaxTest = 50)]
    public bool FailingStore_NeverGrants_AndNeverErrors(NonEmptyString code) =>
        FailingStoreKeepsDenialAsync(code.Get).GetAwaiter().GetResult();

    private static async Task<bool> FailingStoreKeepsDenialAsync(string message)
    {
        var store = Substitute.For<IBreakTheGlassStore>();
#pragma warning disable CA2012 // NSubstitute stubbing of ValueTask members
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(LanguageExt.Prelude.Left<EncinaError, LanguageExt.Option<BreakTheGlassElevation>>(
                EncinaErrors.Create(BreakTheGlassErrors.StoreFailedCode, message))));
#pragma warning restore CA2012
        var gate = GateOver(store, new BreakTheGlassOptions(), new FakeTimeProvider(OpenedAt));

        var result = await gate.TryElevateAsync(new PropertyGateRequest("patient-1"), Context(), EncinaErrors.Forbidden(), CancellationToken.None);

        return result.Match(Right: option => option.IsNone, Left: _ => false);
    }

    // A second gate of the same request reuses the grant while it is in force and writes nothing more.
    [Property(MaxTest = 30)]
    public bool SecondGateOfTheSameRequest_ReusesTheGrant_WhileItIsInForce(PositiveInt duration) =>
        ReusesGrantAsync(duration.Get % 86_400 + 60).GetAwaiter().GetResult();

    private static async Task<bool> ReusesGrantAsync(int durationSeconds)
    {
        var store = new InMemoryBreakTheGlassStore();
        await store.AddAsync(ActiveElevation(durationSeconds) with { TenantId = "tenant-1" });
        var gate = GateOver(store, new BreakTheGlassOptions(), new FakeTimeProvider(OpenedAt));

        var first = await gate.TryElevateAsync(new PropertyGateRequest("patient-1"), Context(), EncinaErrors.Forbidden(), CancellationToken.None);
        var second = await gate.TryElevateAsync(new PropertyGateRequest("patient-1"), Context(), EncinaErrors.Forbidden(), CancellationToken.None);

        return first.Match(Right: o => o.IsSome, Left: _ => false) && second.Match(Right: o => o.IsSome, Left: _ => false);
    }

    [Property(MaxTest = 100)]
    public bool NeverGrants_ForAClosedElevation(PositiveInt duration, int checkOffset, bool revoked)
    {
        var status = revoked ? BreakTheGlassStatus.Revoked : BreakTheGlassStatus.Expired;

        return !GrantsAsync(duration.Get % 86_400, checkOffset % 200_000, status).GetAwaiter().GetResult();
    }
}
