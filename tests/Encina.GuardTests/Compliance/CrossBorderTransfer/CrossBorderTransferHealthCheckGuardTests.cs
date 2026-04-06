#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Health;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="CrossBorderTransferHealthCheck"/> constructor null checks.
/// </summary>
public class CrossBorderTransferHealthCheckGuardTests
{
    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var provider = Substitute.For<IServiceProvider>();
        var logger = NullLoggerFactory.Instance.CreateLogger<CrossBorderTransferHealthCheck>();

        var sut = new CrossBorderTransferHealthCheck(provider, logger);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<CrossBorderTransferHealthCheck>();

        var act = () => new CrossBorderTransferHealthCheck(null!, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var provider = Substitute.For<IServiceProvider>();

        var act = () => new CrossBorderTransferHealthCheck(provider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
