using Encina.Security.ABAC;
using Shouldly;
using LanguageExt;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Unit tests for <see cref="ABACErrors"/> factory methods.
/// Verifies all 17 error codes and factory methods produce correct error instances.
/// </summary>
public sealed class ABACErrorsTests
{
    #region Error Code Constants

    [Theory]
    [InlineData(nameof(ABACErrors.AccessDeniedCode), "abac.access_denied")]
    [InlineData(nameof(ABACErrors.IndeterminateCode), "abac.indeterminate")]
    [InlineData(nameof(ABACErrors.PolicyNotFoundCode), "abac.policy_not_found")]
    [InlineData(nameof(ABACErrors.PolicySetNotFoundCode), "abac.policy_set_not_found")]
    [InlineData(nameof(ABACErrors.EvaluationFailedCode), "abac.evaluation_failed")]
    [InlineData(nameof(ABACErrors.AttributeResolutionFailedCode), "abac.attribute_resolution_failed")]
    [InlineData(nameof(ABACErrors.InvalidPolicyCode), "abac.invalid_policy")]
    [InlineData(nameof(ABACErrors.InvalidPolicySetCode), "abac.invalid_policy_set")]
    [InlineData(nameof(ABACErrors.InvalidConditionCode), "abac.invalid_condition")]
    [InlineData(nameof(ABACErrors.DuplicatePolicyCode), "abac.duplicate_policy")]
    [InlineData(nameof(ABACErrors.DuplicatePolicySetCode), "abac.duplicate_policy_set")]
    [InlineData(nameof(ABACErrors.CombiningFailedCode), "abac.combining_failed")]
    [InlineData(nameof(ABACErrors.MissingContextCode), "abac.missing_context")]
    [InlineData(nameof(ABACErrors.ObligationFailedCode), "abac.obligation_failed")]
    [InlineData(nameof(ABACErrors.FunctionNotFoundCode), "abac.function_not_found")]
    [InlineData(nameof(ABACErrors.FunctionErrorCode), "abac.function_error")]
    [InlineData(nameof(ABACErrors.VariableNotFoundCode), "abac.variable_not_found")]
    public void ErrorCode_HasAbacPrefix(string fieldName, string expectedValue)
    {
        var field = typeof(ABACErrors).GetField(fieldName);
        field.ShouldNotBeNull();
        field!.GetValue(null).ShouldBe(expectedValue);
    }

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        var codes = new[]
        {
            ABACErrors.AccessDeniedCode,
            ABACErrors.IndeterminateCode,
            ABACErrors.PolicyNotFoundCode,
            ABACErrors.PolicySetNotFoundCode,
            ABACErrors.EvaluationFailedCode,
            ABACErrors.AttributeResolutionFailedCode,
            ABACErrors.InvalidPolicyCode,
            ABACErrors.InvalidPolicySetCode,
            ABACErrors.InvalidConditionCode,
            ABACErrors.DuplicatePolicyCode,
            ABACErrors.DuplicatePolicySetCode,
            ABACErrors.CombiningFailedCode,
            ABACErrors.MissingContextCode,
            ABACErrors.ObligationFailedCode,
            ABACErrors.FunctionNotFoundCode,
            ABACErrors.FunctionErrorCode,
            ABACErrors.VariableNotFoundCode
        };

