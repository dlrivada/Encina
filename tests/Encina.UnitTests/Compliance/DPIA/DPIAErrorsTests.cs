#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

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
        DPIAErrors.AssessmentRequiredCode.Should().Be("dpia.assessment_required");
        DPIAErrors.AssessmentExpiredCode.Should().Be("dpia.assessment_expired");
        DPIAErrors.AssessmentRejectedCode.Should().Be("dpia.assessment_rejected");
        DPIAErrors.PriorConsultationRequiredCode.Should().Be("dpia.prior_consultation_required");
        DPIAErrors.DPOConsultationRequiredCode.Should().Be("dpia.dpo_consultation_required");
        DPIAErrors.RiskTooHighCode.Should().Be("dpia.risk_too_high");
        DPIAErrors.StoreErrorCode.Should().Be("dpia.store_error");
        DPIAErrors.TemplateNotFoundCode.Should().Be("dpia.template_not_found");
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void AssessmentRequired_ShouldReturnCorrectError()
    {
        var error = DPIAErrors.AssessmentRequired("Ns.TestCommand");

        error.GetEncinaCode().Should().Be(DPIAErrors.AssessmentRequiredCode);
        error.Message.Should().Contain("Ns.TestCommand");
        error.Message.Should().Contain("Article 35(1)");
    }

    [Fact]
    public void AssessmentExpired_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();
        var expiredAt = DateTimeOffset.UtcNow;

        var error = DPIAErrors.AssessmentExpired(id, "Ns.TestCommand", expiredAt);

        error.GetEncinaCode().Should().Be(DPIAErrors.AssessmentExpiredCode);
        error.Message.Should().Contain(id.ToString());
        error.Message.Should().Contain("Article 35(11)");
    }

    [Fact]
    public void AssessmentRejected_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.AssessmentRejected(id, "Ns.TestCommand");

        error.GetEncinaCode().Should().Be(DPIAErrors.AssessmentRejectedCode);
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void PriorConsultationRequired_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.PriorConsultationRequired(id, "Ns.TestCommand");

        error.GetEncinaCode().Should().Be(DPIAErrors.PriorConsultationRequiredCode);
        error.Message.Should().Contain("Article 36(1)");
    }

    [Fact]
    public void DPOConsultationRequired_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.DPOConsultationRequired(id);

        error.GetEncinaCode().Should().Be(DPIAErrors.DPOConsultationRequiredCode);
        error.Message.Should().Contain("Article 35(2)");
    }

    [Fact]
    public void RiskTooHigh_ShouldReturnCorrectError()
    {
        var id = Guid.NewGuid();

        var error = DPIAErrors.RiskTooHigh(id, "Ns.TestCommand", RiskLevel.VeryHigh);

        error.GetEncinaCode().Should().Be(DPIAErrors.RiskTooHighCode);
        error.Message.Should().Contain("VeryHigh");
    }

    [Fact]
    public void StoreError_ShouldReturnCorrectError()
    {
        var error = DPIAErrors.StoreError("SaveAssessment", "Connection failed");

        error.GetEncinaCode().Should().Be(DPIAErrors.StoreErrorCode);
        error.Message.Should().Contain("SaveAssessment");
        error.Message.Should().Contain("Connection failed");
    }

    [Fact]
    public void StoreError_WithException_ShouldIncludeException()
    {
        var ex = new InvalidOperationException("Test");

        var error = DPIAErrors.StoreError("GetAssessment", "Failed", ex);

        error.GetEncinaCode().Should().Be(DPIAErrors.StoreErrorCode);
    }

    [Fact]
    public void TemplateNotFound_ShouldReturnCorrectError()
    {
        var error = DPIAErrors.TemplateNotFound("AutomatedDecisionMaking");

        error.GetEncinaCode().Should().Be(DPIAErrors.TemplateNotFoundCode);
        error.Message.Should().Contain("AutomatedDecisionMaking");
    }

    #endregion
}
