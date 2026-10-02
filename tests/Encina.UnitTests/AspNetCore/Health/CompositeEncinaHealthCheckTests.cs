using Encina.AspNetCore.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;
using AspNetHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using EncinaHealthCheckResult = Encina.Messaging.Health.HealthCheckResult;
using EncinaHealthStatus = Encina.Messaging.Health.HealthStatus;

namespace Encina.UnitTests.AspNetCore.Health;

/// <summary>
/// Tests for <see cref="CompositeEncinaHealthCheck"/>.
/// </summary>
public sealed class CompositeEncinaHealthCheckTests
{
    private static readonly HealthCheckContext Context = new();

    [Fact]
    public async Task CheckHealthAsync_WithoutChecks_ReturnsHealthy()
    {
        var sut = new CompositeEncinaHealthCheck([]);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBe(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAllChecksPass_ReturnsHealthy()
    {
        var sut = new CompositeEncinaHealthCheck([CreateCheck("a", EncinaHealthStatus.Healthy)]);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBe(AspNetHealthStatus.Healthy);
        result.Data.ShouldContainKey("a");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenACheckIsDegraded_ReturnsDegraded()
    {
        var sut = new CompositeEncinaHealthCheck(
            [CreateCheck("a", EncinaHealthStatus.Healthy), CreateCheck("b", EncinaHealthStatus.Degraded, "slow")]);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBe(AspNetHealthStatus.Degraded);
        result.Description!.ShouldContain("b: slow");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenACheckIsUnhealthy_ReturnsUnhealthyWithoutAnException()
    {
        var sut = new CompositeEncinaHealthCheck(
            [CreateCheck("a", EncinaHealthStatus.Unhealthy, "down")]);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBe(AspNetHealthStatus.Unhealthy);
        result.Description!.ShouldContain("a: down");
        result.Exception.ShouldBeNull();
    }

    private static IEncinaHealthCheck CreateCheck(
        string name,
        EncinaHealthStatus status,
        string? description = null)
    {
        var check = Substitute.For<IEncinaHealthCheck>();
        check.Name.Returns(name);
        check.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(status, description)));
        return check;
    }
}
