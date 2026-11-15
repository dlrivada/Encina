#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Microsoft.Extensions.Options;
using Shouldly;

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

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DPIAEnforcementMode.Block)]
    [InlineData(DPIAEnforcementMode.Warn)]
    [InlineData(DPIAEnforcementMode.Disabled)]
    public void Validate_AllValidEnforcementModes_ReturnsSuccess(DPIAEnforcementMode mode)
    {
        var options = new DPIAOptions { EnforcementMode = mode };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    #endregion

    #region EnforcementMode Validation

    [Fact]
    public void Validate_UndefinedEnforcementMode_ReturnsFail()
    {
        var options = new DPIAOptions { EnforcementMode = (DPIAEnforcementMode)99 };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("EnforcementMode");
        result.FailureMessage.ShouldContain("99");
    }

    #endregion

    #region DefaultReviewPeriod Validation

    [Fact]
    public void Validate_ZeroReviewPeriod_ReturnsFail()
    {
        var options = new DPIAOptions { DefaultReviewPeriod = TimeSpan.Zero };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("DefaultReviewPeriod");
    }

    [Fact]
    public void Validate_NegativeReviewPeriod_ReturnsFail()
    {
        var options = new DPIAOptions { DefaultReviewPeriod = TimeSpan.FromDays(-1) };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("DefaultReviewPeriod");
    }

    [Fact]
    public void Validate_PositiveReviewPeriod_ReturnsSuccess()
    {
        var options = new DPIAOptions { DefaultReviewPeriod = TimeSpan.FromDays(30) };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    #endregion

    #region ExpirationCheckInterval Validation

    public static TheoryData<TimeSpan> OutOfRangeIntervals => new()
    {
        TimeSpan.Zero,
        TimeSpan.FromMinutes(-5),
        TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond - 1),
        TimeSpan.FromMilliseconds(uint.MaxValue - 1) + TimeSpan.FromMilliseconds(1),
        TimeSpan.FromDays(50),
        TimeSpan.MaxValue
    };

    public static TheoryData<TimeSpan> InRangeIntervals => new()
    {
        TimeSpan.FromMilliseconds(1),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromDays(49),
        TimeSpan.FromMilliseconds(uint.MaxValue - 1)
    };

    [Theory]
    [MemberData(nameof(OutOfRangeIntervals))]
    public void Validate_IntervalOutsidePeriodicTimerRange_ReturnsFail(TimeSpan interval)
    {
        var options = new DPIAOptions { ExpirationCheckInterval = interval };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("ExpirationCheckInterval");
    }

    [Theory]
    [MemberData(nameof(InRangeIntervals))]
    public void Validate_IntervalInsidePeriodicTimerRange_ReturnsSuccess(TimeSpan interval)
    {
        var options = new DPIAOptions { ExpirationCheckInterval = interval };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InRangeIntervals))]
    public void Validate_AcceptedInterval_IsAcceptedByPeriodicTimer(TimeSpan interval)
    {
        // Every interval the validator accepts must also be accepted by the timer the service creates
        _sut.Validate(null, new DPIAOptions { ExpirationCheckInterval = interval }).Succeeded.ShouldBeTrue();

        Should.NotThrow(() =>
        {
            using var timer = new PeriodicTimer(interval);
        });
    }

    #endregion

    #region DPOEmail Validation

    [Fact]
    public void Validate_DPOEmailWithoutAt_ReturnsFail()
    {
        var options = new DPIAOptions { DPOEmail = "invalid-email" };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("DPOEmail");
        result.FailureMessage.ShouldContain("@");
    }

    [Fact]
    public void Validate_DPOEmailWithAt_ReturnsSuccess()
    {
        var options = new DPIAOptions { DPOEmail = "dpo@company.com" };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullDPOEmail_ReturnsSuccess()
    {
        var options = new DPIAOptions { DPOEmail = null };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyDPOEmail_ReturnsSuccess()
    {
        var options = new DPIAOptions { DPOEmail = "" };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
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
            ExpirationCheckInterval = TimeSpan.Zero,
            DPOEmail = "bad-email"
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("EnforcementMode");
        result.FailureMessage.ShouldContain("DefaultReviewPeriod");
        result.FailureMessage.ShouldContain("ExpirationCheckInterval");
        result.FailureMessage.ShouldContain("DPOEmail");
    }

    #endregion

    #region Null Options

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _sut.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    #endregion
}
