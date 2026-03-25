using Encina.Compliance.DataSubjectRights;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

public class DataSubjectRightsOptionsValidatorTests
{
    private readonly DataSubjectRightsOptionsValidator _sut = new();

    [Fact]
    public void Validate_ValidDefaults_ReturnsSuccess()
    {
        var options = new DataSubjectRightsOptions();

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ZeroDefaultDeadlineDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { DefaultDeadlineDays = 0 };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultDeadlineDays");
    }

    [Fact]
    public void Validate_NegativeDefaultDeadlineDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { DefaultDeadlineDays = -1 };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultDeadlineDays");
    }

    [Fact]
    public void Validate_NegativeMaxExtensionDays_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = -1 };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxExtensionDays");
    }

    [Fact]
    public void Validate_MaxExtensionDaysExceeds60_ReturnsFail()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 61 };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxExtensionDays");
        result.FailureMessage.Should().Contain("60");
    }

    [Fact]
    public void Validate_MaxExtensionDays60_ReturnsSuccess()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 60 };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_MaxExtensionDaysZero_ReturnsSuccess()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 0 };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);

        act.Should().Throw<ArgumentNullException>();
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

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
