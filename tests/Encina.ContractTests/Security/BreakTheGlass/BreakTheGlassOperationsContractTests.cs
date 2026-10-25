using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Security.BreakTheGlass.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Encina.ContractTests.Security.BreakTheGlass;

/// <summary>
/// Contract tests of the Phase 4 services, instantiated through the DI registration: the options
/// validator honours <see cref="IValidateOptions{TOptions}"/>, the health check honours
/// <see cref="IHealthCheck"/>, and the hosted services honour the <see cref="IHostedService"/> lifecycle.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassOperationsContractTests
{
    private static ServiceProvider Provider(Action<BreakTheGlassOptions>? configure = null, FakeTimeProvider? time = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(time ?? new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.Zero)));
        services.AddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>();
        services.AddEncinaBreakTheGlass(options =>
        {
            options.DeclareScope("clinical-record", scope => scope.OpenerRoles.Add("clinician"));
            options.ReviewerRoles.Add("reviewer");
            options.EnableAutoPurge = true;
            options.AddHealthCheck = true;
            options.AllowInMemoryStore = true;
            configure?.Invoke(options);
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public void OptionsValidator_IsRegisteredAsIValidateOptions_AndRejectsInvalidOptions()
    {
        using var provider = Provider(o => o.ExpirySweepBatchSize = 0);

        var validators = provider.GetServices<IValidateOptions<BreakTheGlassOptions>>().ToList();

        validators.ShouldHaveSingleItem().Validate(null, provider.GetRequiredService<IOptions<BreakTheGlassOptions>>().Value).Failed.ShouldBeTrue();
        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BreakTheGlassOptions>>().Value);
    }

    [Fact]
    public void HealthCheck_HonoursTheContract_AndCarriesTheReadyTag()
    {
        using var provider = Provider();

        var check = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.ShouldHaveSingleItem();

        check.Factory(provider).ShouldBeAssignableTo<IHealthCheck>();
        check.Tags.ShouldBe(BreakTheGlassHealthCheck.Tags, ignoreOrder: true);
    }

    [Fact]
    public async Task HealthCheck_WithInMemoryStores_ReportsDegradedNeverHealthy()
    {
        using var provider = Provider();
        var check = new BreakTheGlassHealthCheck(provider, provider.GetRequiredService<IOptions<BreakTheGlassOptions>>(), provider.GetRequiredService<TimeProvider>());

        (await check.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task HostedServices_StartAndStopCleanly_InRegistrationOrder()
    {
        using var provider = Provider();
        var hosted = provider.GetServices<IHostedService>().ToList();

        hosted.Select(h => h.GetType()).ToList().ShouldBe(
            [typeof(BreakTheGlassStartupValidator), typeof(BreakTheGlassExpiryService), typeof(BreakTheGlassPurgeService)], ignoreOrder: false);

        foreach (var service in hosted.Where(h => h is not BreakTheGlassStartupValidator))
        {
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }

        // The start-up validator passes: the in-memory stores are allowed explicitly, nothing else is wrong.
        await hosted.OfType<BreakTheGlassStartupValidator>().Single().StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExpirySweeper_FollowsTheStoreContract_ClosedRowsAreRecordedOnce()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.Zero));
        using var provider = Provider(time: time);
        var store = provider.GetRequiredService<IBreakTheGlassStore>();
        var elevation = new BreakTheGlassElevation
        {
            ElevationId = Guid.NewGuid(),
            TenantId = "t",
            UserId = "u",
            SubjectId = "s",
            Scope = "clinical-record",
            Justification = "justification",
            OpenedAtUtc = time.GetUtcNow(),
            ExpiresAtUtc = time.GetUtcNow().AddMinutes(5)
        };
        (await store.AddAsync(elevation)).IsRight.ShouldBeTrue();
        time.Advance(TimeSpan.FromMinutes(10));
        var sweeper = new BreakTheGlassExpiryService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IInternalRequestContextScopeFactory>(),
            provider.GetRequiredService<IOptions<BreakTheGlassOptions>>(),
            provider.GetRequiredService<BreakTheGlassSweepState>(),
            time,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BreakTheGlassExpiryService>.Instance);

        (await sweeper.SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();
        (await sweeper.SweepOnceAsync(CancellationToken.None)).ShouldBeTrue();

        var pending = await store.GetExpiredUnrecordedAsync(time.GetUtcNow(), 10);
        pending.Match(Right: rows => rows.Count, Left: _ => -1).ShouldBe(0);
        provider.GetRequiredService<IOperationAuditStore>().ShouldBeOfType<InMemoryOperationAuditStore>()
            .GetAllEntries().Count(e => e.Action == "break_glass.expired").ShouldBe(1);
    }
}
