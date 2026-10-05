using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>Scans this test assembly once: every misconfigured shape must be in the single exception.</summary>
public sealed class MisconfiguredScanFixture : IDisposable
{
    public MisconfiguredScanFixture()
    {
        Host = new StartupValidationHost(typeof(MisconfiguredScanFixture).Assembly);
        Exception = Should.Throw<CryptoShreddingConfigurationException>(Host.StartAsync);
    }

    internal StartupValidationHost Host { get; }

    internal CryptoShreddingConfigurationException Exception { get; }

    public void Dispose() => Host.Dispose();
}

[Trait("Category", "Unit")]
public sealed class CryptoShreddingStartupValidationHostedServiceTests(MisconfiguredScanFixture scan) : IClassFixture<MisconfiguredScanFixture>
{
    [Theory]
    [MemberData(nameof(CryptoShreddedPropertyClassifierTests.Rejected), MemberType = typeof(CryptoShreddedPropertyClassifierTests))]
    public void Scan_ReportsEveryMisconfiguredShapeWithItsReason(Type type, string property, CryptoShreddedPropertyProblems expected)
    {
        scan.Exception.Problem.ShouldBe(CryptoShreddingConfigurationProblem.MisconfiguredProperties);
        scan.Exception.Issues.ShouldContain(i =>
            i.DeclaringTypeName == CryptoShreddedPropertyClassifier.TypeName(type) && i.PropertyName == property && i.Problems.HasFlag(expected));
        scan.Exception.Message.ShouldContain($"{CryptoShreddedPropertyClassifier.TypeName(type)}.{property}");
    }

    [Fact]
    public void Scan_ReportsEachIssueOnceOnItsDeclaringType_AndLogs8459And8470()
    {
        scan.Exception.Issues.GroupBy(i => (i.DeclaringTypeName, i.PropertyName)).ShouldAllBe(g => g.Count() == 1);
        scan.Host.Logs.ShouldContain(r => r.Id.Id == 8459 && r.Level == LogLevel.Error);
        scan.Host.Logs.ShouldContain(r => r.Id.Id == 8470 && r.Level == LogLevel.Error);
    }

