using Encina.Compliance.DPIA;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DPIAOptionsValidator"/> to verify null parameter handling.
/// </summary>
public class DPIAOptionsValidatorGuardTests
{
    #region Validate Guards

    /// <summary>
    /// Verifies that Validate throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new DPIAOptionsValidator();

        var act = () => sut.Validate(null, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    #endregion
}
