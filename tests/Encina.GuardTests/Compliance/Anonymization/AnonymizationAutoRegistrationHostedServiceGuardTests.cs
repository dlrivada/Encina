using Encina.Compliance.Anonymization;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="AnonymizationAutoRegistrationHostedService"/> constructor null checks.
/// </summary>
public class AnonymizationAutoRegistrationHostedServiceGuardTests
{
    private readonly AnonymizationAutoRegistrationDescriptor _descriptor = new([]);
    private readonly IOptions<AnonymizationOptions> _options = Options.Create(new AnonymizationOptions());
    private readonly ILogger<AnonymizationAutoRegistrationHostedService> _logger =
        NullLogger<AnonymizationAutoRegistrationHostedService>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullDescriptor_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationAutoRegistrationHostedService(
            null!, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("descriptor");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationAutoRegistrationHostedService(
            _descriptor, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AnonymizationAutoRegistrationHostedService(
            _descriptor, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var sut = new AnonymizationAutoRegistrationHostedService(
            _descriptor, _options, _logger);

        sut.ShouldNotBeNull();
    }

    #endregion
}
