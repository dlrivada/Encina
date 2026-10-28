using Encina.Messaging;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Registration completeness of the Retry-After ceiling settings (#2588): the options of the recoverability
/// registration and of the outbox registration resolve with their defaults and with a configured value, and the
/// components that read them resolve too, with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> on.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RetryAfterCeilingRegistrationTests
{
    private const string ConnectionString = "Server=localhost;Database=probe;User Id=probe;Password=probe;TrustServerCertificate=True";

    public sealed record ProbeRequest(string Value) : IRequest<string>;

    private static ServiceProvider Build(Action<MessagingConfiguration>? configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        global::Encina.Dapper.SqlServer.ServiceCollectionExtensions.AddEncinaDapper(services, ConnectionString, config =>
        {
            config.UseOutbox = true;
            config.UseRecoverability = true;
            config.RecoverabilityOptions.EnableDelayedRetries = false;
            configure?.Invoke(config);
        });

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void Defaults_ResolveAsTwentyFourHoursAndEveryComponentResolves()
    {
        using var provider = Build(configure: null);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        provider.GetRequiredService<RecoverabilityOptions>().MaxRetryAfter.ShouldBe(TimeSpan.FromHours(24));
        provider.GetRequiredService<OutboxOptions>().MaxRetryAfter.ShouldBe(TimeSpan.FromHours(24));
        sp.GetServices<IPipelineBehavior<ProbeRequest, string>>()
            .ShouldContain(b => b is RecoverabilityPipelineBehavior<ProbeRequest, string>);
        sp.GetRequiredService<OutboxOrchestrator>().ShouldNotBeNull();
        provider.GetServices<IHostedService>().ShouldContain(h => h.GetType().Name.Contains("Outbox", StringComparison.Ordinal));
    }

    [Fact]
    public void ConfiguredValues_FlowThroughToTheResolvedOptions()
    {
        using var provider = Build(config =>
        {
            config.RecoverabilityOptions.MaxRetryAfter = TimeSpan.FromHours(6);
            config.OutboxOptions.MaxRetryAfter = TimeSpan.FromHours(12);
        });

        provider.GetRequiredService<RecoverabilityOptions>().MaxRetryAfter.ShouldBe(TimeSpan.FromHours(6));
        provider.GetRequiredService<OutboxOptions>().MaxRetryAfter.ShouldBe(TimeSpan.FromHours(12));
    }

    [Fact]
    public void RecoverabilityRegistration_WithDelayedRetries_ResolvesTheProcessorWithTheConfiguredCeiling()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRecoverability(options =>
        {
            options.MaxRetryAfter = TimeSpan.FromHours(3);
            options.EnableDelayedRetries = true;
        });

        // The store is registered after the recoverability registration, as a raw descriptor.
        services.AddSingleton(Substitute.For<IDelayedRetryStore>());
        services.AddSingleton(Substitute.For<IDelayedRetryMessageFactory>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<RecoverabilityOptions>().MaxRetryAfter.ShouldBe(TimeSpan.FromHours(3));
        provider.GetServices<IHostedService>().ShouldContain(h => h is DelayedRetryProcessor);
    }
}
