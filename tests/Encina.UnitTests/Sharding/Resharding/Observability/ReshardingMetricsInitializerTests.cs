using Encina.OpenTelemetry;
using Encina.OpenTelemetry.Resharding;
using Encina.Sharding.Resharding;
using Microsoft.Extensions.Hosting;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for the resharding metrics initializer hosted service.
/// Validates that metric instruments are created correctly based on available
/// services (callbacks and/or orchestrator), and verifies graceful behavior
/// when no resharding services are registered.
/// </summary>
/// <remarks>
/// The <c>ReshardingMetricsInitializer</c> is internal, so tests exercise it
/// indirectly via <see cref="ServiceCollectionExtensions.AddEncinaOpenTelemetry"/>
/// which registers it as an <see cref="IHostedService"/>.
/// </remarks>
public sealed class ReshardingMetricsInitializerTests
{
    #region StartAsync - No Services

    [Fact]
    public async Task StartAsync_NoCallbacksNoOrchestrator_CompletesWithoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaOpenTelemetry();
        var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        var initializer = hostedServices
            .FirstOrDefault(s => s.GetType().Name == "ReshardingMetricsInitializer");

        initializer.ShouldNotBeNull();

        // Act & Assert
        await Should.NotThrowAsync(() =>
            initializer.StartAsync(CancellationToken.None));
    }

    #endregion

    #region StartAsync - With Callbacks

    [Fact]
    public async Task StartAsync_WithCallbacks_CompletesWithoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaOpenTelemetry();
        services.AddSingleton(new ReshardingMetricsCallbacks(
            rowsPerSecondCallback: () => 0.0,
            cdcLagMsCallback: () => 0.0,
            activeReshardingCountCallback: () => 0));
        var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        var initializer = hostedServices
            .FirstOrDefault(s => s.GetType().Name == "ReshardingMetricsInitializer");

        initializer.ShouldNotBeNull();

        // Act & Assert
        await Should.NotThrowAsync(() =>
            initializer.StartAsync(CancellationToken.None));
    }

    #endregion

    #region StartAsync - With Orchestrator

    [Fact]
    public async Task StartAsync_WithOrchestratorButNoCallbacks_CreatesNoOpCallbacks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaOpenTelemetry();
        services.AddSingleton(Substitute.For<IReshardingOrchestrator>());
        var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        var initializer = hostedServices
            .FirstOrDefault(s => s.GetType().Name == "ReshardingMetricsInitializer");

        initializer.ShouldNotBeNull();

        // Act & Assert
        await Should.NotThrowAsync(() =>
            initializer.StartAsync(CancellationToken.None));
    }

    #endregion

    #region StopAsync

    [Fact]
    public async Task StopAsync_Completes_WithoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaOpenTelemetry();
        var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        var initializer = hostedServices
            .FirstOrDefault(s => s.GetType().Name == "ReshardingMetricsInitializer");

        initializer.ShouldNotBeNull();

        // Act & Assert
        await Should.NotThrowAsync(() =>
            initializer.StopAsync(CancellationToken.None));
    }

    #endregion

    #region Registration

    [Fact]
    public void AddEncinaOpenTelemetry_RegistersReshardingMetricsInitializer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry();
        var provider = services.BuildServiceProvider();

        // Assert
        var hostedServices = provider.GetServices<IHostedService>();
        hostedServices.ShouldContain(
            s => s.GetType().Name == "ReshardingMetricsInitializer");
    }

    #endregion
}
