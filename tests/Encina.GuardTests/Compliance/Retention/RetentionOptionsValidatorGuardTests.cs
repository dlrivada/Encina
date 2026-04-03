using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionOptionsValidator"/> null parameter handling.
/// </summary>
public sealed class RetentionOptionsValidatorGuardTests
{
    private readonly RetentionOptionsValidator _sut = new();

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Validate_NullName_DoesNotThrow()
    {
        var options = new RetentionOptions();

        var act = () => _sut.Validate(null, options);

        Should.NotThrow(act);
    }

    [Fact]
    public void Validate_ValidNameAndOptions_DoesNotThrow()
    {
        var options = new RetentionOptions();

        var act = () => _sut.Validate("test", options);

        Should.NotThrow(act);
    }
}
