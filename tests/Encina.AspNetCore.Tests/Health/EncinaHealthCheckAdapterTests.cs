using Encina.AspNetCore.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;
using AspNetHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using EncinaHealthCheckResult = Encina.Messaging.Health.HealthCheckResult;
using EncinaHealthStatus = Encina.Messaging.Health.HealthStatus;

namespace Encina.AspNetCore.Tests.Health;

/// <summary>
/// Tests for the <see cref="EncinaHealthCheckAdapter"/> class.
/// </summary>
public sealed class EncinaHealthCheckAdapterTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                EncinaHealthStatus.Healthy,
                "All systems operational")));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(AspNetHealthStatus.Healthy);
        result.Description.ShouldBe("All systems operational");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDegraded_ReturnsDegraded()
    {
        // Arrange
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                EncinaHealthStatus.Degraded,
                "Performance issues detected")));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(AspNetHealthStatus.Degraded);
        result.Description.ShouldBe("Performance issues detected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                EncinaHealthStatus.Unhealthy,
                "System is down")));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(AspNetHealthStatus.Unhealthy);
        result.Description.ShouldBe("System is down");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDegradedWithException_IncludesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection lost");
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                EncinaHealthStatus.Degraded,
                "Database issues",
                expectedException)));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(AspNetHealthStatus.Degraded);
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnhealthyWithException_IncludesException()
    {
        // Arrange
        var expectedException = new TimeoutException("Connection timeout");
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                EncinaHealthStatus.Unhealthy,
                "Connection failed",
                expectedException)));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(AspNetHealthStatus.Unhealthy);
        result.Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WithData_IncludesDataInResult()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["server"] = "db-primary",
            ["latency_ms"] = 45
        };
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                EncinaHealthStatus.Healthy,
                "Connected",
                data: data)));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data["server"].ShouldBe("db-primary");
        result.Data["latency_ms"].ShouldBe(45);
    }

    [Fact]
    public async Task CheckHealthAsync_WithEmptyData_ReturnsEmptyOrNullData()
    {
        // Arrange
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                EncinaHealthStatus.Healthy,
                "OK")));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        // ASP.NET Core HealthCheckResult always creates a Dictionary, even if null is passed
        // So we verify it's either null or empty
        (result.Data == null || result.Data.Count == 0).ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnknownStatus_ReturnsUnhealthy()
    {
        // Arrange
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(
                (EncinaHealthStatus)999, // Unknown status
                "Unknown")));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        var result = await adapter.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(AspNetHealthStatus.Unhealthy);
        result.Description!.ShouldContain("Unknown health status");
    }

    [Fact]
    public void Constructor_WithNullHealthCheck_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<System.Reflection.TargetInvocationException>(() => CreateAdapter(null!));
        ex.InnerException.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_RespectsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var encinaHealthCheck = Substitute.For<IEncinaHealthCheck>();
        encinaHealthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EncinaHealthCheckResult(EncinaHealthStatus.Healthy, "OK")));

        var adapter = CreateAdapter(encinaHealthCheck);
        var context = CreateHealthCheckContext();

        // Act
        await adapter.CheckHealthAsync(context, cts.Token);

        // Assert
        await encinaHealthCheck.Received(1).CheckHealthAsync(cts.Token);
    }

    private static EncinaHealthCheckAdapter CreateAdapter(IEncinaHealthCheck healthCheck)
    {
        // Use reflection to create instance since class is internal
        var type = typeof(EncinaHealthCheckAdapter);
        var constructor = type.GetConstructor([typeof(IEncinaHealthCheck)])
            ?? throw new InvalidOperationException(
                $"{nameof(EncinaHealthCheckAdapter)} must have a public constructor accepting {nameof(IEncinaHealthCheck)}. " +
                "If the constructor signature changed, update this test helper.");
        return (EncinaHealthCheckAdapter)constructor.Invoke([healthCheck]);
    }

    private static HealthCheckContext CreateHealthCheckContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "test-check",
                new NoOpHealthCheck(),
                null,
                null)
        };
    }

    /// <summary>
    /// Lightweight stub for IHealthCheck used in test context creation.
    /// </summary>
    private sealed class NoOpHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(HealthCheckResult.Healthy());
    }
}
