using Encina.Compliance.ProcessorAgreements;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorAgreementOptionsValidator"/> to verify null parameter handling.
/// </summary>
public class ProcessorAgreementOptionsValidatorGuardTests
{
    #region Validate Guards

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new ProcessorAgreementOptionsValidator();

        var act = () => sut.Validate(null, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    #endregion
}
