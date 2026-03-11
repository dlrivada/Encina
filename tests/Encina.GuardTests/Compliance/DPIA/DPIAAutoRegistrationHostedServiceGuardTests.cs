using System.Reflection;

using Encina.Compliance.DPIA;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DPIAAutoRegistrationHostedService"/> to verify null parameter handling.
/// </summary>
public class DPIAAutoRegistrationHostedServiceGuardTests
{
    private readonly IDPIAStore _store = Substitute.For<IDPIAStore>();
    private readonly IOptions<DPIAOptions> _options = Options.Create(new DPIAOptions());
    private readonly DPIAAutoRegistrationDescriptor _descriptor = new([]);
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DPIAAutoRegistrationHostedService> _logger =
        NullLogger<DPIAAutoRegistrationHostedService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when store is null.
    /// </summary>
    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            null!, _options, _descriptor, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("store");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            _store, null!, _descriptor, _timeProvider, _logger);

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
            _store, _options, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("descriptor");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            _store, _options, _descriptor, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoRegistrationHostedService(
            _store, _options, _descriptor, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
