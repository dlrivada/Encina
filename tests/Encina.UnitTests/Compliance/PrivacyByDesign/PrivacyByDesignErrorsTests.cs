#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;

using Shouldly;

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
        PrivacyByDesignErrors.DataMinimizationViolationCode.ShouldBe("pbd.data_minimization_violation");
        PrivacyByDesignErrors.PurposeLimitationViolationCode.ShouldBe("pbd.purpose_limitation_violation");
        PrivacyByDesignErrors.DefaultPrivacyViolationCode.ShouldBe("pbd.default_privacy_violation");
        PrivacyByDesignErrors.MinimizationScoreBelowThresholdCode.ShouldBe("pbd.minimization_score_below_threshold");
        PrivacyByDesignErrors.PurposeNotFoundCode.ShouldBe("pbd.purpose_not_found");
        PrivacyByDesignErrors.DuplicatePurposeCode.ShouldBe("pbd.duplicate_purpose");
        PrivacyByDesignErrors.PurposeExpiredCode.ShouldBe("pbd.purpose_expired");
        PrivacyByDesignErrors.StoreErrorCode.ShouldBe("pbd.store_error");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void DataMinimizationViolation_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DataMinimizationViolation("Ns.CreateOrderCommand", 3, 0.65);

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.DataMinimizationViolationCode);
        error.Message.ShouldContain("Ns.CreateOrderCommand");
        error.Message.ShouldContain("3");
        error.Message.ShouldContain("0.65");
        error.Message.ShouldContain("Article 5(1)(c)");
    }

    [Fact]
    public void PurposeLimitationViolation_ShouldReturnCorrectError()
    {
        var violatingFields = new List<string> { "CampaignCode", "ReferralSource" };

        var error = PrivacyByDesignErrors.PurposeLimitationViolation(
            "Ns.CreateOrderCommand", "Order Processing", violatingFields);

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.PurposeLimitationViolationCode);
        error.Message.ShouldContain("Ns.CreateOrderCommand");
        error.Message.ShouldContain("Order Processing");
        error.Message.ShouldContain("CampaignCode");
        error.Message.ShouldContain("ReferralSource");
        error.Message.ShouldContain("Article 5(1)(b)");
    }

    [Fact]
    public void DefaultPrivacyViolation_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DefaultPrivacyViolation("Ns.UpdatePreferencesCommand", 2);

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.DefaultPrivacyViolationCode);
        error.Message.ShouldContain("Ns.UpdatePreferencesCommand");
        error.Message.ShouldContain("2");
        error.Message.ShouldContain("Article 25(2)");
    }

    [Fact]
    public void MinimizationScoreBelowThreshold_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.MinimizationScoreBelowThreshold(
            "Ns.CreateOrderCommand", 0.45, 0.70);

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.MinimizationScoreBelowThresholdCode);
        error.Message.ShouldContain("Ns.CreateOrderCommand");
        error.Message.ShouldContain("0.45");
        error.Message.ShouldContain("0.70");
        error.Message.ShouldContain("Article 25(2)");
    }

    [Fact]
    public void PurposeNotFound_WithoutModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.PurposeNotFound("Order Processing");

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.PurposeNotFoundCode);
        error.Message.ShouldContain("Order Processing");
        error.Message.ShouldContain("global scope");
    }

    [Fact]
    public void PurposeNotFound_WithModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.PurposeNotFound("Marketing Analytics", "marketing");

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.PurposeNotFoundCode);
        error.Message.ShouldContain("Marketing Analytics");
        error.Message.ShouldContain("marketing");
    }

    [Fact]
    public void DuplicatePurpose_WithoutModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DuplicatePurpose("Order Processing");

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.DuplicatePurposeCode);
        error.Message.ShouldContain("Order Processing");
        error.Message.ShouldContain("global scope");
    }

    [Fact]
    public void DuplicatePurpose_WithModuleId_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.DuplicatePurpose("Marketing Analytics", "marketing");

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.DuplicatePurposeCode);
        error.Message.ShouldContain("Marketing Analytics");
        error.Message.ShouldContain("marketing");
    }

    [Fact]
    public void PurposeExpired_ShouldReturnCorrectError()
    {
        var expiredAt = DateTimeOffset.UtcNow;

        var error = PrivacyByDesignErrors.PurposeExpired("Order Processing", expiredAt);

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.PurposeExpiredCode);
        error.Message.ShouldContain("Order Processing");
        error.Message.ShouldContain("expired");
    }

    [Fact]
    public void StoreError_ShouldReturnCorrectError()
    {
        var error = PrivacyByDesignErrors.StoreError("RegisterPurpose", "Connection failed");

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.StoreErrorCode);
        error.Message.ShouldContain("RegisterPurpose");
        error.Message.ShouldContain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        var ex = new InvalidOperationException("Test exception");

        var error = PrivacyByDesignErrors.StoreError("GetPurpose", "Failed", ex);

        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.StoreErrorCode);
        error.Message.ShouldContain("GetPurpose");
        error.Message.ShouldContain("Failed");
    }

    #endregion
}
