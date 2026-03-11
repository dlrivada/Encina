#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAOptionsValidator"/>.
/// </summary>
public class DPIAOptionsValidatorTests
{
    private readonly DPIAOptionsValidator _sut = new();

    #region Valid Options

    [Fact]
    public void Validate_DefaultOptions_ReturnsSuccess()
    {
        var options = new DPIAOptions();

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(DPIAEnforcementMode.Block)]
    [InlineData(DPIAEnforcementMode.Warn)]
    [InlineData(DPIAEnforcementMode.Disabled)]
    public void Validate_AllValidEnforcementModes_ReturnsSuccess(DPIAEnforcementMode mode)
    {
        var options = new DPIAOptions { EnforcementMode = mode };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region EnforcementMode Validation

    [Fact]
    public void Validate_UndefinedEnforcementMode_ReturnsFail()
    {
        var options = new DPIAOptions { EnforcementMode = (DPIAEnforcementMode)99 };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("EnforcementMode");
        result.FailureMessage.Should().Contain("99");
    }

    #endregion

    #region DefaultReviewPeriod Validation

    [Fact]
    public void Validate_ZeroReviewPeriod_ReturnsFail()
    {
        var options = new DPIAOptions { DefaultReviewPeriod = TimeSpan.Zero };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultReviewPeriod");
    }

    [Fact]
    public void Validate_NegativeReviewPeriod_ReturnsFail()
    {
        var options = new DPIAOptions { DefaultReviewPeriod = TimeSpan.FromDays(-1) };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultReviewPeriod");
    }

    [Fact]
    public void Validate_PositiveReviewPeriod_ReturnsSuccess()
    {
        var options = new DPIAOptions { DefaultReviewPeriod = TimeSpan.FromDays(30) };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region ExpirationCheckInterval Validation

    [Fact]
    public void Validate_MonitoringEnabled_ZeroInterval_ReturnsFail()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.Zero
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ExpirationCheckInterval");
    }

    [Fact]
    public void Validate_MonitoringEnabled_NegativeInterval_ReturnsFail()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromMinutes(-5)
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ExpirationCheckInterval");
    }

    [Fact]
    public void Validate_MonitoringDisabled_ZeroInterval_ReturnsSuccess()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = false,
            ExpirationCheckInterval = TimeSpan.Zero
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_MonitoringEnabled_PositiveInterval_ReturnsSuccess()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromMinutes(30)
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region DPOEmail Validation

    [Fact]
    public void Validate_DPOEmailWithoutAt_ReturnsFail()
    {
        var options = new DPIAOptions { DPOEmail = "invalid-email" };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DPOEmail");
        result.FailureMessage.Should().Contain("@");
    }

    [Fact]
    public void Validate_DPOEmailWithAt_ReturnsSuccess()
    {
        var options = new DPIAOptions { DPOEmail = "dpo@company.com" };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullDPOEmail_ReturnsSuccess()
    {
        var options = new DPIAOptions { DPOEmail = null };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyDPOEmail_ReturnsSuccess()
    {
        var options = new DPIAOptions { DPOEmail = "" };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Multiple Failures

    [Fact]
    public void Validate_MultipleFailures_ReportsAll()
    {
        var options = new DPIAOptions
        {
            EnforcementMode = (DPIAEnforcementMode)99,
            DefaultReviewPeriod = TimeSpan.FromDays(-1),
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.Zero,
            DPOEmail = "bad-email"
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("EnforcementMode");
        result.FailureMessage.Should().Contain("DefaultReviewPeriod");
        result.FailureMessage.Should().Contain("ExpirationCheckInterval");
        result.FailureMessage.Should().Contain("DPOEmail");
    }

    #endregion

    #region Null Options

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion
}
