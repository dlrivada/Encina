using Encina.Compliance.DataResidency.Health;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard clause tests for <see cref="DataResidencyHealthCheck"/> constructor parameters.
/// </summary>
public class DataResidencyHealthCheckGuardTests
{
    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var logger = NullLogger<DataResidencyHealthCheck>.Instance;
        var provider = Substitute.For<IServiceProvider>();

        var sut = new DataResidencyHealthCheck(provider, logger);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var logger = NullLogger<DataResidencyHealthCheck>.Instance;

        var act = () => new DataResidencyHealthCheck(null!, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var provider = Substitute.For<IServiceProvider>();

        var act = () => new DataResidencyHealthCheck(provider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
