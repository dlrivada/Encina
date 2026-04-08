using Encina.Compliance.Anonymization.Health;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="AnonymizationHealthCheck"/> constructor null checks.
/// </summary>
public class AnonymizationHealthCheckGuardTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ILogger<AnonymizationHealthCheck> _logger =
        NullLogger<AnonymizationHealthCheck>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var sut = new AnonymizationHealthCheck(_serviceProvider, _logger);

        sut.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationHealthCheck(null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationHealthCheck(_serviceProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region CheckHealthAsync

    [Fact]
    public async Task CheckHealthAsync_WithMockServiceProvider_ReturnsResult()
    {
        // Arrange: service provider returns a scope but no configured options
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var scopedProvider = Substitute.For<IServiceProvider>();

        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(scopedProvider);

        var sut = new AnonymizationHealthCheck(_serviceProvider, _logger);

        // Act
        var result = await sut.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        // Assert: without configured options it should be Unhealthy
        result.Status.ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
    }

    #endregion
}
