#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Security.BreakTheGlass.Health;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.BreakTheGlass;

[BreakTheGlassEligible("start-up-scope", SubjectIdProperty = nameof(PatientId))]
public sealed record StartupGoodRequest(string PatientId);

[BreakTheGlassEligible("undeclared-scope", SubjectIdProperty = nameof(PatientId))]
public sealed record StartupUndeclaredScopeRequest(string PatientId);

[BreakTheGlassEligible("start-up-scope", SubjectIdProperty = "Nope")]
public sealed record StartupMissingPropertyRequest(string PatientId);

[BreakTheGlassEligible("start-up-scope", SubjectIdProperty = nameof(Complex))]
public sealed record StartupUnconvertibleRequest(object Complex);

/// <summary>The purge service (D32), the health check (D34) and the start-up validator (D16, D17, D22).</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassOperationsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 8, 0, 0, TimeSpan.Zero);

    private static readonly System.Reflection.Assembly[] NoAssemblies = [];

    private static BreakTheGlassOptions Options(Action<BreakTheGlassOptions>? configure = null)
    {
        var options = new BreakTheGlassOptions();
        options.DeclareScope("start-up-scope", scope => scope.OpenerRoles.Add("clinician"));
        options.ReviewerRoles.Add("reviewer");
        configure?.Invoke(options);
        return options;
    }

    // ── Purge ─────────────────────────────────────────────────────────

    private sealed class PurgeRig
    {
        public FakeTimeProvider Time { get; } = new(Now);

        public InMemoryBreakTheGlassStore Store { get; } = new();

        public FakeLogger<BreakTheGlassPurgeService> Logger { get; } = new();

        public BreakTheGlassPurgeService Service(IBreakTheGlassStore? store = null, bool enabled = true)
        {
            var services = new ServiceCollection();
            services.AddSingleton(store ?? Store);
            return new BreakTheGlassPurgeService(
                services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                Microsoft.Extensions.Options.Options.Create(Options(o =>
                {
                    o.RetentionDays = 30;
                    o.EnableAutoPurge = enabled;
                    o.PurgeIntervalHours = 24;
                })),
                Time,
                Logger);
        }
    }

    private static async Task<BreakTheGlassElevation> AddReviewedAsync(InMemoryBreakTheGlassStore store, DateTimeOffset openedAt, string subject, bool close)
    {
        var elevation = BreakTheGlassTestKit.Elevation(openedAt, subjectId: subject, duration: TimeSpan.FromHours(1));
        (await store.AddAsync(elevation)).IsRight.ShouldBeTrue();
        if (close)
        {
            await store.TryCloseAsync(elevation.TenantId, elevation.ElevationId, BreakTheGlassStatus.Expired, "expired", openedAt.AddHours(1));
            await store.MarkExpiryRecordedAsync(elevation.TenantId, elevation.ElevationId, openedAt.AddHours(1));
        }

        await store.TryRecordReviewAsync(elevation.TenantId, elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, "rev-1", null, openedAt.AddHours(2));
        return elevation;
    }

    [Fact]
    public async Task Purge_DeletesOnlyClosedReviewedRowsOlderThanTheRetention()
    {
        var rig = new PurgeRig();
        await AddReviewedAsync(rig.Store, Now.AddDays(-60), "old-closed", close: true);
        await AddReviewedAsync(rig.Store, Now.AddDays(-5), "recent-closed", close: true);
        await AddReviewedAsync(rig.Store, Now.AddDays(-60), "old-still-active", close: false);

        (await rig.Service().PurgeOnceAsync(CancellationToken.None)).ShouldBe(1);

        rig.Logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9756);
    }

    [Fact]
    public async Task Purge_StoreFailure_ReturnsNull_AndLogsOnlyTheCode()
    {
        var rig = new PurgeRig();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.PurgeAsync(default, default).ReturnsForAnyArgs(ValueTask.FromResult(Left<EncinaError, int>(EncinaErrors.Create("break_glass.store_failed", "secret"))));

        (await rig.Service(store).PurgeOnceAsync(CancellationToken.None)).ShouldBeNull();

        var entry = rig.Logger.Collector.GetSnapshot().Single(e => e.Id.Id == 9757);
        entry.Message.ShouldContain("break_glass.store_failed");
        entry.Message.ShouldNotContain("secret");
    }

    [Fact]
    public async Task Purge_Exception_ReturnsNull()
    {
        var rig = new PurgeRig();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.PurgeAsync(default, default).ReturnsForAnyArgs<ValueTask<Either<EncinaError, int>>>(_ => throw new InvalidOperationException("boom"));

        (await rig.Service(store).PurgeOnceAsync(CancellationToken.None)).ShouldBeNull();

        rig.Logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9758);
    }

    [Fact]
    public async Task Purge_Cancelled_Propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.PurgeAsync(default, default).ReturnsForAnyArgs<ValueTask<Either<EncinaError, int>>>(_ => throw new OperationCanceledException(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => new PurgeRig().Service(store).PurgeOnceAsync(cts.Token));
    }

    [Fact]
    public async Task Purge_RunsOnItsOwnDailyCadence_NotAtStart()
    {
        var rig = new PurgeRig();
        var old = await AddReviewedAsync(rig.Store, Now.AddDays(-60), "old", close: true);
        var service = rig.Service();

        async Task<bool> StillThereAsync() =>
            (await rig.Store.GetByIdAsync(old.TenantId, old.ElevationId)).Match(Right: row => row.IsSome, Left: _ => false);

        await service.StartAsync(CancellationToken.None);
        // Nothing runs at start: the purge waits for its own interval (24 hours in this rig).
        (await StillThereAsync()).ShouldBeTrue();

        await WaitUntilPurgedAsync(rig.Time, StillThereAsync);

        await service.StopAsync(CancellationToken.None);
    }

    private static async Task WaitUntilPurgedAsync(FakeTimeProvider time, Func<Task<bool>> stillThere)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (await stillThere())
        {
            time.Advance(TimeSpan.FromHours(24));
            await Task.Delay(1, timeout.Token);
            timeout.Token.ThrowIfCancellationRequested();
        }
    }

    [Fact]
    public async Task Purge_Disabled_LogsOnceAndNeverPurges()
    {
        var rig = new PurgeRig();
        var old = await AddReviewedAsync(rig.Store, Now.AddDays(-60), "old", close: true);
        var service = rig.Service(enabled: false);

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;
        rig.Time.Advance(TimeSpan.FromHours(48));
        await service.StopAsync(CancellationToken.None);

        (await rig.Store.GetByIdAsync(old.TenantId, old.ElevationId)).Match(Right: row => row.IsSome, Left: _ => false).ShouldBeTrue();
        rig.Logger.Collector.GetSnapshot().Count(e => e.Id.Id == 9764).ShouldBe(1);
        rig.Logger.Collector.GetSnapshot().ShouldNotContain(e => e.Id.Id == 9756);
    }

    [Fact]
    public async Task Purge_EnabledOnlyThroughConfigurationBinding_Runs()
    {
        // The configure delegate leaves EnableAutoPurge false; only the bound configuration turns it on.
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EnableAutoPurge"] = "true",
                ["RetentionDays"] = "30",
                ["PurgeIntervalHours"] = "24"
            })
            .Build();
        var time = new FakeTimeProvider(Now);
        var store = new InMemoryBreakTheGlassStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton<IBreakTheGlassStore>(store);
        services.AddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>();
        services.AddEncinaBreakTheGlass(o =>
        {
            o.DeclareScope("start-up-scope", scope => scope.OpenerRoles.Add("clinician"));
            o.ReviewerRoles.Add("reviewer");
            o.AllowInMemoryStore = true;
        });
        services.Configure<BreakTheGlassOptions>(configuration);
        await using var provider = services.BuildServiceProvider();
        var old = await AddReviewedAsync(store, Now.AddDays(-60), "old", close: true);

        var purge = provider.GetServices<IHostedService>().OfType<BreakTheGlassPurgeService>().Single();
        await purge.StartAsync(CancellationToken.None);

        await WaitUntilPurgedAsync(time, async () =>
            (await store.GetByIdAsync(old.TenantId, old.ElevationId)).Match(Right: row => row.IsSome, Left: _ => false));

        await purge.StopAsync(CancellationToken.None);
    }

    // ── Health check ──────────────────────────────────────────────────

    private static (BreakTheGlassHealthCheck Check, BreakTheGlassSweepState State, FakeTimeProvider Time) Health(
        IBreakTheGlassStore store, IOperationAuditStore? audit = null)
    {
        var time = new FakeTimeProvider(Now);
        var state = new BreakTheGlassSweepState();
        var services = new ServiceCollection();
        services.AddSingleton(store);
        if (audit is not null)
        {
            services.AddSingleton(audit);
        }

        services.AddSingleton(state);
        var provider = services.BuildServiceProvider();
        return (new BreakTheGlassHealthCheck(provider, Microsoft.Extensions.Options.Options.Create(Options()), time), state, time);
    }

    private static IBreakTheGlassStore NonMemoryStore(Either<EncinaError, Unit> probe)
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        store.ProbeAsync(default).ReturnsForAnyArgs(ValueTask.FromResult(probe));
        return store;
    }

    [Fact]
    public async Task Health_ReachableStore_IsHealthy()
    {
        var (check, _, _) = Health(NonMemoryStore(Unit.Default), Substitute.For<IOperationAuditStore>());

        (await check.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Health_ProbeLeft_IsDegraded_WithoutTheErrorMessage()
    {
        var (check, _, _) = Health(NonMemoryStore(EncinaErrors.Create("break_glass.store_failed", "secret-detail")));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        (result.Description + string.Join(',', result.Data.Values)).ShouldNotContain("secret-detail");
    }

    [Fact]
    public async Task Health_ProbeThrows_IsUnhealthy_WithTheExceptionTypeOnly()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        store.ProbeAsync(default).ReturnsForAnyArgs<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("secret-detail"));
        var (check, _, _) = Health(store);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Health check failed: InvalidOperationException");
    }

    [Fact]
    public async Task Health_Cancelled_IsUnhealthy()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        store.ProbeAsync(default).ReturnsForAnyArgs<ValueTask<Either<EncinaError, Unit>>>(_ => throw new OperationCanceledException(cts.Token));
        var (check, _, _) = Health(store);

        (await check.CheckHealthAsync(new HealthCheckContext(), cts.Token)).Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Health_InMemoryElevationStore_IsDegraded()
    {
        var (check, _, _) = Health(new InMemoryBreakTheGlassStore(), Substitute.For<IOperationAuditStore>());

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["state"].ShouldBe("in_memory_store");
    }

    [Fact]
    public async Task Health_InMemoryAuditStore_IsDegraded()
    {
        var (check, _, _) = Health(NonMemoryStore(Unit.Default), new InMemoryOperationAuditStore());

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["state"].ShouldBe("in_memory_audit_store");
    }

    [Fact]
    public async Task Health_SweeperThatCompletesNothingForThreeIntervals_IsDegraded()
    {
        var (check, state, time) = Health(NonMemoryStore(Unit.Default), Substitute.For<IOperationAuditStore>());
        state.RecordStarted(time.GetUtcNow());

        time.Advance(TimeSpan.FromMinutes(2));
        (await check.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(HealthStatus.Healthy);

        time.Advance(TimeSpan.FromMinutes(2));
        var stale = await check.CheckHealthAsync(new HealthCheckContext());
        stale.Status.ShouldBe(HealthStatus.Degraded);
        stale.Data["state"].ShouldBe("sweep_stale");

        state.RecordCompleted(time.GetUtcNow());
        (await check.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void Health_NameAndTags_AreStable()
    {
        BreakTheGlassHealthCheck.DefaultName.ShouldBe("encina-break-the-glass");
        BreakTheGlassHealthCheck.Tags.ShouldBe(["encina", "security", "break-the-glass", "ready"]);
    }

    // ── Start-up validator ────────────────────────────────────────────

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "test";

        public string ContentRootPath { get; set; } = ".";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static Task StartAsync(
        BreakTheGlassOptions options,
        IBreakTheGlassStore? store = null,
        IOperationAuditStore? audit = null,
        string? environment = "Production",
        bool tenancy = false,
        System.Reflection.Assembly[]? assemblies = null,
        FakeLogger<BreakTheGlassStartupValidator>? logger = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store ?? new InMemoryBreakTheGlassStore());
        if (audit is not null)
        {
            services.AddSingleton(audit);
        }

        if (environment is not null)
        {
            services.AddSingleton<IHostEnvironment>(new FakeEnvironment(environment));
        }

        if (tenancy)
        {
            services.AddSingleton(Substitute.For<ITenantProvider>());
        }

        var provider = services.BuildServiceProvider();
        var validator = new BreakTheGlassStartupValidator(
            provider,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(options),
            new BreakTheGlassScanTargets(assemblies ?? NoAssemblies),
            logger ?? new FakeLogger<BreakTheGlassStartupValidator>());
        return validator.StartAsync(CancellationToken.None);
    }

    private static IBreakTheGlassStore Database() => Substitute.For<IBreakTheGlassStore>();

    [Fact]
    public async Task Startup_ValidConfiguration_Passes() =>
        await StartAsync(Options(), Database(), Substitute.For<IOperationAuditStore>());

    [Fact]
    public async Task Startup_Disabled_ReturnsAtOnce_WithoutResolvingAnything()
    {
        var options = new BreakTheGlassOptions { Enabled = false };

        await StartAsync(options, audit: null, tenancy: true);
    }

    [Fact]
    public async Task Startup_MissingAuditStore_FailsNamingTheRegistration_AndLogsCritical()
    {
        var logger = new FakeLogger<BreakTheGlassStartupValidator>();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(Options(), Database(), audit: null, logger: logger));

        ex.Message.ShouldContain("IOperationAuditStore");
        logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9761);
        logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9763);
    }

    [Fact]
    public async Task Startup_InMemoryAuditStore_OnlyWarns()
    {
        var logger = new FakeLogger<BreakTheGlassStartupValidator>();

        await StartAsync(Options(), Database(), new InMemoryOperationAuditStore(), logger: logger);

        logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9760);
    }

    [Theory]
    [InlineData("Production", false, true)]
    [InlineData("Development", false, false)]
    [InlineData("Production", true, false)]
    public async Task Startup_InMemoryElevationStore_FailsOutsideDevelopmentUnlessAllowed(string environment, bool allow, bool fails)
    {
        var options = Options(o => o.AllowInMemoryStore = allow);
        var logger = new FakeLogger<BreakTheGlassStartupValidator>();
        var start = () => StartAsync(options, new InMemoryBreakTheGlassStore(), new InMemoryOperationAuditStore(), environment, logger: logger);

        if (fails)
        {
            (await Should.ThrowAsync<InvalidOperationException>(start)).Message.ShouldContain("in-memory break-the-glass store");
        }
        else
        {
            await start();
            logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9759);
        }
    }

    [Fact]
    public async Task Startup_InMemoryStoreWithoutAnyHostEnvironment_AndAllowed_Logs()
    {
        var logger = new FakeLogger<BreakTheGlassStartupValidator>();

        await StartAsync(Options(o => o.AllowInMemoryStore = true), new InMemoryBreakTheGlassStore(), new InMemoryOperationAuditStore(), environment: null, logger: logger);

        logger.Collector.GetSnapshot().Single(e => e.Id.Id == 9759).Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task Startup_TenancyRegisteredWithoutRequireTenant_Fails()
    {
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => StartAsync(Options(), Database(), Substitute.For<IOperationAuditStore>(), tenancy: true));

        ex.Message.ShouldContain("RequireTenant");
    }

    [Fact]
    public async Task Startup_TenancyRegisteredWithRequireTenant_Passes() =>
        await StartAsync(Options(o => o.RequireTenant = true), Database(), Substitute.For<IOperationAuditStore>(), tenancy: true);

    [Fact]
    public async Task Startup_TenancyRegisteredWithExplicitOptOut_PassesAndLogsTheOptOut()
    {
        var logger = new FakeLogger<BreakTheGlassStartupValidator>();

        await StartAsync(Options(o => o.AllowMissingTenantRequirement = true), Database(), Substitute.For<IOperationAuditStore>(), tenancy: true, logger: logger);

        logger.Collector.GetSnapshot().ShouldContain(e => e.Id.Id == 9762);
    }

    [Fact]
    public async Task Startup_NoTenancy_RequireTenantFalse_Passes() =>
        await StartAsync(Options(), Database(), Substitute.For<IOperationAuditStore>(), tenancy: false);

    [Fact]
    public async Task Startup_EligibleTypes_AreCheckedAgainstTheDeclaredScopesAndSubjectProperties()
    {
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(
            Options(), Database(), Substitute.For<IOperationAuditStore>(), assemblies: [typeof(StartupGoodRequest).Assembly]));

        ex.Message.ShouldContain(nameof(StartupUndeclaredScopeRequest));
        ex.Message.ShouldContain(nameof(StartupMissingPropertyRequest));
        ex.Message.ShouldContain(nameof(StartupUnconvertibleRequest));
        ex.Message.ShouldNotContain(nameof(StartupGoodRequest));
    }
}
