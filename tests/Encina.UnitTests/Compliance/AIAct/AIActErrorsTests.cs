using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;
using FluentAssertions;

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
        AIActErrors.ProhibitedUseCode.Should().StartWith("aiact.");
        AIActErrors.ComplianceValidationFailedCode.Should().StartWith("aiact.");
        AIActErrors.HumanOversightRequiredCode.Should().StartWith("aiact.");
        AIActErrors.TransparencyRequiredCode.Should().StartWith("aiact.");
        AIActErrors.SystemNotRegisteredCode.Should().StartWith("aiact.");
        AIActErrors.PipelineBlockedCode.Should().StartWith("aiact.");
        AIActErrors.ValidatorErrorCode.Should().StartWith("aiact.");
    }

    // -- ProhibitedUse --

    [Fact]
    public void ProhibitedUse_ShouldContainRequestTypeAndSystemId()
    {
        var error = AIActErrors.ProhibitedUse(
            typeof(string), "test-system", ["Social scoring detected"]);

        error.Message.Should().Contain("String");
        error.Message.Should().Contain("test-system");
        error.Message.Should().Contain("Social scoring detected");
        error.Message.Should().Contain("Art. 5");
    }

    // -- ComplianceValidationFailed --

    [Fact]
    public void ComplianceValidationFailed_ShouldContainViolationsAndRiskLevel()
    {
        var error = AIActErrors.ComplianceValidationFailed(
            typeof(string), "cv-screener", AIRiskLevel.HighRisk, ["Missing Art. 14 oversight"]);

        error.Message.Should().Contain("String");
        error.Message.Should().Contain("cv-screener");
        error.Message.Should().Contain("HighRisk");
        error.Message.Should().Contain("Missing Art. 14 oversight");
    }

    // -- HumanOversightRequired --

    [Fact]
    public void HumanOversightRequired_ShouldContainArt14Reference()
    {
        var error = AIActErrors.HumanOversightRequired(typeof(string), "loan-system");

        error.Message.Should().Contain("String");
        error.Message.Should().Contain("loan-system");
        error.Message.Should().Contain("Art. 14");
    }

    // -- TransparencyRequired --

    [Fact]
    public void TransparencyRequired_ShouldContainArt13And50Reference()
    {
        var error = AIActErrors.TransparencyRequired(typeof(string), "chatbot-v1");

        error.Message.Should().Contain("String");
        error.Message.Should().Contain("chatbot-v1");
        error.Message.Should().Contain("Art. 13");
    }

    // -- SystemNotRegistered --

    [Fact]
    public void SystemNotRegistered_ShouldContainSystemIdAndRequestType()
    {
        var error = AIActErrors.SystemNotRegistered(typeof(string), "unknown-system");

        error.Message.Should().Contain("String");
        error.Message.Should().Contain("unknown-system");
        error.Message.Should().Contain("not registered");
    }

    // -- PipelineBlocked --

    [Fact]
    public void PipelineBlocked_ShouldContainRequestTypeAndReason()
    {
        var error = AIActErrors.PipelineBlocked("MyRequest", "Compliance check failed");

        error.Message.Should().Contain("MyRequest");
        error.Message.Should().Contain("Compliance check failed");
    }

    // -- ValidatorError --

    [Fact]
    public void ValidatorError_ShouldContainInnerErrorMessage()
    {
        var inner = EncinaError.New("Registry unavailable");

        var error = AIActErrors.ValidatorError(typeof(string), inner);

        error.Message.Should().Contain("String");
        error.Message.Should().Contain("Registry unavailable");
    }
}
