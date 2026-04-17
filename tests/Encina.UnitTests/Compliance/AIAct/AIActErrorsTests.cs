using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="AIActErrors"/>.
/// </summary>
public class AIActErrorsTests
{
    // -- Error codes --

    [Fact]
    public void ErrorCodes_ShouldFollowConvention()
    {
        AIActErrors.ProhibitedUseCode.ShouldStartWith("aiact.");
        AIActErrors.ComplianceValidationFailedCode.ShouldStartWith("aiact.");
        AIActErrors.HumanOversightRequiredCode.ShouldStartWith("aiact.");
        AIActErrors.TransparencyRequiredCode.ShouldStartWith("aiact.");
        AIActErrors.SystemNotRegisteredCode.ShouldStartWith("aiact.");
        AIActErrors.PipelineBlockedCode.ShouldStartWith("aiact.");
        AIActErrors.ValidatorErrorCode.ShouldStartWith("aiact.");
    }

    // -- ProhibitedUse --

    [Fact]
    public void ProhibitedUse_ShouldContainRequestTypeAndSystemId()
    {
        var error = AIActErrors.ProhibitedUse(
            typeof(string), "test-system", ["Social scoring detected"]);

        error.Message.ShouldContain("String");
        error.Message.ShouldContain("test-system");
        error.Message.ShouldContain("Social scoring detected");
        error.Message.ShouldContain("Art. 5");
    }

    // -- ComplianceValidationFailed --

    [Fact]
    public void ComplianceValidationFailed_ShouldContainViolationsAndRiskLevel()
    {
        var error = AIActErrors.ComplianceValidationFailed(
            typeof(string), "cv-screener", AIRiskLevel.HighRisk, ["Missing Art. 14 oversight"]);

        error.Message.ShouldContain("String");
        error.Message.ShouldContain("cv-screener");
        error.Message.ShouldContain("HighRisk");
        error.Message.ShouldContain("Missing Art. 14 oversight");
    }

    // -- HumanOversightRequired --

    [Fact]
    public void HumanOversightRequired_ShouldContainArt14Reference()
    {
        var error = AIActErrors.HumanOversightRequired(typeof(string), "loan-system");

        error.Message.ShouldContain("String");
        error.Message.ShouldContain("loan-system");
        error.Message.ShouldContain("Art. 14");
    }

    // -- TransparencyRequired --

    [Fact]
    public void TransparencyRequired_ShouldContainArt13And50Reference()
    {
        var error = AIActErrors.TransparencyRequired(typeof(string), "chatbot-v1");

        error.Message.ShouldContain("String");
        error.Message.ShouldContain("chatbot-v1");
        error.Message.ShouldContain("Art. 13");
    }

    // -- SystemNotRegistered --

    [Fact]
    public void SystemNotRegistered_ShouldContainSystemIdAndRequestType()
    {
        var error = AIActErrors.SystemNotRegistered(typeof(string), "unknown-system");

        error.Message.ShouldContain("String");
        error.Message.ShouldContain("unknown-system");
        error.Message.ShouldContain("not registered");
    }

    // -- PipelineBlocked --

    [Fact]
    public void PipelineBlocked_ShouldContainRequestTypeAndReason()
    {
        var error = AIActErrors.PipelineBlocked("MyRequest", "Compliance check failed");

        error.Message.ShouldContain("MyRequest");
        error.Message.ShouldContain("Compliance check failed");
    }

    // -- ValidatorError --

    [Fact]
    public void ValidatorError_ShouldContainInnerErrorMessage()
    {
        var inner = EncinaError.New("Registry unavailable");

        var error = AIActErrors.ValidatorError(typeof(string), inner);

        error.Message.ShouldContain("String");
        error.Message.ShouldContain("Registry unavailable");
    }
}
