using Encina.Compliance.Retention;

using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

public class RetentionOptionsValidatorTests
{
    private readonly RetentionOptionsValidator _sut = new();

    [Fact]
    public void Validate_ValidDefaults_ReturnsSuccess()
    {
        var options = new RetentionOptions();

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_ReturnsFail()
    {
        var options = new RetentionOptions { EnforcementMode = (RetentionEnforcementMode)99 };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("EnforcementMode");
    }

    [Fact]
    public void Validate_NegativeEnforcementInterval_ReturnsFail()
    {
        var options = new RetentionOptions { EnforcementInterval = TimeSpan.FromMinutes(-1) };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("EnforcementInterval");
    }

    [Fact]
    public void Validate_ZeroEnforcementInterval_ReturnsFail()
    {
        var options = new RetentionOptions { EnforcementInterval = TimeSpan.Zero };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("EnforcementInterval");
    }

    [Fact]
    public void Validate_NegativeAlertDays_ReturnsFail()
    {
        var options = new RetentionOptions { AlertBeforeExpirationDays = -1 };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("AlertBeforeExpirationDays");
    }

    [Fact]
    public void Validate_NegativeDefaultRetentionPeriod_ReturnsFail()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = TimeSpan.FromDays(-1) };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("DefaultRetentionPeriod");
    }

    [Fact]
    public void Validate_ZeroDefaultRetentionPeriod_ReturnsFail()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = TimeSpan.Zero };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullDefaultRetentionPeriod_ReturnsSuccess()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = null };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_PositiveDefaultRetentionPeriod_ReturnsSuccess()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = TimeSpan.FromDays(365) };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Validate_MultipleFailures_ReportsAll()
    {
        var options = new RetentionOptions
        {
            EnforcementMode = (RetentionEnforcementMode)99,
            EnforcementInterval = TimeSpan.FromMinutes(-5),
            AlertBeforeExpirationDays = -10,
            DefaultRetentionPeriod = TimeSpan.FromDays(-1)
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.Count.ShouldBeGreaterThanOrEqualTo(4);
    }
}
