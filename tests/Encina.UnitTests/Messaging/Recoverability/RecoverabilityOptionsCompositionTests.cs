using Encina.Messaging;
using Encina.Messaging.Recoverability;
using Encina.Recoverability;
using Encina.UnitTests.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// One <see cref="RecoverabilityOptions"/> value for the recoverability registration (#3262): the options pattern
/// (<c>Configure</c>) holds it, and the scheduler, the processor and the startup check decide from that single
/// value when the container resolves them. Recoverability is registered once per host (#3263), so each test
/// registers one path: <c>AddEncinaRecoverability</c> or <c>UseRecoverability</c>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RecoverabilityOptionsCompositionTests
{
    private static readonly ServiceProviderOptions Validate = new() { ValidateOnBuild = true, ValidateScopes = true };

    public sealed record ProbeCommand(int Value) : IRequest<int>;

    public enum RegistrationPath
    {
        AddEncinaRecoverability,
        UseRecoverability
    }

    private sealed class MarkerClassifier : IErrorClassifier
    {
        public ErrorClassification Classify(EncinaError encinaError, Exception? exception) => ErrorClassification.Permanent;
    }

    private static void UseRecoverability(IServiceCollection services, Action<RecoverabilityOptions> configure)
    {
        var config = new MessagingConfiguration { UseRecoverability = true };
        configure(config.RecoverabilityOptions);
        MessagingServiceCollectionExtensions.RegisterRecoverability(services, config);
    }

    private static void Register(IServiceCollection services, RegistrationPath path, Action<RecoverabilityOptions> configure)
    {
        if (path == RegistrationPath.AddEncinaRecoverability)
            services.AddEncinaRecoverability(configure);
        else
            UseRecoverability(services, configure);
    }

    private static ServiceCollection Compose(
        RegistrationPath path,
        Action<RecoverabilityOptions> configure,
        bool withStore,
        bool withEncina = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (withEncina)
            services.AddSingleton(Substitute.For<IEncina>());

        Register(services, path, configure);

        if (withStore)
        {
            services.AddEncinaDelayedRetryStore<
                MessageSerializerRegistrationTests.ProbeDelayedRetryStore,
                MessageSerializerRegistrationTests.ProbeDelayedRetryMessageFactory>();
        }

        return services;
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability, true)]
    [InlineData(RegistrationPath.AddEncinaRecoverability, false)]
    [InlineData(RegistrationPath.UseRecoverability, true)]
    [InlineData(RegistrationPath.UseRecoverability, false)]
    public void EachPath_GivesOneEffectiveValueThroughTheOptionsPattern(RegistrationPath path, bool delayedRetries)
    {
        var services = Compose(
            path,
            o => { o.EnableDelayedRetries = delayedRetries; o.ImmediateRetries = 7; },
            withStore: true);

        using var provider = services.BuildServiceProvider(Validate);

        var options = provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value;
        options.EnableDelayedRetries.ShouldBe(delayedRetries);
        options.ImmediateRetries.ShouldBe(7);
        provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value.ShouldBeSameAs(options);
        provider.GetService<RecoverabilityOptions>().ShouldBeNull();
    }

    [Fact]
    public void UseRecoverability_OverwritesAConfigureMadeBeforeIt()
    {
        // The only remaining composition: an application Configure made before UseRecoverability is overwritten
        // as a whole, including the settings left at their default.
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<RecoverabilityOptions>(o => { o.ImmediateRetries = 11; o.UseJitter = false; });
        UseRecoverability(services, o => o.ImmediateRetries = 2);

        using var provider = services.BuildServiceProvider(Validate);

        var options = provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value;
        options.ImmediateRetries.ShouldBe(2);
        options.UseJitter.ShouldBeTrue();
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability, true, true)]
    [InlineData(RegistrationPath.AddEncinaRecoverability, true, false)]
    [InlineData(RegistrationPath.AddEncinaRecoverability, false, true)]
    [InlineData(RegistrationPath.AddEncinaRecoverability, false, false)]
    [InlineData(RegistrationPath.UseRecoverability, true, true)]
    [InlineData(RegistrationPath.UseRecoverability, true, false)]
    [InlineData(RegistrationPath.UseRecoverability, false, true)]
    [InlineData(RegistrationPath.UseRecoverability, false, false)]
    public void Matrix_BuildsWithValidationAndResolvesWhatTheFlagAndTheStoreAllow(RegistrationPath path, bool delayedRetries, bool withStore)
    {
        var services = Compose(
            path,
            o => o.EnableDelayedRetries = delayedRetries,
            withStore: withStore);

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        if (!delayedRetries)
        {
            scope.ServiceProvider.GetService<IDelayedRetryScheduler>().ShouldBeNull();
            scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeCommand, int>>()
                .ShouldContain(b => b is RecoverabilityPipelineBehavior<ProbeCommand, int>);
        }
        else if (withStore)
        {
            scope.ServiceProvider.GetRequiredService<IDelayedRetryScheduler>().ShouldBeOfType<DelayedRetryScheduler>();
        }
        else
        {
            var ex = Should.Throw<InvalidOperationException>(
                () => scope.ServiceProvider.GetRequiredService<IDelayedRetryScheduler>());
            ex.Message.ShouldContain(nameof(IDelayedRetryStore));
        }
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability)]
    public async Task Host_WithDelayedRetriesOnAndNoStore_FailsToStart(RegistrationPath path)
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var composed = Compose(path, o => o.EnableDelayedRetries = true, withStore: false);
                foreach (var descriptor in composed)
                    services.Add(descriptor);
            })
            .UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; })
            .Build();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());
        ex.Message.ShouldContain(nameof(IDelayedRetryStore));
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability)]
    public async Task Host_WithDelayedRetriesOffAndNoStore_StartsAndTheProcessorStaysIdle(RegistrationPath path)
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var composed = Compose(path, o => o.EnableDelayedRetries = false, withStore: false);
                foreach (var descriptor in composed)
                    services.Add(descriptor);
            })
            .UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; })
            .Build();

        await host.StartAsync();
        var processor = host.Services.GetServices<IHostedService>().OfType<DelayedRetryProcessor>().ShouldHaveSingleItem();
        await processor.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10)); // returns at once: no work, no store needed
        await host.StopAsync();
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability)]
    public void CustomClassifier_SetByEitherPath_ReplacesTheRegisteredOneInAnyOrder(RegistrationPath path)
    {
        var custom = new MarkerClassifier();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IErrorClassifier>(new DefaultErrorClassifier());
        Register(services, path, o => o.ErrorClassifier = custom);

        using var provider = services.BuildServiceProvider(Validate);

        provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void Classifier_KeptFromATypeOrFactoryRegistration_IsCreatedAtResolution()
    {
        var byType = new ServiceCollection();
        byType.AddLogging();
        byType.AddSingleton<IErrorClassifier, MarkerClassifier>();
        byType.AddEncinaRecoverability();

        var byFactory = new ServiceCollection();
        byFactory.AddLogging();
        byFactory.AddSingleton<IErrorClassifier>(_ => new MarkerClassifier());
        byFactory.AddEncinaRecoverability();

        foreach (var services in new[] { byType, byFactory })
        {
            using var provider = services.BuildServiceProvider(Validate);
            provider.GetRequiredService<IErrorClassifier>().ShouldBeOfType<MarkerClassifier>();
        }
    }

    [Fact]
    public void Classifier_KeptFromAScopedRegistration_KeepsItsLifetime()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IErrorClassifier, MarkerClassifier>();
        services.AddEncinaRecoverability();

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        services.Single(d => d.ServiceType == typeof(IErrorClassifier)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        scope.ServiceProvider.GetRequiredService<IErrorClassifier>().ShouldBeOfType<MarkerClassifier>();
    }

    private sealed class DisposableClassifier : IErrorClassifier, IDisposable, IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ErrorClassification Classify(EncinaError encinaError, Exception? exception) => ErrorClassification.Permanent;

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void OptionsClassifier_ThatIsDisposable_IsNotDisposedWithAScopeAndStillClassifiesInTheNext()
    {
        // The options own the instance; a scoped registration must not make the container dispose it with the first scope.
        var external = new DisposableClassifier();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IErrorClassifier, MarkerClassifier>();
        services.AddEncinaRecoverability(o => o.ErrorClassifier = external);

        using var provider = services.BuildServiceProvider(Validate);
        using (var first = provider.CreateScope())
        {
            first.ServiceProvider.GetRequiredService<IErrorClassifier>().Classify(EncinaErrors.Create("x", "y"), null)
                .ShouldBe(ErrorClassification.Permanent);
        }

        external.DisposeCount.ShouldBe(0);
        using var second = provider.CreateScope();
        second.ServiceProvider.GetRequiredService<IErrorClassifier>().Classify(EncinaErrors.Create("x", "y"), null)
            .ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void EarlierInstance_ThatIsDisposable_IsNotDisposedWithTheProvider()
    {
        var instance = new DisposableClassifier();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IErrorClassifier>(instance);
        services.AddEncinaRecoverability();

        var provider = services.BuildServiceProvider(Validate);
        provider.GetRequiredService<IErrorClassifier>();
        provider.Dispose();

        instance.DisposeCount.ShouldBe(0);
    }

    [Fact]
    public void Classifier_CreatedByTheContainer_IsStillDisposedWithTheProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IErrorClassifier, DisposableClassifier>();
        services.AddEncinaRecoverability();

        var provider = services.BuildServiceProvider(Validate);
        var created = provider.GetRequiredService<IErrorClassifier>().ShouldBeOfType<DisposableClassifier>();
        provider.Dispose();

        created.DisposeCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ConfigureAction_InjectingTheClassifier_ReceivesTheEarlierRegistrationWithoutACycle()
    {
        var earlier = new MarkerClassifier();
        IErrorClassifier? received = null;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IErrorClassifier>(earlier);
        services.AddOptions<RecoverabilityOptions>().Configure<IErrorClassifier>((_, c) => received = c);
        services.AddEncinaRecoverability();

        using var provider = services.BuildServiceProvider(Validate);

        provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(earlier);
        received.ShouldBeSameAs(earlier);
    }

    [Fact]
    public void ConfigureAction_InjectingTheClassifier_WithNoEarlierRegistration_ReceivesTheDefault()
    {
        IErrorClassifier? received = null;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRecoverability();
        services.AddOptions<RecoverabilityOptions>().Configure<IErrorClassifier>((_, c) => received = c);

        using var provider = services.BuildServiceProvider(Validate);

        provider.GetRequiredService<IErrorClassifier>().ShouldBeOfType<DefaultErrorClassifier>();
        received.ShouldBeOfType<DefaultErrorClassifier>();
    }

    [Fact]
    public void ConfigureDelegate_IsAppliedWhenTheOptionsAreResolved_NotWhenRegistered()
    {
        var services = new ServiceCollection();
        var invoked = false;
        services.AddEncinaRecoverability(_ => invoked = true);

        invoked.ShouldBeFalse();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value.ShouldNotBeNull();
        invoked.ShouldBeTrue();
    }

    [Fact]
    public void CopyTo_CopiesEverySetting()
    {
        var source = new RecoverabilityOptions
        {
            ImmediateRetries = 1,
            ImmediateRetryDelay = TimeSpan.FromSeconds(2),
            UseExponentialBackoffForImmediateRetries = false,
            DelayedRetries = [TimeSpan.FromSeconds(3)],
            OnPermanentFailure = (_, _) => Task.CompletedTask,
            ErrorClassifier = new MarkerClassifier(),
            EnableDelayedRetries = true,
            UseJitter = false,
            MaxJitterPercent = 5,
            MaxRetryAfter = TimeSpan.FromHours(1)
        };
        var target = new RecoverabilityOptions();

        source.CopyTo(target);

        target.ShouldBeEquivalentTo(source);
    }
}
