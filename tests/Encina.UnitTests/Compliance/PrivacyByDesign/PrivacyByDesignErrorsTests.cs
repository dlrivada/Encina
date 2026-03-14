#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PrivacyByDesignErrors"/>.
/// </summary>
public class PrivacyByDesignErrorsTests
{
    #region Error Code Constants

    [Fact]
    public void ErrorCodes_ShouldHaveExpectedValues()
    {
        PrivacyByDesignErrors.DataMinimizationViolationCode.Should().Be("pbd.data_minimization_violation");
        PrivacyByDesignErrors.PurposeLimitationViolationCode.Should().Be("pbd.purpose_limitation_violation");
        PrivacyByDesignErrors.DefaultPrivacyViolationCode.Should().Be("pbd.default_privacy_violation");
        PrivacyByDesignErrors.MinimizationScoreBelowThresholdCode.Should().Be("pbd.minimization_score_below_threshold");
        PrivacyByDesignErrors.PurposeNotFoundCode.Should().Be("pbd.purpose_not_found");
        PrivacyByDesignErrors.DuplicatePurposeCode.Should().Be("pbd.duplicate_purpose");
        PrivacyByDesignErrors.PurposeExpiredCode.Should().Be("pbd.purpose_expired");
        PrivacyByDesignErrors.StoreErrorCode.Should().Be("pbd.store_error");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void DataMinimizationViolation_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DataMinimizationViolation("Ns.CreateOrderCommand", 3, 0.65);

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.DataMinimizationViolationCode);
        error.Message.Should().Contain("Ns.CreateOrderCommand");
        error.Message.Should().Contain("3");
        error.Message.Should().Contain("0.65");
        error.Message.Should().Contain("Article 5(1)(c)");
    }

    [Fact]
    public void PurposeLimitationViolation_ShouldReturnCorrectError()
    {
        var violatingFields = new List<string> { "CampaignCode", "ReferralSource" };

        var error = PrivacyByDesignErrors.PurposeLimitationViolation(
            "Ns.CreateOrderCommand", "Order Processing", violatingFields);

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.PurposeLimitationViolationCode);
        error.Message.Should().Contain("Ns.CreateOrderCommand");
        error.Message.Should().Contain("Order Processing");
        error.Message.Should().Contain("CampaignCode");
        error.Message.Should().Contain("ReferralSource");
        error.Message.Should().Contain("Article 5(1)(b)");
    }

    [Fact]
    public void DefaultPrivacyViolation_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DefaultPrivacyViolation("Ns.UpdatePreferencesCommand", 2);

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.DefaultPrivacyViolationCode);
        error.Message.Should().Contain("Ns.UpdatePreferencesCommand");
        error.Message.Should().Contain("2");
        error.Message.Should().Contain("Article 25(2)");
    }

    [Fact]
    public void MinimizationScoreBelowThreshold_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.MinimizationScoreBelowThreshold(
            "Ns.CreateOrderCommand", 0.45, 0.70);

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.MinimizationScoreBelowThresholdCode);
        error.Message.Should().Contain("Ns.CreateOrderCommand");
        error.Message.Should().Contain("0.45");
        error.Message.Should().Contain("0.70");
        error.Message.Should().Contain("Article 25(2)");
    }

    [Fact]
    public void PurposeNotFound_WithoutModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.PurposeNotFound("Order Processing");

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.PurposeNotFoundCode);
        error.Message.Should().Contain("Order Processing");
        error.Message.Should().Contain("global scope");
    }

    [Fact]
    public void PurposeNotFound_WithModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.PurposeNotFound("Marketing Analytics", "marketing");

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.PurposeNotFoundCode);
        error.Message.Should().Contain("Marketing Analytics");
        error.Message.Should().Contain("marketing");
    }

    [Fact]
    public void DuplicatePurpose_WithoutModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DuplicatePurpose("Order Processing");

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.DuplicatePurposeCode);
        error.Message.Should().Contain("Order Processing");
        error.Message.Should().Contain("global scope");
    }

    [Fact]
    public void DuplicatePurpose_WithModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DuplicatePurpose("Marketing Analytics", "marketing");

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.DuplicatePurposeCode);
        error.Message.Should().Contain("Marketing Analytics");
        error.Message.Should().Contain("marketing");
    }

    [Fact]
    public void PurposeExpired_ShouldReturnCorrectError()
    {
        var expiredAt = DateTimeOffset.UtcNow;

        var error = PrivacyByDesignErrors.PurposeExpired("Order Processing", expiredAt);

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.PurposeExpiredCode);
        error.Message.Should().Contain("Order Processing");
        error.Message.Should().Contain("expired");
    }

    [Fact]
    public void StoreError_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.StoreError("RegisterPurpose", "Connection failed");

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.StoreErrorCode);
        error.Message.Should().Contain("RegisterPurpose");
        error.Message.Should().Contain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        var ex = new InvalidOperationException("Test exception");

        var error = PrivacyByDesignErrors.StoreError("GetPurpose", "Failed", ex);

        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.StoreErrorCode);
        error.Message.Should().Contain("GetPurpose");
        error.Message.Should().Contain("Failed");
    }

    #endregion
}
