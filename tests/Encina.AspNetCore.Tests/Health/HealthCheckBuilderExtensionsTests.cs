using Encina.AspNetCore.Health;
using Encina.AspNetCore.Modules;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using AspNetHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using EncinaHealthCheckResult = Encina.Messaging.Health.HealthCheckResult;
using EncinaHealthStatus = Encina.Messaging.Health.HealthStatus;

namespace Encina.AspNetCore.Tests.Health;

/// <summary>
/// Tests for the <see cref="HealthCheckBuilderExtensions"/> class.
/// </summary>
public sealed class HealthCheckBuilderExtensionsTests
{
    [Fact]
    public void AddEncinaHealthChecks_RegistersCompositeHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaHealthChecks();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.ShouldContain(r => r.Name == "encina");
    }

    [Fact]
    public void AddEncinaHealthChecks_WithCustomTags_IncludesAllTags()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        var customTags = new[] { "custom", "live" };

        // Act
        builder.AddEncinaHealthChecks(tags: customTags);

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.First(r => r.Name == "encina");
        registration.Tags.ShouldContain("encina");
        registration.Tags.ShouldContain("ready");
        registration.Tags.ShouldContain("custom");
        registration.Tags.ShouldContain("live");
    }

    [Fact]
    public void AddEncinaHealthChecks_WithFailureStatus_SetsCorrectStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaHealthChecks(failureStatus: AspNetHealthStatus.Degraded);

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.First(r => r.Name == "encina");
        registration.FailureStatus.ShouldBe(AspNetHealthStatus.Degraded);
    }

    [Fact]
    public void AddEncinaHealthChecks_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaHealthChecks());
    }

    [Fact]
    public void AddEncinaOutbox_RegistersOutboxHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var outboxStore = Substitute.For<IOutboxStore>();
        services.AddSingleton(outboxStore);
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaOutbox();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.FirstOrDefault(r => r.Name == "encina-outbox");
        registration.ShouldNotBeNull();
        registration.Tags.ShouldContain("outbox");
        registration.Tags.ShouldContain("database");
        registration.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void AddEncinaOutbox_WithCustomName_UsesCustomName()
    {
        // Arrange
        var services = new ServiceCollection();
        var outboxStore = Substitute.For<IOutboxStore>();
        services.AddSingleton(outboxStore);
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaOutbox(name: "my-outbox-check");

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.ShouldContain(r => r.Name == "my-outbox-check");
    }

    [Fact]
    public void AddEncinaOutbox_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaOutbox());
    }

    [Fact]
    public void AddEncinaInbox_RegistersInboxHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var inboxStore = Substitute.For<IInboxStore>();
        services.AddSingleton(inboxStore);
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaInbox();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.FirstOrDefault(r => r.Name == "encina-inbox");
        registration.ShouldNotBeNull();
        registration.Tags.ShouldContain("inbox");
        registration.Tags.ShouldContain("database");
        registration.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void AddEncinaInbox_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaInbox());
    }

    [Fact]
    public void AddEncinaSaga_RegistersSagaHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var sagaStore = Substitute.For<ISagaStore>();
        services.AddSingleton(sagaStore);
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaSaga();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.FirstOrDefault(r => r.Name == "encina-saga");
        registration.ShouldNotBeNull();
        registration.Tags.ShouldContain("saga");
        registration.Tags.ShouldContain("database");
        registration.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void AddEncinaSaga_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaSaga());
    }

    [Fact]
    public void AddEncinaScheduling_RegistersSchedulingHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var scheduledMessageStore = Substitute.For<IScheduledMessageStore>();
        services.AddSingleton(scheduledMessageStore);
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaScheduling();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.FirstOrDefault(r => r.Name == "encina-scheduling");
        registration.ShouldNotBeNull();
        registration.Tags.ShouldContain("scheduling");
        registration.Tags.ShouldContain("database");
        registration.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void AddEncinaScheduling_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaScheduling());
    }

    [Fact]
    public void AddEncinaHealthCheck_RegistersCustomHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaHealthCheck<TestCustomHealthCheck>("custom-check");

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.FirstOrDefault(r => r.Name == "custom-check");
        registration.ShouldNotBeNull();
        registration.Tags.ShouldContain("encina");
    }

    [Fact]
    public void AddEncinaHealthCheck_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaHealthCheck<TestCustomHealthCheck>("test"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddEncinaHealthCheck_WithNullOrEmptyName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.AddEncinaHealthCheck<TestCustomHealthCheck>(name!));
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_RegistersModuleHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaModuleHealthChecks();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.FirstOrDefault(r => r.Name == "encina-modules");
        registration.ShouldNotBeNull();
        registration.Tags.ShouldContain("modules");
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaModuleHealthChecks());
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_WithModulesRegistered_AggregatesHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        var registry = Substitute.For<IModuleRegistry>();
        var moduleWithHealthChecks = Substitute.For<IModuleWithHealthChecks>();
        var healthCheck = Substitute.For<IEncinaHealthCheck>();
        moduleWithHealthChecks.GetHealthChecks().Returns([healthCheck]);
        var modules = new List<IModule> { moduleWithHealthChecks };
        registry.Modules.Returns(modules);
        services.AddSingleton(registry);
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaModuleHealthChecks();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.First(r => r.Name == "encina-modules");
        var healthCheckInstance = registration.Factory(sp);
        healthCheckInstance.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_WithNoModuleRegistry_ReturnsEmptyHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaModuleHealthChecks();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.First(r => r.Name == "encina-modules");
        var healthCheckInstance = registration.Factory(sp);
        healthCheckInstance.ShouldBeOfType<EmptyHealthCheck>();
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_Generic_RegistersSpecificModule()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaModuleHealthChecks<TestModuleWithHealthChecks>();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.FirstOrDefault(r => r.Name.Contains("module"));
        registration.ShouldNotBeNull();
        registration.Tags.ShouldContain("modules");
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_Generic_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder builder = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddEncinaModuleHealthChecks<TestModuleWithHealthChecks>());
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_Generic_WhenModuleNotRegistered_ReturnsEmptyHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        builder.AddEncinaModuleHealthChecks<TestModuleWithHealthChecks>();

        // Assert
        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.First(r => r.Name.Contains("module"));
        var healthCheckInstance = registration.Factory(sp);
        healthCheckInstance.ShouldBeOfType<EmptyHealthCheck>();
    }

    [Fact]
    public void AddEncinaOutbox_WithOptions_PassesOptionsToHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var outboxStore = Substitute.For<IOutboxStore>();
        services.AddSingleton(outboxStore);
        var builder = services.AddHealthChecks();
        var options = new OutboxHealthCheckOptions { PendingMessageWarningThreshold = 100 };

        // Act
        builder.AddEncinaOutbox(options: options);

        // Assert
        using var sp = services.BuildServiceProvider();
        var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = healthCheckOptions.Registrations.First(r => r.Name == "encina-outbox");
        registration.ShouldNotBeNull();

        // Verify options are propagated by creating the health check instance
        var healthCheckInstance = registration.Factory(sp);
        var adapter = healthCheckInstance.ShouldBeOfType<EncinaHealthCheckAdapter>();
        var innerHealthCheck = GetInnerHealthCheck<OutboxHealthCheck>(adapter);
        var configuredOptions = GetPrivateField<OutboxHealthCheckOptions>(innerHealthCheck, "_options");
        configuredOptions.PendingMessageWarningThreshold.ShouldBe(100);
    }

    [Fact]
    public async Task AddEncinaSaga_WithOptions_PassesOptionsToHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var sagaStore = Substitute.For<ISagaStore>();
        sagaStore.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<SagaState>>([]));
        sagaStore.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<SagaState>>([]));
        services.AddSingleton(sagaStore);
        var builder = services.AddHealthChecks();
        var options = new SagaHealthCheckOptions { SagaWarningThreshold = 50 };

        // Act
        builder.AddEncinaSaga(options: options);

        // Assert - Verify options by executing the health check and checking result data
        using var sp = services.BuildServiceProvider();
        var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = healthCheckOptions.Registrations.First(r => r.Name == "encina-saga");
        registration.ShouldNotBeNull();

        var healthCheckInstance = registration.Factory(sp);
        var adapter = healthCheckInstance.ShouldBeOfType<EncinaHealthCheckAdapter>();

        // Execute health check and verify options through result data
        var context = new HealthCheckContext { Registration = registration };
        var result = await adapter.CheckHealthAsync(context);
        result.Data["warning_threshold"].ShouldBe(50);
    }

    [Fact]
    public async Task AddEncinaScheduling_WithOptions_PassesOptionsToHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var scheduledMessageStore = Substitute.For<IScheduledMessageStore>();
        scheduledMessageStore.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<ScheduledMessage>>([]));
        services.AddSingleton(scheduledMessageStore);
        var builder = services.AddHealthChecks();
        var options = new SchedulingHealthCheckOptions { OverdueWarningThreshold = 10 };

        // Act
        builder.AddEncinaScheduling(options: options);

        // Assert - Verify options by executing the health check and checking result data
        using var sp = services.BuildServiceProvider();
        var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = healthCheckOptions.Registrations.First(r => r.Name == "encina-scheduling");
        registration.ShouldNotBeNull();

        var healthCheckInstance = registration.Factory(sp);
        var adapter = healthCheckInstance.ShouldBeOfType<EncinaHealthCheckAdapter>();

        // Execute health check and verify options through result data
        var context = new HealthCheckContext { Registration = registration };
        var result = await adapter.CheckHealthAsync(context);
        result.Data["warning_threshold"].ShouldBe(10);
    }

    // Test helpers
    private sealed class TestCustomHealthCheck : IEncinaHealthCheck
    {
        public string Name => "test-custom";

        public IReadOnlyCollection<string> Tags => ["test"];

        public Task<EncinaHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EncinaHealthCheckResult(EncinaHealthStatus.Healthy, "OK"));
        }
    }

    private sealed class TestModuleWithHealthChecks : IModuleWithHealthChecks
    {
        public string Name => "TestModule";

        public void ConfigureServices(IServiceCollection services)
        {
            // No-op for testing
        }

        public IEnumerable<IEncinaHealthCheck> GetHealthChecks()
        {
            return [];
        }
    }
}
