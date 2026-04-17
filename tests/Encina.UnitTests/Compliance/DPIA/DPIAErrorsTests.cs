#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAErrors"/>.
/// </summary>
public class DPIAErrorsTests
{
    #region Error Code Constants

    [Fact]
    public void ErrorCodes_ShouldHaveExpectedValues()
    {
        DPIAErrors.AssessmentRequiredCode.ShouldBe("dpia.assessment_required");
        DPIAErrors.AssessmentExpiredCode.ShouldBe("dpia.assessment_expired");
        DPIAErrors.AssessmentRejectedCode.ShouldBe("dpia.assessment_rejected");
        DPIAErrors.PriorConsultationRequiredCode.ShouldBe("dpia.prior_consultation_required");
        DPIAErrors.DPOConsultationRequiredCode.ShouldBe("dpia.dpo_consultation_required");
        DPIAErrors.RiskTooHighCode.ShouldBe("dpia.risk_too_high");
        DPIAErrors.StoreErrorCode.ShouldBe("dpia.store_error");
        DPIAErrors.TemplateNotFoundCode.ShouldBe("dpia.template_not_found");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void AssessmentRequired_ShouldReturnCorrectError()
    {
        var error = DPIAErrors.AssessmentRequired("Ns.TestCommand");

        error.GetEncinaCode().ShouldBe(DPIAErrors.AssessmentRequiredCode);
        error.Message.ShouldContain("Ns.TestCommand");
        error.Message.ShouldContain("Article 35(1)");
    }

    [Fact]
    public void AssessmentExpired_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();
        var expiredAt = DateTimeOffset.UtcNow;

        var error = DPIAErrors.AssessmentExpired(id, "Ns.TestCommand", expiredAt);

        error.GetEncinaCode().ShouldBe(DPIAErrors.AssessmentExpiredCode);
        error.Message.ShouldContain(id.ToString());
        error.Message.ShouldContain("Article 35(11)");
    }

    [Fact]
    public void AssessmentRejected_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.AssessmentRejected(id, "Ns.TestCommand");

        error.GetEncinaCode().ShouldBe(DPIAErrors.AssessmentRejectedCode);
        error.Message.ShouldContain(id.ToString());
    }

    [Fact]
    public void PriorConsultationRequired_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.PriorConsultationRequired(id, "Ns.TestCommand");

        error.GetEncinaCode().ShouldBe(DPIAErrors.PriorConsultationRequiredCode);
        error.Message.ShouldContain("Article 36(1)");
    }

    [Fact]
    public void DPOConsultationRequired_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.DPOConsultationRequired(id);

        error.GetEncinaCode().ShouldBe(DPIAErrors.DPOConsultationRequiredCode);
        error.Message.ShouldContain("Article 35(2)");
    }

    [Fact]
    public void RiskTooHigh_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.RiskTooHigh(id, "Ns.TestCommand", RiskLevel.VeryHigh);

        error.GetEncinaCode().ShouldBe(DPIAErrors.RiskTooHighCode);
        error.Message.ShouldContain("VeryHigh");
    }

    [Fact]
    public void StoreError_ShouldReturnCorrectError()
    {
        var error = DPIAErrors.StoreError("SaveAssessment", "Connection failed");

        error.GetEncinaCode().ShouldBe(DPIAErrors.StoreErrorCode);
        error.Message.ShouldContain("SaveAssessment");
        error.Message.ShouldContain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        var ex = new InvalidOperationException("Test");

        var error = DPIAErrors.StoreError("GetAssessment", "Failed", ex);

        error.GetEncinaCode().ShouldBe(DPIAErrors.StoreErrorCode);
    }

    [Fact]
    public void TemplateNotFound_ShouldReturnCorrectError()
    {
        var error = DPIAErrors.TemplateNotFound("AutomatedDecisionMaking");

        error.GetEncinaCode().ShouldBe(DPIAErrors.TemplateNotFoundCode);
        error.Message.ShouldContain("AutomatedDecisionMaking");
    }

    #endregion
}
