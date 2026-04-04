using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionOptionsValidator"/> verifying deep validation paths.
/// </summary>
public class RetentionOptionsValidatorExtendedGuardTests
{
    private readonly RetentionOptionsValidator _sut = new();

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = _sut.Validate(null, new RetentionOptions());
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ZeroEnforcementInterval_Fails()
    {
        var options = new RetentionOptions { EnforcementInterval = TimeSpan.Zero };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NegativeEnforcementInterval_Fails()
    {
        var options = new RetentionOptions { EnforcementInterval = TimeSpan.FromMinutes(-1) };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NegativeAlertBeforeExpirationDays_Fails()
    {
        var options = new RetentionOptions { AlertBeforeExpirationDays = -1 };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ZeroAlertBeforeExpirationDays_Succeeds()
    {
        var options = new RetentionOptions { AlertBeforeExpirationDays = 0 };
        var result = _sut.Validate(null, options);
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NegativeDefaultRetentionPeriod_Fails()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = TimeSpan.FromDays(-1) };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ZeroDefaultRetentionPeriod_Fails()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = TimeSpan.Zero };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_PositiveDefaultRetentionPeriod_Succeeds()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = TimeSpan.FromDays(365) };
        var result = _sut.Validate(null, options);
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullDefaultRetentionPeriod_Succeeds()
    {
        var options = new RetentionOptions { DefaultRetentionPeriod = null };
        var result = _sut.Validate(null, options);
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_Fails()
    {
        var options = new RetentionOptions { EnforcementMode = (RetentionEnforcementMode)99 };
        var result = _sut.Validate(null, options);
        result.Failed.ShouldBeTrue();
    }
}
