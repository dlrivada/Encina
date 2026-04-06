using Encina.Compliance.DataSubjectRights.Health;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DataSubjectRightsHealthCheck"/> verifying constructor null checks.
/// </summary>
public class DataSubjectRightsHealthCheckGuardTests
{
    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLoggerFactory.Instance.CreateLogger<DataSubjectRightsHealthCheck>();

        var act = () => new DataSubjectRightsHealthCheck(serviceProvider, logger);

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<DataSubjectRightsHealthCheck>();

        var act = () => new DataSubjectRightsHealthCheck(null!, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        var act = () => new DataSubjectRightsHealthCheck(serviceProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
