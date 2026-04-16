using Encina.Compliance.DataSubjectRights;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

public class DataSubjectRightsOptionsValidatorTests
{
    private readonly DataSubjectRightsOptionsValidator _sut = new();

    [Fact]
    public void Validate_ValidDefaults_ReturnsSuccess()
    {
        var options = new DataSubjectRightsOptions();

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ZeroDefaultDeadlineDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { DefaultDeadlineDays = 0 };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("DefaultDeadlineDays");
    }

    [Fact]
    public void Validate_NegativeDefaultDeadlineDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { DefaultDeadlineDays = -1 };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("DefaultDeadlineDays");
    }

    [Fact]
    public void Validate_NegativeMaxExtensionDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = -1 };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("MaxExtensionDays");
    }

    [Fact]
    public void Validate_MaxExtensionDaysExceeds60_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 61 };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("MaxExtensionDays");
        result.FailureMessage!.ShouldContain("60");
    }

    [Fact]
    public void Validate_MaxExtensionDays60_ReturnsSuccess()
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

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        Action act = () => _sut.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Validate_MultipleFailures_ReportsAll()
    {
        var options = new DataSubjectRightsOptions
        {
            DefaultDeadlineDays = 0,
            MaxExtensionDays = -1
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.Count().ShouldBeGreaterThanOrEqualTo(2);
    }
}
