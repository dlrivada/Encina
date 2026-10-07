using Encina.Security.ABAC;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for <see cref="ABACErrors"/> factory methods.
/// Exercises all factory methods to cover line-by-line code in the error creation paths.
/// </summary>
public class ABACErrorsGuardTests
{
    #region Error Code Constants

    [Fact]
    public void ErrorCodes_AreNotNullOrEmpty()
    {
        ABACErrors.AccessDeniedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.IndeterminateCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.PolicyNotFoundCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.PolicySetNotFoundCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.EvaluationFailedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.AttributeResolutionFailedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.InvalidPolicyCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.InvalidPolicySetCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.InvalidConditionCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.DuplicatePolicyCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.DuplicatePolicySetCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.CombiningFailedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.ObligationFailedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.FunctionNotFoundCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.FunctionErrorCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.VariableNotFoundCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.SerializationFailedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.DeserializationFailedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.StoreOperationFailedCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.PersistentStoreNotRegisteredCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.CacheProviderNotRegisteredCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.ConditionNotMetCode.ShouldNotBeNullOrWhiteSpace();
        ABACErrors.ObligationHandlerExceptionCode.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region RequiredPolicyNotFound, ConditionNotMet, ObligationHandlerException (#1634)

    [Fact]
    public void RequiredPolicyNotFound_NullRequestType_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ABACErrors.RequiredPolicyNotFound(null!, "policy-a"))
            .ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void RequiredPolicyNotFound_NullPolicyName_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ABACErrors.RequiredPolicyNotFound(typeof(string), null!))
            .ParamName.ShouldBe("policyName");
    }

    [Fact]
    public void RequiredPolicyNotFound_KeepsTheNameOutOfTheMessage()
    {
        var error = ABACErrors.RequiredPolicyNotFound(typeof(string), "policy-a");

        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.PolicyNotFoundCode);
        error.Message.ShouldNotContain("policy-a");
        error.GetDetails()["policyId"].ShouldBe("policy-a");
    }

    [Fact]
    public void ConditionNotMet_NullRequestType_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ABACErrors.ConditionNotMet(null!, 0))
            .ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void ConditionNotMet_RecordsTheConditionIndex()
    {
        var error = ABACErrors.ConditionNotMet(typeof(string), 2);

        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.ConditionNotMetCode);
        error.GetDetails()["conditionIndex"].ShouldBe(2);
    }

    [Fact]
    public void ObligationHandlerException_NullObligationId_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ABACErrors.ObligationHandlerException(null!, typeof(Exception)))
            .ParamName.ShouldBe("obligationId");
    }

    [Fact]
    public void ObligationHandlerException_NullExceptionType_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ABACErrors.ObligationHandlerException("ob-1", null!))
            .ParamName.ShouldBe("exceptionType");
    }

    [Fact]
    public void ObligationHandlerException_RecordsTheTypeNotAMessage()
    {
        var error = ABACErrors.ObligationHandlerException("ob-1", typeof(InvalidOperationException));

        error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.ObligationHandlerExceptionCode);
        error.GetDetails()["exceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    #endregion

    #region AccessDenied

    [Fact]
    public void AccessDenied_WithPolicyId_ContainsPolicyId()
    {
        var error = ABACErrors.AccessDenied(typeof(string), "finance-policy");
        error.Message.ShouldContain("finance-policy");
        error.Message.ShouldContain("String");
    }

    [Fact]
    public void AccessDenied_WithoutPolicyId_ContainsRequestType()
    {
        var error = ABACErrors.AccessDenied(typeof(string));
        error.Message.ShouldContain("String");
        error.Message.ShouldContain("ABAC policy evaluation");
    }

    #endregion

    #region Indeterminate

    [Fact]
    public void Indeterminate_WithReason_ContainsReason()
    {
        var error = ABACErrors.Indeterminate(typeof(int), "missing attribute");
        error.Message.ShouldContain("missing attribute");
    }

    [Fact]
    public void Indeterminate_WithoutReason_ContainsDefaultMessage()
    {
        var error = ABACErrors.Indeterminate(typeof(int));
        error.Message.ShouldContain("indeterminate result");
    }

    #endregion

    #region PolicyNotFound / PolicySetNotFound

    [Fact]
    public void PolicyNotFound_ContainsPolicyId()
    {
        var error = ABACErrors.PolicyNotFound("my-policy");
        error.Message.ShouldContain("my-policy");
    }

    [Fact]
    public void PolicySetNotFound_ContainsPolicySetId()
    {
        var error = ABACErrors.PolicySetNotFound("my-policy-set");
        error.Message.ShouldContain("my-policy-set");
    }

    #endregion

    #region EvaluationFailed

    [Fact]
    public void EvaluationFailed_RecordsTheExceptionTypeButNeverItsMessage()
    {
        var error = ABACErrors.EvaluationFailed(typeof(string), new InvalidOperationException("boom"));
        error.Message.ShouldNotContain("boom");
        error.Message.ShouldContain("String");
        error.GetDetails()["exceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
    }

    #endregion

    #region AttributeResolutionFailed

    [Fact]
    public void AttributeResolutionFailed_ContainsAttributeInfo()
    {
        var error = ABACErrors.AttributeResolutionFailed("role", AttributeCategory.Subject);
        error.Message.ShouldContain("role");
        error.Message.ShouldContain("Subject");
    }

    #endregion

    #region InvalidPolicy / InvalidPolicySet / InvalidCondition

    [Fact]
    public void InvalidPolicy_ContainsPolicyIdAndReason()
    {
        var error = ABACErrors.InvalidPolicy("p-1", "missing rules");
        error.Message.ShouldContain("p-1");
        error.Message.ShouldContain("missing rules");
    }

    [Fact]
    public void InvalidPolicySet_ContainsPolicySetIdAndReason()
    {
        var error = ABACErrors.InvalidPolicySet("ps-1", "no children");
        error.Message.ShouldContain("ps-1");
        error.Message.ShouldContain("no children");
    }

    [Fact]
    public void InvalidCondition_WithReason_ContainsReason()
    {
        var error = ABACErrors.InvalidCondition("x > 5", "syntax error");
        error.Message.ShouldContain("syntax error");
    }

    [Fact]
    public void InvalidCondition_WithoutReason_ContainsDefaultMessage()
    {
        var error = ABACErrors.InvalidCondition("x > 5");
        error.Message.ShouldContain("could not be parsed");
    }

    #endregion

    #region DuplicatePolicy / DuplicatePolicySet

    [Fact]
    public void DuplicatePolicy_ContainsPolicyId()
    {
        var error = ABACErrors.DuplicatePolicy("dup-policy");
        error.Message.ShouldContain("dup-policy");
    }

    [Fact]
    public void DuplicatePolicySet_ContainsPolicySetId()
    {
        var error = ABACErrors.DuplicatePolicySet("dup-ps");
        error.Message.ShouldContain("dup-ps");
    }

    #endregion

    #region CombiningFailed

    [Fact]
    public void CombiningFailed_WithReason_ContainsReason()
    {
        var error = ABACErrors.CombiningFailed("deny-overrides", "conflict");
        error.Message.ShouldContain("conflict");
    }

    [Fact]
    public void CombiningFailed_WithoutReason_ContainsDefaultMessage()
    {
        var error = ABACErrors.CombiningFailed("deny-overrides");
        error.Message.ShouldContain("indeterminate result");
    }

    #endregion

    #region UnauthenticatedCaller

    [Fact]
    public void UnauthenticatedCaller_NullRequestType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ABACErrors.UnauthenticatedCaller(null!))
            .ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void UnauthenticatedCaller_CarriesTheRequestTypeAndTheAbacGate()
    {
        var error = ABACErrors.UnauthenticatedCaller(typeof(string));
        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        error.GetDetails()["requestType"].ShouldBe(typeof(string).FullName);
        error.GetDetails()["gate"].ShouldBe("abac");
    }

    #endregion

    #region ObligationFailed

    [Fact]
    public void ObligationFailed_WithReason_ContainsReason()
    {
        var error = ABACErrors.ObligationFailed("audit-log", "handler error");
        error.Message.ShouldContain("handler error");
    }

    [Fact]
    public void ObligationFailed_WithoutReason_ContainsDefaultMessage()
    {
        var error = ABACErrors.ObligationFailed("audit-log");
        error.Message.ShouldContain("could not be fulfilled");
    }

    #endregion

    #region FunctionNotFound / FunctionError

    [Fact]
    public void FunctionNotFound_ContainsFunctionId()
    {
        var error = ABACErrors.FunctionNotFound("custom-func");
        error.Message.ShouldContain("custom-func");
    }

    [Fact]
    public void FunctionError_ContainsFunctionIdAndException()
    {
        var error = ABACErrors.FunctionError("string-equal", new ArgumentException("bad arg"));
        error.Message.ShouldContain("string-equal");
        error.Message.ShouldContain("bad arg");
    }

    #endregion

    #region VariableNotFound

    [Fact]
    public void VariableNotFound_ContainsVariableId()
    {
        var error = ABACErrors.VariableNotFound("myVar");
        error.Message.ShouldContain("myVar");
    }

    #endregion

    #region SerializationFailed / DeserializationFailed

    [Fact]
    public void SerializationFailed_ContainsTypeAndReason()
    {
        var error = ABACErrors.SerializationFailed("PolicySet", "circular ref");
        error.Message.ShouldContain("PolicySet");
        error.Message.ShouldContain("circular ref");
    }

    [Fact]
    public void DeserializationFailed_ContainsTypeAndReason()
    {
        var error = ABACErrors.DeserializationFailed("Policy", "invalid JSON");
        error.Message.ShouldContain("Policy");
        error.Message.ShouldContain("invalid JSON");
    }

    #endregion

    #region StoreOperationFailed

    [Fact]
    public void StoreOperationFailed_ContainsOperationAndReason()
    {
        var error = ABACErrors.StoreOperationFailed("SavePolicySetAsync", "connection timeout");
        error.Message.ShouldContain("SavePolicySetAsync");
        error.Message.ShouldContain("connection timeout");
    }

    #endregion

    #region PersistentStoreNotRegistered / CacheProviderNotRegistered

    [Fact]
    public void PersistentStoreNotRegistered_ContainsGuidance()
    {
        var error = ABACErrors.PersistentStoreNotRegistered();
        error.Message.ShouldContain("IPolicyStore");
    }

    [Fact]
    public void CacheProviderNotRegistered_ContainsGuidance()
    {
        var error = ABACErrors.CacheProviderNotRegistered();
        error.Message.ShouldContain("ICacheProvider");
    }

    #endregion
}
