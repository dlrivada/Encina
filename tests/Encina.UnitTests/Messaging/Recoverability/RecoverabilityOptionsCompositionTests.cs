using Encina.Messaging;
using Encina.Messaging.Recoverability;
using Encina.Recoverability;
using Encina.UnitTests.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// One <see cref="RecoverabilityOptions"/> value for every recoverability path (#3262): <c>AddEncinaRecoverability</c>
/// and <c>UseRecoverability</c> compose through <c>Configure</c> in registration order, and the scheduler, the
/// processor and the startup check decide from that single value when the container resolves them.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RecoverabilityOptionsCompositionTests
{
    private static readonly ServiceProviderOptions Validate = new() { ValidateOnBuild = true, ValidateScopes = true };

    public sealed record ProbeCommand(int Value) : IRequest<int>;

    public enum Order
    {
        AddEncinaRecoverabilityFirst,
        UseRecoverabilityFirst
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

    private static ServiceCollection Compose(
        Order order,
        Action<RecoverabilityOptions> viaAddEncina,
        Action<RecoverabilityOptions> viaUse,
        bool withStore,
        bool withEncina = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (withEncina)
            services.AddSingleton(Substitute.For<IEncina>());

        if (order == Order.AddEncinaRecoverabilityFirst)
        {
            services.AddEncinaRecoverability(viaAddEncina);
            UseRecoverability(services, viaUse);
        }
        else
        {
            UseRecoverability(services, viaUse);
            services.AddEncinaRecoverability(viaAddEncina);
        }

        if (withStore)
        {
            services.AddEncinaDelayedRetryStore<
                MessageSerializerRegistrationTests.ProbeDelayedRetryStore,
                MessageSerializerRegistrationTests.ProbeDelayedRetryMessageFactory>();
        }

        return services;
    }

    [Theory]
    [InlineData(Order.AddEncinaRecoverabilityFirst, true)]
    [InlineData(Order.AddEncinaRecoverabilityFirst, false)]
    [InlineData(Order.UseRecoverabilityFirst, true)]
    [InlineData(Order.UseRecoverabilityFirst, false)]
    public void BothPaths_GiveOneEffectiveValueAndTheLastRegistrationWinsInOrder(Order order, bool delayedRetries)
    {
        // The two paths set the same flag differently: the later registration is the effective one in either order.
        var services = Compose(
            order,
            viaAddEncina: o => { o.EnableDelayedRetries = delayedRetries; o.ImmediateRetries = 7; },
            viaUse: o => { o.EnableDelayedRetries = !delayedRetries; o.ImmediateRetries = 9; },
            withStore: true);

        using var provider = services.BuildServiceProvider(Validate);

        var options = provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value;
        var lastWriter = order == Order.AddEncinaRecoverabilityFirst ? !delayedRetries : delayedRetries;
        options.EnableDelayedRetries.ShouldBe(lastWriter);
        options.ImmediateRetries.ShouldBe(order == Order.AddEncinaRecoverabilityFirst ? 9 : 7);
        provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value.ShouldBeSameAs(options);
        provider.GetService<RecoverabilityOptions>().ShouldBeNull();
    }

    [Theory]
    [InlineData(Order.AddEncinaRecoverabilityFirst)]
    [InlineData(Order.UseRecoverabilityFirst)]
    public void ConsumersAgree_WhenOneTrueAndOneFalse_TheSchedulerFollowsTheEffectiveFlag(Order order)
    {
        // Reported bug: the first path said "on", the last "off" (or the reverse); the consumers read different values.
        var services = Compose(
            order,
            viaAddEncina: o => o.EnableDelayedRetries = true,
            viaUse: o => o.EnableDelayedRetries = false,
            withStore: true);

        using var provider = services.BuildServiceProvider(Validate);
        using var scope = provider.CreateScope();

        var effective = provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value.EnableDelayedRetries;
        (scope.ServiceProvider.GetService<IDelayedRetryScheduler>() is not null).ShouldBe(effective);
        scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeCommand, int>>()
            .ShouldContain(b => b is RecoverabilityPipelineBehavior<ProbeCommand, int>);
    }

    [Theory]
    [InlineData(Order.AddEncinaRecoverabilityFirst, true, true)]
    [InlineData(Order.AddEncinaRecoverabilityFirst, true, false)]
    [InlineData(Order.AddEncinaRecoverabilityFirst, false, true)]
    [InlineData(Order.AddEncinaRecoverabilityFirst, false, false)]
    [InlineData(Order.UseRecoverabilityFirst, true, true)]
    [InlineData(Order.UseRecoverabilityFirst, true, false)]
    [InlineData(Order.UseRecoverabilityFirst, false, true)]
    [InlineData(Order.UseRecoverabilityFirst, false, false)]
    public void Matrix_BuildsWithValidationAndResolvesWhatTheFlagAndTheStoreAllow(Order order, bool delayedRetries, bool withStore)
    {
        var services = Compose(
            order,
            viaAddEncina: o => o.EnableDelayedRetries = delayedRetries,
            viaUse: o => o.EnableDelayedRetries = delayedRetries,
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
    [InlineData(Order.AddEncinaRecoverabilityFirst)]
    [InlineData(Order.UseRecoverabilityFirst)]
    public async Task Host_WithDelayedRetriesOnAndNoStore_FailsToStart(Order order)
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var composed = Compose(order, o => o.EnableDelayedRetries = true, o => o.EnableDelayedRetries = true, withStore: false);
                foreach (var descriptor in composed)
                    services.Add(descriptor);
            })
            .UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; })
            .Build();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());
        ex.Message.ShouldContain(nameof(IDelayedRetryStore));
    }

    [Theory]
    [InlineData(Order.AddEncinaRecoverabilityFirst)]
    [InlineData(Order.UseRecoverabilityFirst)]
    public async Task Host_WithDelayedRetriesOffAndNoStore_StartsAndTheProcessorStaysIdle(Order order)
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var composed = Compose(order, o => o.EnableDelayedRetries = false, o => o.EnableDelayedRetries = false, withStore: false);
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
    [InlineData(Order.AddEncinaRecoverabilityFirst)]
    [InlineData(Order.UseRecoverabilityFirst)]
    public void CustomClassifier_SetByEitherPath_ReplacesTheRegisteredOneInAnyOrder(Order order)
    {
        var custom = new MarkerClassifier();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IErrorClassifier>(new DefaultErrorClassifier());
        if (order == Order.AddEncinaRecoverabilityFirst)
            services.AddEncinaRecoverability(o => o.ErrorClassifier = custom);
        else
            UseRecoverability(services, o => o.ErrorClassifier = custom);

        using var provider = services.BuildServiceProvider(Validate);

        provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void ClassifierRegisteredByBothPaths_IsDecidedOnceAndKeepsTheEarlierRegistrationWithoutACustomOne()
    {
        var earlier = new MarkerClassifier();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IErrorClassifier>(earlier);
        services.AddEncinaRecoverability();
        UseRecoverability(services, _ => { });

        using var provider = services.BuildServiceProvider(Validate);

        services.Count(d => d.ServiceType == typeof(IErrorClassifier)).ShouldBe(1);
        provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(earlier);
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
