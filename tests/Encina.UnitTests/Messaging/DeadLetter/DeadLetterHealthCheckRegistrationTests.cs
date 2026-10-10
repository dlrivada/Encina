using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Testing.Fakes.Stores;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Registration tests proving the dead letter health check coexists with other
/// <see cref="IEncinaHealthCheck"/> implementations in any registration order (#2011).
/// </summary>
public sealed class DeadLetterHealthCheckRegistrationTests
{
    private sealed class BeforeHealthCheck : StubHealthCheck;

    private sealed class AfterHealthCheck : StubHealthCheck;

    private abstract class StubHealthCheck : IEncinaHealthCheck
    {
        public string Name => GetType().Name;

        public IReadOnlyCollection<string> Tags => [];

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(HealthCheckResult.Healthy());
    }

    private sealed class StubFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => Substitute.For<IDeadLetterMessage>();
    }

    private static ServiceProvider Build(IServiceCollection services)
        => services.AddLogging().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [Fact]
    public void AddEncinaDeadLetterQueue_WithProviderHealthCheckRegisteredBeforeAndAfter_ResolvesAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IEncinaHealthCheck, BeforeHealthCheck>();
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>();
        services.AddScoped<IEncinaHealthCheck, AfterHealthCheck>();

        // Act
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        var checks = scope.ServiceProvider.GetServices<IEncinaHealthCheck>().ToList();

        // Assert
        checks.Count.ShouldBe(3);
        checks.OfType<BeforeHealthCheck>().ShouldHaveSingleItem();
        checks.OfType<AfterHealthCheck>().ShouldHaveSingleItem();
        checks.OfType<DeadLetterHealthCheck>().ShouldHaveSingleItem();
    }

    [Fact]
    public void AddEncinaDeadLetterQueue_WithHealthCheckOptions_RegistersOptionsUsedByHealthCheck()
    {
        // Arrange
        var healthOptions = new DeadLetterHealthCheckOptions { PendingMessageWarningThreshold = 3 };
        var services = new ServiceCollection();
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>(null, healthOptions);

        // Act
        using var provider = Build(services);
        using var scope = provider.CreateScope();

        // Assert
        scope.ServiceProvider.GetRequiredService<DeadLetterHealthCheckOptions>().ShouldBeSameAs(healthOptions);
    }

    [Fact]
    public void AddEncinaDeadLetterQueue_WithNullHealthCheckOptions_RegistersTheDefaultOptionsOnce()
    {
        var services = new ServiceCollection();
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>(null, null);

        // The default options are always registered, so the health check resolves under ValidateOnBuild.
        services.Count(d => d.ServiceType == typeof(DeadLetterHealthCheckOptions)).ShouldBe(1);
    }

    [Fact]
    public void AddEncinaDeadLetterQueue_WithCleanupEnabled_RegistersCleanupProcessor()
    {
        var services = new ServiceCollection();
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>(o =>
        {
            o.EnableAutomaticCleanup = true;
            o.RetentionPeriod = TimeSpan.FromDays(1);
        });

        services.Any(d => d.ImplementationType == typeof(DeadLetterCleanupProcessor)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AddEncinaDeadLetterQueue_WithCleanupDisabledOrNoRetention_DoesNotRegisterCleanupProcessor(
        bool enabled, bool hasRetention)
    {
        var services = new ServiceCollection();
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>(o =>
        {
            o.EnableAutomaticCleanup = enabled;
            o.RetentionPeriod = hasRetention ? TimeSpan.FromDays(1) : null;
        });

        services.Any(d => d.ImplementationType == typeof(DeadLetterCleanupProcessor)).ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaDeadLetterQueue_CalledTwice_RegistersHealthCheckOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>();
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>();

        // Act
        using var provider = Build(services);
        using var scope = provider.CreateScope();

        // Assert
        scope.ServiceProvider.GetServices<IEncinaHealthCheck>().OfType<DeadLetterHealthCheck>()
            .ShouldHaveSingleItem();
    }
}