    [Fact]
    public void Scan_AcceptsOpenGenericOwnersWithDeferral8473_AndWarns8478And8484()
    {
        scan.Exception.Issues.ShouldNotContain(i => i.DeclaringTypeName.Contains("GenericOwner`1[[System.Guid", StringComparison.Ordinal));
        scan.Host.Logs.ShouldContain(r => r.Id.Id == 8473);
        scan.Host.Logs.ShouldContain(r => r.Id.Id == 8478 && r.Message.Contains(nameof(PersonalDataSubjectOwner), StringComparison.Ordinal));
        scan.Host.Logs.ShouldContain(r => r.Id.Id == 8484 && r.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task ValidAssembly_Passes_AndLogs8461()
    {
        using var host = new StartupValidationHost(typeof(CryptoShredderSerializer).Assembly);

        await host.StartAsync();

        host.Logs.ShouldContain(r => r.Id.Id == 8461);
    }

    [Fact]
    public async Task Disabled_LogsTheOptOutAsAWarning()
    {
        using var host = new StartupValidationHost(typeof(MisconfiguredScanFixture).Assembly, validateOnStartup: false);

        await host.StartAsync();

        host.Logs.ShouldContain(r => r.Id.Id == 8462 && r.Level == LogLevel.Warning);
    }

    [Fact]
    public void SerializerNotWrapped_FailsStartup()
    {
        using var host = new StartupValidationHost(typeof(CryptoShredderSerializer).Assembly, configureMarten: false);

        Should.Throw<CryptoShreddingConfigurationException>(host.StartAsync).Problem.ShouldBe(CryptoShreddingConfigurationProblem.SerializerNotWrapped);
        host.Logs.ShouldContain(r => r.Id.Id == 8471 && r.Level == LogLevel.Critical);
    }

    [Fact]
    public void ResolverReplacedAfterInstallation_FailsStartup()
    {
        using var host = new StartupValidationHost(
            typeof(CryptoShredderSerializer).Assembly,
            configureStore: o => ((CryptoShredderSerializer)o.Serializer()).Inner.UseTypeInfoResolver(DefaultModeContext.Default));

        Should.Throw<CryptoShreddingConfigurationException>(host.StartAsync).Problem.ShouldBe(CryptoShreddingConfigurationProblem.ContractModifierMissing);
    }

    [Fact]
    public void SkipSerializationErrorsTurnedBackOn_FailsStartup()
    {
        using var host = new StartupValidationHost(
            typeof(CryptoShredderSerializer).Assembly,
            configureStore: o => o.Projections.Errors.SkipSerializationErrors = true);

        Should.Throw<CryptoShreddingConfigurationException>(host.StartAsync).Problem.ShouldBe(CryptoShreddingConfigurationProblem.ProjectionSkipsSerializationErrors);
    }

    [Fact]
    public void ErasureStrategyRegisteredAfter_FailsStartup()
    {
        using var host = new StartupValidationHost(
            typeof(CryptoShredderSerializer).Assembly,
            after: s => s.AddScoped<IDataErasureStrategy, ApplicationErasureStrategy>());

        Should.Throw<CryptoShreddingConfigurationException>(host.StartAsync).Problem.ShouldBe(CryptoShreddingConfigurationProblem.ErasureStrategyBypassed);
    }

    [Fact]
    public async Task ErasureStrategyRegisteredBefore_BecomesTheInnerStrategy()
    {
        using var host = new StartupValidationHost(
            typeof(CryptoShredderSerializer).Assembly,
            before: s => s.AddScoped<IDataErasureStrategy, ApplicationErasureStrategy>());

        await host.StartAsync();

        using var scope = host.Provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDataErasureStrategy>().ShouldBeOfType<CryptoShredRoutingErasureStrategy>()
            .Inner.ShouldBeOfType<ApplicationErasureStrategy>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplicationLocatorWithoutComposite_FailsStartupInEitherOrder(bool registeredBefore)
    {
        void Register(IServiceCollection s) => s.AddScoped<IPersonalDataLocator, ApplicationLocator>();
        using var host = registeredBefore
            ? new StartupValidationHost(typeof(CryptoShredderSerializer).Assembly, before: Register)
            : new StartupValidationHost(typeof(CryptoShredderSerializer).Assembly, after: Register);

        if (registeredBefore)
        {
            // The Marten locator is registered last and resolves itself, so it takes part.
            Should.NotThrow(host.StartAsync);
            return;
        }

        Should.Throw<CryptoShreddingConfigurationException>(host.StartAsync).Problem.ShouldBe(CryptoShreddingConfigurationProblem.PersonalDataLocatorBypassed);
    }

    [Fact]
    public async Task CompositeIncludingTheMartenLocator_Passes()
    {
        using var host = new StartupValidationHost(
            typeof(CryptoShredderSerializer).Assembly,
            after: s =>
            {
                s.AddScoped<IPersonalDataLocator, ApplicationLocator>();
                s.AddScoped<IPersonalDataLocator>(sp => new CompositePersonalDataLocator(
                    [new ApplicationLocator(), ActivatorUtilities.CreateInstance<MartenEventPersonalDataLocator>(sp)],
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<CompositePersonalDataLocator>.Instance));
            });

        await host.StartAsync();
    }

    [Fact]
    public async Task Validation_RunsInStartingAsync_BeforeAnyHostedServiceStarts()
    {
        var started = new StartRecorder();
        using var setup = new StartupValidationHost(
            typeof(MisconfiguredScanFixture).Assembly,
            before: s => s.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(started));
        var hostedServices = setup.Provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().ToList();
        var lifecycle = hostedServices.OfType<Microsoft.Extensions.Hosting.IHostedLifecycleService>().ToList();

        // The generic host runs every StartingAsync before any StartAsync.
        var ex = await Should.ThrowAsync<CryptoShreddingConfigurationException>(async () =>
        {
            foreach (var service in lifecycle)
            {
                await service.StartingAsync(CancellationToken.None);
            }

            foreach (var service in hostedServices)
            {
                await service.StartAsync(CancellationToken.None);
            }
        });

        ex.Problem.ShouldBe(CryptoShreddingConfigurationProblem.MisconfiguredProperties);
        started.Started.ShouldBeFalse();
        var validator = lifecycle.OfType<CryptoShreddingStartupValidationHostedService>().Single();
        await validator.StartAsync(CancellationToken.None);
        await validator.StartedAsync(CancellationToken.None);
        await validator.StoppingAsync(CancellationToken.None);
        await validator.StopAsync(CancellationToken.None);
        await validator.StoppedAsync(CancellationToken.None);
    }

    private sealed class StartRecorder : Microsoft.Extensions.Hosting.IHostedService
    {
        internal bool Started { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public void IncludesMartenLocator_RecognisesTheLocatorAndComposites()
    {
        var marten = new MartenEventPersonalDataLocator(Substitute.For<IDocumentSession>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenEventPersonalDataLocator>.Instance);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CompositePersonalDataLocator>.Instance;

        CryptoShreddingStartupValidationHostedService.IncludesMartenLocator(marten).ShouldBeTrue();
        CryptoShreddingStartupValidationHostedService.IncludesMartenLocator(new CompositePersonalDataLocator([marten], logger)).ShouldBeTrue();
        CryptoShreddingStartupValidationHostedService.IncludesMartenLocator(new CompositePersonalDataLocator([new ApplicationLocator()], logger)).ShouldBeFalse();
        CryptoShreddingStartupValidationHostedService.IncludesMartenLocator(null).ShouldBeFalse();
    }
}
