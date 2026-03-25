using Encina.Compliance.LawfulBasis.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

public class LawfulBasisHealthCheckTests
{
    [Fact]
    public void DefaultName_ShouldBeEncinaLawfulBasis()
    {
        LawfulBasisHealthCheck.DefaultName.ShouldBe("encina-lawful-basis");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LawfulBasisHealthCheck(null!, NullLogger<LawfulBasisHealthCheck>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var sp = Substitute.For<IServiceProvider>();
        Should.Throw<ArgumentNullException>(() =>
            new LawfulBasisHealthCheck(sp, null!));
    }

    [Fact]
    public async Task CheckHealthAsync_NoService_ReturnsUnhealthy()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var healthCheck = new LawfulBasisHealthCheck(services, NullLogger<LawfulBasisHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("ILawfulBasisService is not registered");
    }

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        var tags = LawfulBasisHealthCheck.Tags.ToList();
        tags.ShouldContain("encina");
        tags.ShouldContain("gdpr");
        tags.ShouldContain("lawful-basis");
    }
}
