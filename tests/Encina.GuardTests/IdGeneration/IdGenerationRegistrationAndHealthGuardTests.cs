using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.Time.Testing;

namespace Encina.GuardTests.IdGeneration;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions.AddEncinaIdGeneration"/> with invalid
/// strategy configuration, and for <see cref="IdGeneratorHealthCheck"/> construction and drift handling.
/// </summary>
public sealed class IdGenerationRegistrationAndHealthGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddEncinaIdGeneration_ConfigureThrows_PropagatesException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaIdGeneration(_ => throw new InvalidOperationException("boom"));

        Should.Throw<InvalidOperationException>(act).Message.ShouldBe("boom");
    }

    [Fact]
    public void AddEncinaIdGeneration_InvalidSnowflakeOptions_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaIdGeneration(o => o.UseSnowflake(s => s.MachineId = -1));

        Should.Throw<ArgumentException>(act).Message.ShouldContain("MachineId");
    }

    [Fact]
    public void AddEncinaIdGeneration_NoStrategyEnabled_RegistersNothing()
    {
        var services = new ServiceCollection();

        var returned = services.AddEncinaIdGeneration(_ => { });

        returned.ShouldBeSameAs(services);
        services.Count.ShouldBe(0);
    }

    [Fact]
    public void HealthCheck_Constructor_AllArgumentsOmitted_UsesDefaults()
    {
        var healthCheck = new IdGeneratorHealthCheck();

        healthCheck.Name.ShouldBe(IdGeneratorHealthCheck.DefaultName);
        healthCheck.Tags.ShouldContain("id-generation");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task HealthCheck_ClockJumpBeyondThreshold_ReturnsDegraded()
    {
        var time = new FakeTimeProvider(Now);
        var healthCheck = new IdGeneratorHealthCheck(
            new IdGeneratorHealthCheckOptions { ClockDriftThresholdMs = 100 },
            timeProvider: time);

        time.Advance(TimeSpan.FromSeconds(5));
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task HealthCheck_ClockJumpWithinThreshold_ReturnsHealthy()
    {
        var time = new FakeTimeProvider(Now);
        var healthCheck = new IdGeneratorHealthCheck(
            new IdGeneratorHealthCheckOptions { ClockDriftThresholdMs = 100 },
            timeProvider: time);

        time.Advance(TimeSpan.FromMilliseconds(50));
        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_WithSnowflakeOptions_ReportsMaxMachineId()
    {
        var healthCheck = new IdGeneratorHealthCheck(
            snowflakeOptions: new SnowflakeOptions { ShardBits = 10, TimestampBits = 41, SequenceBits = 12 },
            timeProvider: new FakeTimeProvider(Now));

        var result = await healthCheck.CheckHealthAsync(CancellationToken.None);

        result.Data["snowflake_max_machine_id"].ShouldBe(1023L);
    }
}
