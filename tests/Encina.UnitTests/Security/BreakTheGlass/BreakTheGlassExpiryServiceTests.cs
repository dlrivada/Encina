#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>
/// The expiry sweeper (D29, D30): rows Active past expiry and Expired-unrecorded are audited and marked
/// once, a per-row audit failure continues, a store failure stops, and a failed cycle is never completed.
/// Driven through <c>SweepOnceAsync</c> with a <see cref="FakeTimeProvider"/>; no sleeping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassExpiryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 8, 0, 0, TimeSpan.Zero);

    private sealed class Rig : IDisposable
    {
        private readonly ServiceProvider _provider;

        public Rig(IBreakTheGlassStore? store = null, IOperationAuditStore? audit = null)
        {
            InMemory = store as InMemoryBreakTheGlassStore ?? new InMemoryBreakTheGlassStore();
            InMemoryAudit = audit as InMemoryOperationAuditStore ?? new InMemoryOperationAuditStore();
            Store = store ?? InMemory;
            Audit = audit ?? InMemoryAudit;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<TimeProvider>(Time);
            services.AddSingleton(Store);
            services.AddSingleton(Audit);
            services.AddEncinaBreakTheGlass(options =>
            {
                options.DeclareScope(BreakTheGlassTestKit.Scope, scope => scope.OpenerRoles.Add("clinician"));
                options.ReviewerRoles.Add("reviewer");
                options.ExpirySweepBatchSize = 10;
            });
            _provider = services.BuildServiceProvider();
            State = _provider.GetRequiredService<BreakTheGlassSweepState>();
        }

        public FakeTimeProvider Time { get; } = new(Now);

        public InMemoryBreakTheGlassStore InMemory { get; }

        public InMemoryOperationAuditStore InMemoryAudit { get; }

        public IBreakTheGlassStore Store { get; }

        public IOperationAuditStore Audit { get; }

        public BreakTheGlassSweepState State { get; }

        public FakeLogger<BreakTheGlassExpiryService> Logger { get; } = new();

        public BreakTheGlassExpiryService Sweeper() => new(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _provider.GetRequiredService<IInternalRequestContextScopeFactory>(),
            _provider.GetRequiredService<IOptions<BreakTheGlassOptions>>(),
            State,
            Time,
            Logger);

        public async Task<BreakTheGlassElevation> OpenAsync(string subject = "patient-1", TimeSpan? duration = null)
        {
            var elevation = BreakTheGlassTestKit.Elevation(Time.GetUtcNow(), subjectId: subject, duration: duration ?? TimeSpan.FromHours(1));
            (await InMemory.AddAsync(elevation)).IsRight.ShouldBeTrue();
            return elevation;
        }

        public async Task<BreakTheGlassElevation> RowAsync(BreakTheGlassElevation elevation) =>
            (await InMemory.GetByIdAsync(elevation.TenantId, elevation.ElevationId)).Match(Right: o => o.IfNoneUnsafe((BreakTheGlassElevation?)null)!, Left: _ => null!);

        public void Dispose() => _provider.Dispose();
    }

    [Fact]
    public async Task ActiveRowPastExpiry_IsClosedAuditedAndMarked_AndTheCycleCompletes()
    {
        using var rig = new Rig();
        var elevation = await rig.OpenAsync();
        rig.Time.Advance(TimeSpan.FromHours(2));

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();

        var row = await rig.RowAsync(elevation);
        row.Status.ShouldBe(BreakTheGlassStatus.Expired);
        row.ExpiryRecordedAtUtc.ShouldNotBeNull();
        var entry = rig.InMemoryAudit.GetAllEntries().ShouldHaveSingleItem();
        entry.Action.ShouldBe("break_glass.expired");
        entry.EntityId.ShouldBe(elevation.ElevationId.ToString("D"));
        entry.TenantId.ShouldBe(elevation.TenantId);
        entry.UserId.ShouldBe("service:" + BreakTheGlassExpiryService.ServiceIdentityName);
        entry.Outcome.ShouldBe(AuditOutcome.Success);
        entry.Metadata!.Values.Select(v => v?.ToString()).ShouldNotContain("patient-1");
        rig.State.LastCompletedAtUtc.ShouldBe(rig.Time.GetUtcNow());
    }

    [Fact]
    public async Task ElevationNotYetExpired_IsLeftAlone()
    {
        using var rig = new Rig();
        var elevation = await rig.OpenAsync();
        rig.Time.Advance(TimeSpan.FromMinutes(30));

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();

        (await rig.RowAsync(elevation)).Status.ShouldBe(BreakTheGlassStatus.Active);
        rig.InMemoryAudit.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExpiredUnrecordedRow_GoesStraightToAuditAndMark()
    {
        using var rig = new Rig();
        var elevation = await rig.OpenAsync();
        rig.Time.Advance(TimeSpan.FromHours(2));
        (await rig.InMemory.TryCloseAsync(elevation.TenantId, elevation.ElevationId, BreakTheGlassStatus.Expired, "expired", rig.Time.GetUtcNow())).IsRight.ShouldBeTrue();

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();

        (await rig.RowAsync(elevation)).ExpiryRecordedAtUtc.ShouldNotBeNull();
        rig.InMemoryAudit.GetAllEntries().ShouldHaveSingleItem().Action.ShouldBe("break_glass.expired");
    }

    [Fact]
    public async Task TwoSweepers_OnTheSameStore_RecordTheExpiryOnce()
    {
        using var rig = new Rig();
        await rig.OpenAsync();
        rig.Time.Advance(TimeSpan.FromHours(2));

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();
        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();

        rig.InMemoryAudit.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LosingTheCloseRace_WritesNothing()
    {
        using var rig = new Rig();
        var elevation = await rig.OpenAsync();
        rig.Time.Advance(TimeSpan.FromHours(2));
        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetExpiredUnrecordedAsync(default, default, default).ReturnsForAnyArgs(
            ValueTask.FromResult(Right<EncinaError, IReadOnlyList<BreakTheGlassElevation>>([elevation])));
        store.TryCloseAsync(default, default, default, default!, default, default).ReturnsForAnyArgs(
            ValueTask.FromResult(Right<EncinaError, bool>(false)));
        using var racing = new Rig(store);

        (await racing.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();

        racing.InMemoryAudit.Count.ShouldBe(0);
        await store.DidNotReceiveWithAnyArgs().MarkExpiryRecordedAsync(default, default, default, default);
    }

    [Fact]
    public async Task PerRowAuditFailure_ContinuesWithTheBatch_AndTheCycleIsNotCompleted()
    {
        using var seed = new Rig();
        var first = await seed.OpenAsync("patient-1");
        var second = await seed.OpenAsync("patient-2");
        seed.Time.Advance(TimeSpan.FromHours(2));

        var audit = Substitute.For<IOperationAuditStore>();
        audit.RecordAsync(default!, default).ReturnsForAnyArgs(call =>
        {
            var entry = call.Arg<OperationAuditEntry>();
            return ValueTask.FromResult(entry.EntityId == first.ElevationId.ToString("D")
                ? Left<EncinaError, Unit>(EncinaErrors.Create("audit.down", "down"))
                : Right<EncinaError, Unit>(Unit.Default));
        });
        using var rig = new Rig(seed.InMemory, audit);
        rig.Time.Advance(TimeSpan.FromHours(2));

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeFalse();

        (await rig.RowAsync(second)).ExpiryRecordedAtUtc.ShouldNotBeNull();
        var failedRow = await rig.RowAsync(first);
        failedRow.Status.ShouldBe(BreakTheGlassStatus.Expired);
        failedRow.ExpiryRecordedAtUtc.ShouldBeNull();
        rig.State.LastCompletedAtUtc.ShouldBeNull();
        rig.Logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9753);

        // The failed row is retried next cycle, straight to audit and mark.
        using var retry = new Rig(rig.InMemory);
        retry.Time.Advance(TimeSpan.FromHours(2));
        (await retry.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();
        (await retry.RowAsync(first)).ExpiryRecordedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task StoreFailureWhileListing_StopsTheCycle_AndLogsOnlyTheCode()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetExpiredUnrecordedAsync(default, default, default).ReturnsForAnyArgs(
            ValueTask.FromResult(Left<EncinaError, IReadOnlyList<BreakTheGlassElevation>>(EncinaErrors.Create("break_glass.store_failed", "secret-detail"))));
        using var rig = new Rig(store);

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeFalse();

        rig.State.LastCompletedAtUtc.ShouldBeNull();
        var entry = rig.Logger.Collector.GetSnapshot().Single(e => e.Id.Id == 9752);
        entry.Message.ShouldContain("break_glass.store_failed");
        entry.Message.ShouldNotContain("secret-detail");
    }

    [Fact]
    public async Task StoreFailureOnARow_StopsTheBatch()
    {
        using var seed = new Rig();
        var first = await seed.OpenAsync("patient-1");
        var second = await seed.OpenAsync("patient-2");
        seed.Time.Advance(TimeSpan.FromHours(2));
        var rows = (await seed.InMemory.GetExpiredUnrecordedAsync(seed.Time.GetUtcNow(), 10)).Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        rows.Count.ShouldBe(2);

        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetExpiredUnrecordedAsync(default, default, default).ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<BreakTheGlassElevation>>(rows)));
        store.TryCloseAsync(default, default, default, default!, default, default).ReturnsForAnyArgs(
            ValueTask.FromResult(Left<EncinaError, bool>(EncinaErrors.Create("break_glass.store_failed", "x"))));
        using var rig = new Rig(store);

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeFalse();

        await store.ReceivedWithAnyArgs(1).TryCloseAsync(default, default, default, default!, default, default);
        rig.InMemoryAudit.Count.ShouldBe(0);
        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
    }

    [Fact]
    public async Task StoreFailureWhileMarking_StopsTheBatch_AndTheCycleIsNotCompleted()
    {
        using var seed = new Rig();
        var elevation = await seed.OpenAsync();
        seed.Time.Advance(TimeSpan.FromHours(2));
        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetExpiredUnrecordedAsync(default, default, default).ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<BreakTheGlassElevation>>([elevation])));
        store.TryCloseAsync(default, default, default, default!, default, default).ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, bool>(true)));
        store.MarkExpiryRecordedAsync(default, default, default, default).ReturnsForAnyArgs(
            ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("break_glass.store_failed", "x"))));
        using var rig = new Rig(store);

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeFalse();

        rig.State.LastCompletedAtUtc.ShouldBeNull();
        rig.InMemoryAudit.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ExceptionFromTheStore_FailsTheCycle_WithoutCrashing()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetExpiredUnrecordedAsync(default, default, default).ReturnsForAnyArgs<ValueTask<Either<EncinaError, IReadOnlyList<BreakTheGlassElevation>>>>(_ => throw new InvalidOperationException("boom"));
        using var rig = new Rig(store);

        (await rig.Sweeper().SweepOnceAsync(CancellationToken.None)).ShouldBeFalse();

        rig.Logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9754);
    }

    [Fact]
    public async Task CancelledToken_Propagates()
    {
        using var rig = new Rig();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => rig.Sweeper().SweepOnceAsync(cts.Token));
    }

    [Fact]
    public async Task Loop_SweepsAtStart_AndAgainWhenTheIntervalElapses()
    {
        using var rig = new Rig();
        var elevation = await rig.OpenAsync();
        rig.Time.Advance(TimeSpan.FromHours(2));
        var sweeper = rig.Sweeper();

        await sweeper.StartAsync(CancellationToken.None);
        await WaitUntilAsync(async () => (await rig.RowAsync(elevation)).ExpiryRecordedAtUtc is not null);
        rig.State.StartedAtUtc.ShouldNotBeNull();

        var second = await rig.OpenAsync("patient-2", TimeSpan.FromSeconds(30));
        rig.Time.Advance(TimeSpan.FromMinutes(2));
        await WaitUntilAsync(async () => (await rig.RowAsync(second)).ExpiryRecordedAtUtc is not null);

        await sweeper.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DisabledFeature_StillSweeps()
    {
        using var rig = new Rig();
        var elevation = await rig.OpenAsync();
        rig.Time.Advance(TimeSpan.FromHours(2));
        var options = Options.Create(new BreakTheGlassOptions { Enabled = false });
        var provider = new ServiceCollection().AddSingleton(rig.Store).AddSingleton(rig.Audit).BuildServiceProvider();
        var scopes = new ServiceCollection().AddLogging().AddEncinaBreakTheGlass(o => o.Enabled = false).BuildServiceProvider();
        var sweeper = new BreakTheGlassExpiryService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            scopes.GetRequiredService<IInternalRequestContextScopeFactory>(),
            options,
            new BreakTheGlassSweepState(),
            rig.Time,
            NullLogger<BreakTheGlassExpiryService>.Instance);

        (await sweeper.SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();

        (await rig.RowAsync(elevation)).ExpiryRecordedAtUtc.ShouldNotBeNull();
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!await condition())
        {
            await Task.Yield();
            timeout.Token.ThrowIfCancellationRequested();
        }
    }
}
