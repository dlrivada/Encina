using Encina.Compliance.DataResidency;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard clause tests for <see cref="DataResidencyOptionsValidator"/>.
/// </summary>
public class DataResidencyOptionsValidatorGuardTests
{
    [Fact]
    public void Validate_NullOptions_ShouldThrow()
    {
        var sut = new DataResidencyOptionsValidator();
        var act = () => sut.Validate(null, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }
}
