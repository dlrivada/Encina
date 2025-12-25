using Encina.Messaging.Health;
using Shouldly;

namespace Encina.Tests.Health;

public sealed class EncinaHealthCheckTests
{
    [Fact]
    public void Constructor_SetsNameAndTags()
    {
        // Arrange
        var tags = new[] { "ready", "live" };

        // Act
        var check = new TestHealthCheck("test-check", tags);

        // Assert
        check.Name.ShouldBe("test-check");
        check.Tags.ShouldBe(tags);
    }

    [Fact]
    public void Constructor_WithNullTags_UsesEmptyCollection()
    {
        // Act
        var check = new TestHealthCheck("test-check", null);

        // Assert
        check.Tags.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new TestHealthCheck("", null));
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new TestHealthCheck("   ", null));
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsResultFromCore()
    {
        // Arrange
        var expected = HealthCheckResult.Healthy("Test passed");
        var check = new TestHealthCheck("test", null) { ResultToReturn = expected };

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldBe("Test passed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCoreThrows_ReturnsUnhealthy()
    {
        // Arrange
        var exception = new InvalidOperationException("Core failed");
        var check = new TestHealthCheck("test", null) { ExceptionToThrow = exception };

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBe(exception);
        result.Description!.ShouldContain("Core failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ReturnsUnhealthy()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var check = new TestHealthCheck("test", null) { ThrowOnCancellation = true };

        // Act
        var result = await check.CheckHealthAsync(cts.Token);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("cancelled");
    }

    private sealed class TestHealthCheck : EncinaHealthCheck
    {
        public HealthCheckResult ResultToReturn { get; set; } = HealthCheckResult.Healthy();
        public Exception? ExceptionToThrow { get; set; }
        public bool ThrowOnCancellation { get; set; }

        public TestHealthCheck(string name, IReadOnlyCollection<string>? tags)
            : base(name, tags)
        {
        }

        protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
        {
            if (ThrowOnCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(ResultToReturn);
        }
    }
}
