using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for EF Core health check integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use.</typeparam>
public abstract class HealthCheckEFTestsBase<TFixture, TContext> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
{
    /// <summary>
    /// Creates the health check instance for testing.
    /// </summary>
    protected abstract IHealthCheck CreateHealthCheck(TContext context);

    /// <summary>
    /// Creates a health check context for testing.
    /// </summary>
    protected virtual HealthCheckContext CreateHealthCheckContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "test",
                CreateHealthCheck(CreateDbContext<TContext>()),
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };
    }

    [Fact]
    public async Task HealthCheck_WithHealthyDatabase_ShouldReturnHealthy()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var healthCheck = CreateHealthCheck(context);
        var healthCheckContext = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "test",
                healthCheck,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(healthCheckContext);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_ShouldNotThrowException()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var healthCheck = CreateHealthCheck(context);
        var healthCheckContext = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "test",
                healthCheck,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };

        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await healthCheck.CheckHealthAsync(healthCheckContext);
        });
    }

    [Fact]
    public async Task HealthCheck_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var healthCheck = CreateHealthCheck(context);
        var healthCheckContext = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "test",
                healthCheck,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await healthCheck.CheckHealthAsync(healthCheckContext, cts.Token);
        });
    }

    [Fact]
    public async Task HealthCheck_MultipleSequentialCalls_ShouldAllSucceed()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var healthCheck = CreateHealthCheck(context);
        var healthCheckContext = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "test",
                healthCheck,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };

        // Act
        var results = new List<HealthCheckResult>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(await healthCheck.CheckHealthAsync(healthCheckContext));
        }

        // Assert
        results.ShouldAllBe(r => r.Status == HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_ConcurrentCalls_ShouldAllSucceed()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = CreateDbContext<TContext>();
            var healthCheck = CreateHealthCheck(context);
            var healthCheckContext = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "test",
                    healthCheck,
                    failureStatus: HealthStatus.Unhealthy,
                    tags: null)
            };

            return await healthCheck.CheckHealthAsync(healthCheckContext);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(r => r.Status == HealthStatus.Healthy);
    }
}
