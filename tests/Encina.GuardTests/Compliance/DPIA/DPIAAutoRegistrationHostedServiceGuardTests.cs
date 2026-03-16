using System.Reflection;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DPIAAutoRegistrationHostedService"/> to verify null parameter handling.
/// </summary>
public class DPIAAutoRegistrationHostedServiceGuardTests
{
    private readonly IDPIAService _service = Substitute.For<IDPIAService>();
    private readonly IOptions<DPIAOptions> _options = Options.Create(new DPIAOptions());
    private readonly DPIAAutoRegistrationDescriptor _descriptor = new([]);
    private readonly ILogger<DPIAAutoRegistrationHostedService> _logger =
        NullLogger<DPIAAutoRegistrationHostedService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when service is null.
    /// </summary>
    [Fact]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            null!, _options, _descriptor, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("service");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            _service, null!, _descriptor, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when descriptor is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDescriptor_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            _service, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("descriptor");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            _service, _options, _descriptor, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
