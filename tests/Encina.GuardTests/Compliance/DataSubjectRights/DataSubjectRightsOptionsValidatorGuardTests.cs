using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DataSubjectRightsOptionsValidator"/> verifying validation logic.
/// </summary>
public class DataSubjectRightsOptionsValidatorGuardTests
{
    private readonly DataSubjectRightsOptionsValidator _sut = new();

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Validate_DefaultOptions_ReturnsSuccess()
    {
        var result = _sut.Validate(null, new DataSubjectRightsOptions());
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ZeroDeadlineDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { DefaultDeadlineDays = 0 };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NegativeDeadlineDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { DefaultDeadlineDays = -1 };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NegativeMaxExtensionDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = -1 };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ExcessiveMaxExtensionDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 61 };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MaxExtensionDaysExactly60_ReturnsSuccess()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 60 };
        var result = _sut.Validate(null, options);
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MaxExtensionDaysZero_ReturnsSuccess()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 0 };
        var result = _sut.Validate(null, options);
        result.Succeeded.ShouldBeTrue();
    }
}
