using Encina.Security.Secrets.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;

namespace Encina.GuardTests.Security.Secrets.Health;

/// <summary>
/// Guard tests for <see cref="SecretsHealthCheck"/> including constructor and method-level guards.
/// </summary>
public sealed class SecretsHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new SecretsHealthCheck(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public async Task CheckHealthAsync_NoSecretReader_ReturnsUnhealthy()
    {
        // Arrange - empty service collection (no ISecretReader registered)
        var services = new ServiceCollection().BuildServiceProvider();
        var sut = new SecretsHealthCheck(services);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, HealthStatus.Unhealthy, [])
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        SecretsHealthCheck.DefaultName.ShouldBe("encina-secrets");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        var tags = SecretsHealthCheck.Tags.ToList();
        tags.ShouldContain("encina");
        tags.ShouldContain("secrets");
        tags.ShouldContain("ready");
    }
}
