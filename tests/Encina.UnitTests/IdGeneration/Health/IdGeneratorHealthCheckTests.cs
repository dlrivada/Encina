using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.IdGeneration.Health;

/// <summary>
/// Unit tests for <see cref="IdGeneratorHealthCheck"/>.
/// </summary>
public sealed class IdGeneratorHealthCheckTests
{
    [Fact]
    public void DefaultName_IsEncinaIdGeneration()
    {
        IdGeneratorHealthCheck.DefaultName.ShouldBe("encina-id-generation");
    }

    [Fact]
    public async Task CheckHealthAsync_NoSnowflakeOptions_ReturnsHealthy()
    {
        var healthCheck = new IdGeneratorHealthCheck();

        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithSnowflakeOptions_IncludesMachineIdInData()
    {
        var snowflakeOptions = new SnowflakeOptions { MachineId = 42 };
        var healthCheck = new IdGeneratorHealthCheck(snowflakeOptions: snowflakeOptions);

        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("snowflake_machine_id");
        result.Data["snowflake_machine_id"].ShouldBe(42L);
    }

    [Fact]
    public async Task CheckHealthAsync_NoClockDrift_ReturnsHealthy()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var healthCheck = new IdGeneratorHealthCheck(timeProvider: timeProvider);

        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesClockDriftData()
    {
        var healthCheck = new IdGeneratorHealthCheck();

        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Data.ShouldContainKey("clock_drift_ms");
        result.Data.ShouldContainKey("last_check_utc");
    }

    [Fact]
    public async Task CheckHealthAsync_CustomOptions_UsesThreshold()
    {
        var options = new IdGeneratorHealthCheckOptions { ClockDriftThresholdMs = 100 };
        var healthCheck = new IdGeneratorHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void Constructor_DefaultOptions_UsesDefault500msThreshold()
    {
        var options = new IdGeneratorHealthCheckOptions();
        options.ClockDriftThresholdMs.ShouldBe(500L);
    }
}
