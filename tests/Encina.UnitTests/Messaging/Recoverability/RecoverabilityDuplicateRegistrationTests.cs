using Encina.EntityFrameworkCore;
using Encina.Messaging;
using Encina.Messaging.Recoverability;
using Encina.Recoverability;
using Encina.UnitTests.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Recoverability is registered once per host (#3263, ADR-050): a second registration, a
/// <see cref="RecoverabilityOptions"/> instance next to it or a raw duplicate of the behavior is an error that
/// names the duplicate, the module and the fix. The registration methods throw at registration; a duplicate added
/// directly to the collection after them fails the host start.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RecoverabilityDuplicateRegistrationTests
{
    private static readonly ServiceProviderOptions Validate = new() { ValidateOnBuild = true, ValidateScopes = true };

    public enum RegistrationPath
    {
        AddEncinaRecoverability,
        UseRecoverability
    }

    private sealed class MarkerClassifier : IErrorClassifier
    {
        public ErrorClassification Classify(EncinaError encinaError, Exception? exception) => ErrorClassification.Permanent;
    }

    private sealed class CapturedLog(EventId eventId, LogLevel level)
    {
        public EventId EventId { get; } = eventId;

        public LogLevel Level { get; } = level;
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<CapturedLog> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<CapturedLog> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => entries.Add(new CapturedLog(eventId, logLevel));
        }
    }

    private static void Register(IServiceCollection services, RegistrationPath path)
    {
        if (path == RegistrationPath.AddEncinaRecoverability)
        {
            services.AddEncinaRecoverability();
            return;
        }

        MessagingServiceCollectionExtensions.RegisterRecoverability(
            services, new MessagingConfiguration { UseRecoverability = true });
    }

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static void ShouldNameTheDuplicate(InvalidOperationException ex, string duplicate)
    {
        ex.Message.ShouldContain(duplicate);
        ex.Message.ShouldContain("Recoverability");
        ex.Message.ShouldContain("Encina.Messaging");
        ex.Message.ShouldContain("ADR-050");
        ex.Message.ShouldContain("AddEncinaRecoverability");
        ex.Message.ShouldContain("UseRecoverability");
    }

    private static IHost BuildHost(Action<IServiceCollection> configure, CapturingLoggerProvider? log = null)
        => new HostBuilder()
            .ConfigureServices(services =>
            {
                if (log is null)
                    services.AddLogging();
                else
                    services.AddLogging(b => b.AddProvider(log));
                configure(services);
            })
            .UseDefaultServiceProvider(o => { o.ValidateOnBuild = true; o.ValidateScopes = true; })
            .Build();

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability, RegistrationPath.UseRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability, RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.AddEncinaRecoverability, RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability, RegistrationPath.UseRecoverability)]
    public void SecondRegistration_InAnyCombination_ThrowsNamingTheDuplicateAndTheFix(
        RegistrationPath first, RegistrationPath second)
    {
        var services = NewServices();
        Register(services, first);

        var ex = Should.Throw<InvalidOperationException>(() => Register(services, second));

        ShouldNameTheDuplicate(ex, "RecoverabilityPipelineBehavior");
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability)]
    public void RejectedRegistration_AddsNothingToTheCollection(RegistrationPath second)
    {
        var services = NewServices();
        services.AddEncinaRecoverability();
        var before = services.ToList();

        Should.Throw<InvalidOperationException>(() => Register(services, second));

        services.ShouldBe(before);
    }

    [Fact]
    public void GenericRegistration_AfterAnotherOne_ThrowsBeforeAddingTheClassifier()
    {
        var services = NewServices();
        services.AddEncinaRecoverability();
        var before = services.Count;

        var ex = Should.Throw<InvalidOperationException>(() => services.AddEncinaRecoverability<MarkerClassifier>());

        ShouldNameTheDuplicate(ex, "RecoverabilityPipelineBehavior");
        services.Count.ShouldBe(before);
    }

    [Fact]
    public void GenericRegistration_BeforeUseRecoverability_ThrowsInBothOrders()
    {
        var generic = NewServices();
        generic.AddEncinaRecoverability<MarkerClassifier>();
        Should.Throw<InvalidOperationException>(() => Register(generic, RegistrationPath.UseRecoverability));

        var use = NewServices();
        Register(use, RegistrationPath.UseRecoverability);
        Should.Throw<InvalidOperationException>(() => use.AddEncinaRecoverability<MarkerClassifier>());
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability)]
    public void OptionsInstanceRegisteredBefore_Throws(RegistrationPath path)
    {
        var services = NewServices();
        services.AddSingleton(new RecoverabilityOptions());

        var ex = Should.Throw<InvalidOperationException>(() => Register(services, path));

        ShouldNameTheDuplicate(ex, "RecoverabilityOptions instance");
        ex.Message.ShouldContain("Configure<RecoverabilityOptions>");
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability)]
    public void SingleRegistration_BuildsWithValidationAndRegistersTheBehaviorOnce(RegistrationPath path)
    {
        var services = NewServices();
        Register(services, path);

        using var provider = services.BuildServiceProvider(Validate);

        services.Count(d => d.ImplementationType == typeof(RecoverabilityPipelineBehavior<,>)).ShouldBe(1);
        provider.GetServices<IHostedService>().OfType<RecoverabilityRegistrationCheck>().ShouldHaveSingleItem();
    }

    [Fact]
    public void AppConfigureBeforeUseRecoverability_IsNotADuplicate()
    {
        var services = NewServices();
        services.Configure<RecoverabilityOptions>(o => o.ImmediateRetries = 4);

        Should.NotThrow(() => Register(services, RegistrationPath.UseRecoverability));
    }

    [Fact]
    public void UseRecoverabilityOff_NextToAddEncinaRecoverability_IsNotADuplicate()
    {
        var services = NewServices();
        services.AddEncinaRecoverability();

        Should.NotThrow(() => MessagingServiceCollectionExtensions.RegisterRecoverability(
            services, new MessagingConfiguration { UseRecoverability = false }));
    }

    [Fact]
    public void ClosedRegistrationOfTheBehavior_IsNotCounted()
    {
        // Only the open generic registration is checked: a closed one is a deliberate, narrower choice.
        var services = NewServices();
        services.AddScoped(typeof(IPipelineBehavior<Probe, int>), typeof(RecoverabilityPipelineBehavior<Probe, int>));

        Should.NotThrow(() => services.AddEncinaRecoverability());
    }

    public sealed record Probe : IRequest<int>;

    [Fact]
    public void KeyedOptionsRegistration_IsNotCounted()
    {
        var services = NewServices();
        services.AddKeyedSingleton("other", new RecoverabilityOptions());

        Should.NotThrow(() => services.AddEncinaRecoverability());
    }

    [Fact]
    public async Task Host_WithOneRegistration_Starts()
    {
        var log = new CapturingLoggerProvider();
        using var host = BuildHost(services => services.AddEncinaRecoverability(), log);

        await host.StartAsync();
        await host.StopAsync();

        log.Entries.ShouldNotContain(e => e.EventId.Id == 5458);
    }

    [Fact]
    public async Task Host_WhenTheBehaviorIsAddedRawAfterEveryEncinaCall_FailsToStartAndLogsCritical()
    {
        var log = new CapturingLoggerProvider();
        using var host = BuildHost(
            services =>
            {
                services.AddEncinaRecoverability();
                services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));
            },
            log);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());

        ShouldNameTheDuplicate(ex, "RecoverabilityPipelineBehavior is registered 2 times");
        log.Entries.ShouldContain(e => e.EventId.Id == 5458 && e.Level == LogLevel.Critical);
    }

    [Theory]
    [InlineData(RegistrationPath.AddEncinaRecoverability)]
    [InlineData(RegistrationPath.UseRecoverability)]
    public async Task Host_WhenAnOptionsInstanceIsAddedRawAfterEveryEncinaCall_FailsToStartAndLogsCritical(RegistrationPath path)
    {
        var log = new CapturingLoggerProvider();
        using var host = BuildHost(
            services =>
            {
                Register(services, path);
                services.AddSingleton(new RecoverabilityOptions());
            },
            log);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());

        ShouldNameTheDuplicate(ex, "RecoverabilityOptions instance");
        log.Entries.ShouldContain(e => e.EventId.Id == 5458 && e.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task Host_WhenTheBehaviorIsRemovedAndAnOptionsInstanceIsAddedRaw_FailsToStartAndLogsCritical()
    {
        var log = new CapturingLoggerProvider();
        using var host = BuildHost(
            services =>
            {
                services.AddEncinaRecoverability();
                var behavior = services.Single(d =>
                    d.ServiceType == typeof(IPipelineBehavior<,>)
                    && d.ImplementationType == typeof(RecoverabilityPipelineBehavior<,>));
                services.Remove(behavior);
                services.AddSingleton(new RecoverabilityOptions());
            },
            log);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());

        ShouldNameTheDuplicate(ex, "RecoverabilityOptions instance");
        log.Entries.ShouldContain(e => e.EventId.Id == 5458 && e.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task Host_WithDelayedRetriesOff_StillRunsTheDuplicateCheck()
    {
        using var host = BuildHost(services =>
        {
            services.AddEncinaRecoverability(o => o.EnableDelayedRetries = false);
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RecoverabilityPipelineBehavior<,>));
        });

        await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());
    }

    [Fact]
    public void Check_WithoutAServiceCollection_RejectsNullArguments()
    {
        Should.Throw<ArgumentNullException>(() => new RecoverabilityRegistrationCheck(
            null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<RecoverabilityRegistrationCheck>.Instance));
        Should.Throw<ArgumentNullException>(() => new RecoverabilityRegistrationCheck(
            new RecoverabilityDescriptorSnapshot(NewServices()), null!));
    }

    [Fact]
    public async Task Check_StopAsync_Completes()
    {
        var check = new RecoverabilityRegistrationCheck(
            new RecoverabilityDescriptorSnapshot(NewServices()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RecoverabilityRegistrationCheck>.Instance);

        await check.StartAsync(CancellationToken.None);
        await check.StopAsync(CancellationToken.None);
    }

    private static ServiceCollection RegisterEntityFramework(Action<MessagingConfiguration> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("RecoverabilityRoutersDb"));
        services.AddEncinaEntityFrameworkCore<TestDbContext>(configure);
        return services;
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_UseRecoverabilityNextToAddEncinaRecoverability_ThrowsInBothOrders()
    {
        var afterEfCore = RegisterEntityFramework(config => config.UseRecoverability = true);
        var first = Should.Throw<InvalidOperationException>(() => afterEfCore.AddEncinaRecoverability());

        var efCoreAfter = new ServiceCollection();
        efCoreAfter.AddLogging();
        efCoreAfter.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("RecoverabilityDuplicateDb"));
        efCoreAfter.AddEncinaRecoverability();
        var second = Should.Throw<InvalidOperationException>(
            () => efCoreAfter.AddEncinaEntityFrameworkCore<TestDbContext>(config => config.UseRecoverability = true));

        foreach (var message in new[] { first.Message, second.Message })
        {
            message.ShouldContain("RecoverabilityPipelineBehavior");
            message.ShouldContain("ADR-050");
            message.ShouldContain("UseRecoverability");
        }
    }
}
