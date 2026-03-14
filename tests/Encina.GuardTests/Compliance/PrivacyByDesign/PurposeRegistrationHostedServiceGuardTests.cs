using Encina.Compliance.PrivacyByDesign;

namespace Encina.GuardTests.Compliance.PrivacyByDesign;

/// <summary>
/// Guard tests for <see cref="PurposeRegistrationHostedService"/> to verify null parameter handling.
/// </summary>
public class PurposeRegistrationHostedServiceGuardTests
{
    private readonly IPurposeRegistry _registry = Substitute.For<IPurposeRegistry>();
    private readonly IOptions<PrivacyByDesignOptions> _options = Options.Create(new PrivacyByDesignOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<PurposeRegistrationHostedService> _logger = NullLogger<PurposeRegistrationHostedService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when registry is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        var act = () => new PurposeRegistrationHostedService(
            null!, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new PurposeRegistrationHostedService(
            _registry, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new PurposeRegistrationHostedService(
            _registry, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new PurposeRegistrationHostedService(
            _registry, _options, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