        codes.ShouldBeUnique();
    }

    #endregion

    #region Factory Methods — Error Codes in Result

    [Fact]
    public void AccessDenied_ReturnsCorrectCode()
    {
        var error = ABACErrors.AccessDenied(typeof(string));

        error.GetCode().IfNone("").ShouldBe(ABACErrors.AccessDeniedCode);
    }

    [Fact]
    public void AccessDenied_WithPolicyId_IncludesPolicyInMessage()
    {
        var error = ABACErrors.AccessDenied(typeof(string), "test-policy");

        error.Message.ShouldContain("test-policy");
    }

    [Fact]
    public void AccessDenied_WithoutPolicyId_UsesGenericMessage()
    {
        var error = ABACErrors.AccessDenied(typeof(string));

        error.Message.ShouldContain("String");
        error.Message.ShouldContain("ABAC policy evaluation");
    }

    [Fact]
    public void Indeterminate_ReturnsCorrectCode()
    {
        var error = ABACErrors.Indeterminate(typeof(int));

        error.GetCode().IfNone("").ShouldBe(ABACErrors.IndeterminateCode);
    }

    [Fact]
    public void Indeterminate_WithReason_IncludesReasonInMessage()
    {
        var error = ABACErrors.Indeterminate(typeof(int), "missing attribute");

        error.Message.ShouldContain("missing attribute");
    }

    [Fact]
    public void PolicyNotFound_ReturnsCorrectCode()
    {
        var error = ABACErrors.PolicyNotFound("finance-policy");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.PolicyNotFoundCode);
        error.Message.ShouldContain("finance-policy");
    }

    [Fact]
    public void PolicySetNotFound_ReturnsCorrectCode()
    {
        var error = ABACErrors.PolicySetNotFound("org-policies");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.PolicySetNotFoundCode);
        error.Message.ShouldContain("org-policies");
    }

    [Fact]
    public void EvaluationFailed_ReturnsCorrectCode()
    {
        var ex = new InvalidOperationException("test error");
        var error = ABACErrors.EvaluationFailed(typeof(string), ex);

        error.GetCode().IfNone("").ShouldBe(ABACErrors.EvaluationFailedCode);
        error.Message.ShouldContain("test error");
    }

    [Fact]
    public void AttributeResolutionFailed_ReturnsCorrectCode()
    {
        var error = ABACErrors.AttributeResolutionFailed("department", AttributeCategory.Subject);

        error.GetCode().IfNone("").ShouldBe(ABACErrors.AttributeResolutionFailedCode);
        error.Message.ShouldContain("department");
        error.Message.ShouldContain("Subject");
    }

    [Fact]
    public void InvalidPolicy_ReturnsCorrectCode()
    {
        var error = ABACErrors.InvalidPolicy("bad-policy", "no rules defined");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.InvalidPolicyCode);
        error.Message.ShouldContain("bad-policy");
        error.Message.ShouldContain("no rules defined");
    }

    [Fact]
    public void InvalidPolicySet_ReturnsCorrectCode()
    {
        var error = ABACErrors.InvalidPolicySet("bad-set", "empty");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.InvalidPolicySetCode);
        error.Message.ShouldContain("bad-set");
    }

    [Fact]
    public void InvalidCondition_ReturnsCorrectCode()
    {
        var error = ABACErrors.InvalidCondition("user.role ==", "unexpected end of expression");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.InvalidConditionCode);
        error.Message.ShouldContain("invalid");
    }

    [Fact]
    public void InvalidCondition_WithoutReason_UsesGenericMessage()
    {
        var error = ABACErrors.InvalidCondition("bad expression");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.InvalidConditionCode);
        error.Message.ShouldContain("parsed");
    }

    [Fact]
    public void DuplicatePolicy_ReturnsCorrectCode()
    {
        var error = ABACErrors.DuplicatePolicy("dup-policy");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.DuplicatePolicyCode);
        error.Message.ShouldContain("dup-policy");
    }

    [Fact]
    public void DuplicatePolicySet_ReturnsCorrectCode()
    {
        var error = ABACErrors.DuplicatePolicySet("dup-set");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.DuplicatePolicySetCode);
        error.Message.ShouldContain("dup-set");
    }

    [Fact]
    public void CombiningFailed_ReturnsCorrectCode()
    {
        var error = ABACErrors.CombiningFailed("deny-overrides", "conflicting results");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.CombiningFailedCode);
        error.Message.ShouldContain("deny-overrides");
    }

    [Fact]
    public void CombiningFailed_WithoutReason_UsesGenericMessage()
    {
        var error = ABACErrors.CombiningFailed("permit-overrides");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.CombiningFailedCode);
        error.Message.ShouldContain("indeterminate");
    }

    [Fact]
    public void MissingContext_ReturnsCorrectCode()
    {
        var error = ABACErrors.MissingContext(typeof(string));

        error.GetCode().IfNone("").ShouldBe(ABACErrors.MissingContextCode);
        error.Message.ShouldContain("String");
    }

    [Fact]
    public void ObligationFailed_ReturnsCorrectCode()
    {
        var error = ABACErrors.ObligationFailed("log-access", "handler timeout");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.ObligationFailedCode);
        error.Message.ShouldContain("log-access");
        error.Message.ShouldContain("handler timeout");
    }

    [Fact]
    public void ObligationFailed_WithoutReason_UsesXACMLMessage()
    {
        var error = ABACErrors.ObligationFailed("log-access");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.ObligationFailedCode);
        error.Message.ShouldContain("XACML");
    }

    [Fact]
    public void FunctionNotFound_ReturnsCorrectCode()
    {
        var error = ABACErrors.FunctionNotFound("custom:geo-distance");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.FunctionNotFoundCode);
        error.Message.ShouldContain("custom:geo-distance");
    }

    [Fact]
    public void FunctionError_ReturnsCorrectCode()
    {
        var ex = new DivideByZeroException("division by zero");
        var error = ABACErrors.FunctionError("integer-divide", ex);

        error.GetCode().IfNone("").ShouldBe(ABACErrors.FunctionErrorCode);
        error.Message.ShouldContain("integer-divide");
        error.Message.ShouldContain("division by zero");
    }

    [Fact]
    public void VariableNotFound_ReturnsCorrectCode()
    {
        var error = ABACErrors.VariableNotFound("user-role");

        error.GetCode().IfNone("").ShouldBe(ABACErrors.VariableNotFoundCode);
        error.Message.ShouldContain("user-role");
    }

    #endregion
}
