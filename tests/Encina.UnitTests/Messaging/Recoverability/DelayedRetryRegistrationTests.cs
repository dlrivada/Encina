using System.Data;
using Encina.Messaging;
using Encina.Messaging.Recoverability;
using Encina.UnitTests.Messaging.Sagas;
using Encina.UnitTests.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Registration completeness of delayed retries (#2248): no combination of recoverability options produces an
/// unbuildable container, and delayed retries switched on without a store fail with a clear message, at host start
/// (startup check) or, without a host, when the scheduler or the behavior is resolved.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DelayedRetryRegistrationTests
{
    private static readonly ServiceProviderOptions Validate = new() { ValidateOnBuild = true, ValidateScopes = true };

    public sealed record ProbeCommand(int Value) : IRequest<int>;

    public enum StoreOrder
    {
        None,
        BeforeRecoverability,
        AfterRecoverability
    }

    public enum StoreKind
    {
        Helper,
        RawDescriptors
    }

    public static IEnumerable<object[]> StoreCases()
    {
        foreach (var kind in Enum.GetValues<StoreKind>())
        {
            foreach (var order in new[] { StoreOrder.BeforeRecoverability, StoreOrder.AfterRecoverability })
            {
                yield return [kind, order];
            }
        }
    }

    private static void AddStore(IServiceCollection services, StoreKind kind)
    {
        if (kind == StoreKind.Helper)
        {
            services.AddEncinaDelayedRetryStore<
                MessageSerializerRegistrationTests.ProbeDelayedRetryStore,
                MessageSerializerRegistrationTests.ProbeDelayedRetryMessageFactory>();
            return;
        }

        services.AddScoped<IDelayedRetryStore, MessageSerializerRegistrationTests.ProbeDelayedRetryStore>();
        services.AddScoped<IDelayedRetryMessageFactory, MessageSerializerRegistrationTests.ProbeDelayedRetryMessageFactory>();
    }

    private static ServiceCollection Build(bool enableDelayedRetries, StoreKind kind, StoreOrder order, bool withEncina = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (withEncina)
            services.AddSingleton(Substitute.For<IEncina>());
        if (order == StoreOrder.BeforeRecoverability)
            AddStore(services, kind);

        services.AddEncinaRecoverability(options => options.EnableDelayedRetries = enableDelayedRetries);

        if (order == StoreOrder.AfterRecoverability)
            AddStore(services, kind);

        return services;
    }

    private static DelayedRetryStartupCheck StartupCheckOf(IServiceProvider provider)
        => provider.GetServices<IHostedService>().OfType<DelayedRetryStartupCheck>().Single();

    [Fact]
    public void AddEncinaRecoverability_Alone_BuildsAndStartsWithoutAStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRecoverability();

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetService<IDelayedRetryScheduler>().ShouldBeNull();
        provider.GetServices<IHostedService>().OfType<DelayedRetryProcessor>().ShouldBeEmpty();
        scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeCommand, int>>()
            .ShouldContain(b => b is RecoverabilityPipelineBehavior<ProbeCommand, int>);
        Should.NotThrow(() => StartupCheckOf(provider).StartAsync(CancellationToken.None));
    }

    [Theory]
    [MemberData(nameof(StoreCases))]
    public async Task DelayedRetriesOn_WithStoreAndFactory_BuildsResolvesTheSchedulerAndStarts(StoreKind kind, StoreOrder order)
    {
        var services = Build(enableDelayedRetries: true, kind, order);

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IDelayedRetryScheduler>().ShouldBeOfType<DelayedRetryScheduler>();
        scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeCommand, int>>()
            .ShouldContain(b => b is RecoverabilityPipelineBehavior<ProbeCommand, int>);
        provider.GetServices<IHostedService>().OfType<DelayedRetryProcessor>().Count().ShouldBe(1);
        await StartupCheckOf(provider).StartAsync(CancellationToken.None);
    }

    [Fact]
    public void StartupCheck_IsRegisteredBeforeTheProcessor()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.AfterRecoverability);

        using var provider = services.BuildServiceProvider(Validate);
        var hosted = provider.GetServices<IHostedService>().Select(h => h.GetType()).ToList();

        hosted.IndexOf(typeof(DelayedRetryStartupCheck)).ShouldBeGreaterThanOrEqualTo(0);
        hosted.IndexOf(typeof(DelayedRetryStartupCheck)).ShouldBeLessThan(hosted.IndexOf(typeof(DelayedRetryProcessor)));
    }

    [Fact]
    public void RegisteringTwice_AddsTheStartupCheckOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRecoverability(o => o.EnableDelayedRetries = true);
        services.AddEncinaRecoverability(o => o.EnableDelayedRetries = true);

        services.Count(d => d.ImplementationType == typeof(DelayedRetryStartupCheck)).ShouldBe(1);
    }

    [Fact]
    public async Task StartupCheck_WithDelayedRetriesOnAndNoStore_FailsNamingTheStoreAndTheFactory()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.None);

        using var provider = services.BuildServiceProvider(Validate);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => StartupCheckOf(provider).StartAsync(CancellationToken.None));
        ex.Message.ShouldContain(nameof(IDelayedRetryStore));
        ex.Message.ShouldContain(nameof(IDelayedRetryMessageFactory));
        ex.Message.ShouldContain("AddEncinaDelayedRetryStore");
        ex.Message.ShouldContain("ADR-050");
    }

    [Fact]
    public async Task StartupCheck_WithStoreButNoFactory_NamesOnlyTheFactory()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.None);
        services.AddScoped<IDelayedRetryStore, MessageSerializerRegistrationTests.ProbeDelayedRetryStore>();

        using var provider = services.BuildServiceProvider(Validate);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => StartupCheckOf(provider).StartAsync(CancellationToken.None));
        ex.Message.ShouldContain($"but {nameof(IDelayedRetryMessageFactory)} is not registered");
    }

    [Fact]
    public async Task StartupCheck_WithFactoryButNoStore_NamesOnlyTheStore()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.None);
        services.AddScoped<IDelayedRetryMessageFactory, MessageSerializerRegistrationTests.ProbeDelayedRetryMessageFactory>();

        using var provider = services.BuildServiceProvider(Validate);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => StartupCheckOf(provider).StartAsync(CancellationToken.None));
        ex.Message.ShouldContain($"but {nameof(IDelayedRetryStore)} is not registered");
    }

    [Fact]
    public async Task StartupCheck_WithStoreAndFactoryButNoEncina_FailsNamingOnlyEncina()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.AfterRecoverability, withEncina: false);

        using var provider = services.BuildServiceProvider(Validate);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => StartupCheckOf(provider).StartAsync(CancellationToken.None));
        ex.Message.ShouldContain($"but {nameof(IEncina)} is not registered");
        ex.Message.ShouldNotContain(nameof(IDelayedRetryStore));
    }

    [Fact]
    public async Task Host_WithDelayedRetriesOnAndNoEncina_FailsToStartNamingEncina()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddEncinaRecoverability(o => o.EnableDelayedRetries = true);
                AddStore(services, StoreKind.RawDescriptors);
            })
            .UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; })
            .Build();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());
        ex.Message.ShouldContain(nameof(IEncina));
    }

    [Fact]
    public void WithoutAHost_DelayedRetriesOnAndNoEncina_TheSchedulerStillResolves()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.AfterRecoverability, withEncina: false);

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IDelayedRetryScheduler>().ShouldBeOfType<DelayedRetryScheduler>();
    }

    [Fact]
    public async Task StartupCheck_WithDelayedRetriesOnAndAnEmptyDelayedRetriesArray_StillRequiresTheStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRecoverability(o =>
        {
            o.EnableDelayedRetries = true;
            o.DelayedRetries = [];
        });

        using var provider = services.BuildServiceProvider(Validate);

        await Should.ThrowAsync<InvalidOperationException>(() => StartupCheckOf(provider).StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartupCheck_WithDelayedRetriesOff_IsANoOpEvenWithoutAStore()
    {
        var services = Build(enableDelayedRetries: false, StoreKind.Helper, StoreOrder.None);

        using var provider = services.BuildServiceProvider(Validate);

        await StartupCheckOf(provider).StartAsync(CancellationToken.None);
        await StartupCheckOf(provider).StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Host_WithDelayedRetriesOnAndNoStore_FailsToStart()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddEncinaRecoverability(o => o.EnableDelayedRetries = true);
            })
            .UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; })
            .Build();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());
        ex.Message.ShouldContain(nameof(IDelayedRetryStore));
    }

    [Fact]
    public async Task Host_WithDelayedRetriesOnAndAStore_Starts()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddEncinaRecoverability(o => o.EnableDelayedRetries = true);
                AddStore(services, StoreKind.RawDescriptors);
                services.AddSingleton(Substitute.For<IEncina>());
            })
            .UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; })
            .Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public void WithoutAHost_DelayedRetriesOnAndNoStore_ResolvingTheSchedulerFailsClearly()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.None);

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        var ex = Should.Throw<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IDelayedRetryScheduler>());
        ex.Message.ShouldContain(nameof(IDelayedRetryStore));
    }

    [Fact]
    public void WithoutAHost_DelayedRetriesOnAndNoStore_ResolvingTheBehaviorFailsInsteadOfSkippingTheDelayedPhase()
    {
        var services = Build(enableDelayedRetries: true, StoreKind.Helper, StoreOrder.None);

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        var ex = Should.Throw<InvalidOperationException>(() =>
            scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeCommand, int>>().ToList());
        ex.Message.ShouldContain(nameof(IDelayedRetryStore));
    }

    [Fact]
    public void DescribeMissing_CoversEveryCombination()
    {
        DelayedRetryStartupCheck.DescribeMissing(true, true, true).ShouldBeNull();
        DelayedRetryStartupCheck.DescribeMissing(false, false, true).ShouldBe($"{nameof(IDelayedRetryStore)} and {nameof(IDelayedRetryMessageFactory)}");
        DelayedRetryStartupCheck.DescribeMissing(false, true, true).ShouldBe(nameof(IDelayedRetryStore));
        DelayedRetryStartupCheck.DescribeMissing(true, false, true).ShouldBe(nameof(IDelayedRetryMessageFactory));
        DelayedRetryStartupCheck.DescribeMissing(true, true, false).ShouldBe(nameof(IEncina));
        DelayedRetryStartupCheck.DescribeMissing(false, false, false)
            .ShouldBe($"{nameof(IDelayedRetryStore)} and {nameof(IDelayedRetryMessageFactory)} and {nameof(IEncina)}");
    }

    [Fact]
    public void StartupCheck_WithNullArguments_Throws()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DelayedRetryStartupCheck>.Instance;
        var isService = Substitute.For<IServiceProviderIsService>();

        Should.Throw<ArgumentNullException>(() => new DelayedRetryStartupCheck(null!, isService, logger));
        Should.Throw<ArgumentNullException>(() => new DelayedRetryStartupCheck(new RecoverabilityOptions(), null!, logger));
        Should.Throw<ArgumentNullException>(() => new DelayedRetryStartupCheck(new RecoverabilityOptions(), isService, null!));
    }

    [Theory]
    [MemberData(nameof(ProviderOptionCases))]
    public void AddEncinaProvider_UseRecoverabilityWithDefaults_BuildsWithoutAStore(
        string providerName,
        Action<IServiceCollection, Action<MessagingConfiguration>> register,
        bool withStore)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());

        register(services, config =>
        {
            config.UseRecoverability = true;
            config.RecoverabilityOptions.EnableDelayedRetries = withStore;
        });
        if (withStore)
            AddStore(services, StoreKind.RawDescriptors);

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeCommand, int>>()
            .ShouldContain(b => b is RecoverabilityPipelineBehavior<ProbeCommand, int>, providerName);
        if (withStore)
            scope.ServiceProvider.GetRequiredService<IDelayedRetryScheduler>().ShouldNotBeNull(providerName);
        else
            scope.ServiceProvider.GetService<IDelayedRetryScheduler>().ShouldBeNull(providerName);
    }

    public static IEnumerable<object[]> ProviderOptionCases()
    {
        foreach (var provider in AdoDapperMessagingDiRegistrationTests.ProviderCases())
        {
            yield return [provider[0], provider[1], false];
            yield return [provider[0], provider[1], true];
        }
    }
}
