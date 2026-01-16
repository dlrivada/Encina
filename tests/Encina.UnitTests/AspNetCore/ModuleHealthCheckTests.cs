using Encina.Testing;
using Encina.AspNetCore;
using Encina.AspNetCore.Health;
using Encina.AspNetCore.Modules;
using Encina.Messaging.Health;
using Encina.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using AspNetHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using EncinaHealthCheckResult = Encina.Messaging.Health.HealthCheckResult;

namespace Encina.UnitTests.AspNetCore;

public class ModuleHealthCheckTests
{
    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    [Fact]
    public async Task AddEncinaModuleHealthChecks_WhenNoModulesRegistered_ReturnsHealthy()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(AspNetHealthStatus.Healthy);
        report.Entries.ShouldContainKey("encina-modules");
        report.Entries["encina-modules"].Status.ShouldBe(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public async Task AddEncinaModuleHealthChecks_WhenModulesWithHealthChecks_ReturnsAggregatedResult()
    {
        // Arrange
        var services = CreateServices();

        services.AddEncinaModules(config =>
        {
            config.AddModule(new TestModuleWithHealthChecks("TestModule", [
                new TestHealthCheck("test-check-1", AspNetHealthStatus.Healthy),
                new TestHealthCheck("test-check-2", AspNetHealthStatus.Healthy)
            ]));
        });

        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(AspNetHealthStatus.Healthy);
        report.Entries.ShouldContainKey("encina-modules");
        report.Entries["encina-modules"].Status.ShouldBe(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public async Task AddEncinaModuleHealthChecks_WhenModuleHasUnhealthyCheck_ReturnsUnhealthy()
    {
        // Arrange
        var services = CreateServices();

        services.AddEncinaModules(config =>
        {
            config.AddModule(new TestModuleWithHealthChecks("TestModule", [
                new TestHealthCheck("healthy-check", AspNetHealthStatus.Healthy),
                new TestHealthCheck("unhealthy-check", AspNetHealthStatus.Unhealthy)
            ]));
        });

        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(AspNetHealthStatus.Unhealthy);
        report.Entries["encina-modules"].Status.ShouldBe(AspNetHealthStatus.Unhealthy);
    }

    [Fact]
    public async Task AddEncinaModuleHealthChecks_WhenModuleHasDegradedCheck_ReturnsDegraded()
    {
        // Arrange
        var services = CreateServices();

        services.AddEncinaModules(config =>
        {
            config.AddModule(new TestModuleWithHealthChecks("TestModule", [
                new TestHealthCheck("healthy-check", AspNetHealthStatus.Healthy),
                new TestHealthCheck("degraded-check", AspNetHealthStatus.Degraded)
            ]));
        });

        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(AspNetHealthStatus.Degraded);
        report.Entries["encina-modules"].Status.ShouldBe(AspNetHealthStatus.Degraded);
    }

    [Fact]
    public async Task AddEncinaModuleHealthChecks_WhenMultipleModules_AggregatesAllChecks()
    {
        // Arrange
        var services = CreateServices();

        services.AddEncinaModules(config =>
        {
            config.AddModule(new TestModuleWithHealthChecks("Orders", [
                new TestHealthCheck("orders-db", AspNetHealthStatus.Healthy)
            ]));
            config.AddModule(new TestModuleWithHealthChecks("Payments", [
                new TestHealthCheck("payments-gateway", AspNetHealthStatus.Healthy)
            ]));
        });

        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_HasCorrectTags()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckRegistrations = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Act
        var registration = healthCheckRegistrations.Registrations.First(r => r.Name == "encina-modules");

        // Assert
        registration.Tags.ShouldContain("encina");
        registration.Tags.ShouldContain("ready");
        registration.Tags.ShouldContain("modules");
    }

    [Fact]
    public async Task AddEncinaModuleHealthChecks_Generic_WhenModuleNotRegistered_ReturnsHealthy()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks<TestModuleWithHealthChecks>();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public async Task AddEncinaModuleHealthChecks_Generic_WhenModuleRegistered_ChecksModuleHealth()
    {
        // Arrange
        var services = CreateServices();
        var module = new TestModuleWithHealthChecks("Test", [
            new TestHealthCheck("test-check", AspNetHealthStatus.Healthy)
        ]);

        services.AddSingleton(module);
        services.AddHealthChecks()
            .AddEncinaModuleHealthChecks<TestModuleWithHealthChecks>();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public void AddEncinaModuleHealthChecks_ShouldNotThrow_WhenBuilderIsNull()
    {
        // Arrange
        IHealthChecksBuilder? builder = null;

        // Act
        var act = () => builder!.AddEncinaModuleHealthChecks();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("builder");
    }

    private sealed class TestModuleWithHealthChecks : IModuleWithHealthChecks
    {
        private readonly string _name;
        private readonly IEncinaHealthCheck[] _healthChecks;

        public TestModuleWithHealthChecks(string name, IEncinaHealthCheck[] healthChecks)
        {
            _name = name;
            _healthChecks = healthChecks;
        }

        public string Name => _name;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public IEnumerable<IEncinaHealthCheck> GetHealthChecks() => _healthChecks;
    }

    private sealed class TestHealthCheck : IEncinaHealthCheck
    {
        private readonly AspNetHealthStatus _status;

        public TestHealthCheck(string name, AspNetHealthStatus status)
        {
            Name = name;
            _status = status;
        }

        public string Name { get; }
        public IReadOnlyCollection<string> Tags => ["test"];

        public Task<EncinaHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_status switch
            {
                AspNetHealthStatus.Healthy => EncinaHealthCheckResult.Healthy($"{Name} is healthy"),
                AspNetHealthStatus.Degraded => EncinaHealthCheckResult.Degraded($"{Name} is degraded"),
                _ => EncinaHealthCheckResult.Unhealthy($"{Name} is unhealthy")
            });
        }
    }
}
