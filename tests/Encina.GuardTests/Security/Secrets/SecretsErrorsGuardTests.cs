using Encina.Security.Secrets;
using Shouldly;

namespace Encina.GuardTests.Security.Secrets;

/// <summary>
/// Guard clause tests for <see cref="SecretsErrors"/>.
/// Verifies that factory methods produce errors with the correct codes and that
/// error constants are properly defined.
/// </summary>
public sealed class SecretsErrorsGuardTests
{
    #region Error Code Constants

    [Fact]
    public void NotFoundCode_IsNotEmpty()
    {
        SecretsErrors.NotFoundCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AccessDeniedCode_IsNotEmpty()
    {
        SecretsErrors.AccessDeniedCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RotationFailedCode_IsNotEmpty()
    {
        SecretsErrors.RotationFailedCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CacheFailureCode_IsNotEmpty()
    {
        SecretsErrors.CacheFailureCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DeserializationFailedCode_IsNotEmpty()
    {
        SecretsErrors.DeserializationFailedCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ProviderUnavailableCode_IsNotEmpty()
    {
        SecretsErrors.ProviderUnavailableCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InjectionFailedCode_IsNotEmpty()
    {
        SecretsErrors.InjectionFailedCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FailoverExhaustedCode_IsNotEmpty()
    {
        SecretsErrors.FailoverExhaustedCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CircuitBreakerOpenCode_IsNotEmpty()
    {
        SecretsErrors.CircuitBreakerOpenCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ResilienceTimeoutCode_IsNotEmpty()
    {
        SecretsErrors.ResilienceTimeoutCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AuditFailedCode_IsNotEmpty()
    {
        SecretsErrors.AuditFailedCode.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region Factory Methods Produce Correct Codes

    [Fact]
    public void NotFound_ReturnsErrorWithNotFoundCode()
    {
        var error = SecretsErrors.NotFound("test-secret");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.NotFoundCode);
    }

    [Fact]
    public void AccessDenied_ReturnsErrorWithAccessDeniedCode()
    {
        var error = SecretsErrors.AccessDenied("test-secret");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.AccessDeniedCode);
    }

    [Fact]
    public void RotationFailed_ReturnsErrorWithRotationFailedCode()
    {
        var error = SecretsErrors.RotationFailed("test-secret");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.RotationFailedCode);
    }

    [Fact]
    public void CacheFailure_ReturnsErrorWithCacheFailureCode()
    {
        var error = SecretsErrors.CacheFailure("test-secret");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.CacheFailureCode);
    }

    [Fact]
    public void ProviderUnavailable_ReturnsErrorWithProviderUnavailableCode()
    {
        var error = SecretsErrors.ProviderUnavailable("env");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.ProviderUnavailableCode);
    }

    [Fact]
    public void InjectionFailed_ReturnsErrorWithInjectionFailedCode()
    {
        var error = SecretsErrors.InjectionFailed("my-secret", "MyProp");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.InjectionFailedCode);
    }

    [Fact]
    public void FailoverExhausted_ReturnsErrorWithFailoverExhaustedCode()
    {
        var error = SecretsErrors.FailoverExhausted("test-secret", 3);

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.FailoverExhaustedCode);
    }

    [Fact]
    public void CircuitBreakerOpen_ReturnsErrorWithCircuitBreakerOpenCode()
    {
        var error = SecretsErrors.CircuitBreakerOpen("secrets");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.CircuitBreakerOpenCode);
    }

    [Fact]
    public void ResilienceTimeout_ReturnsErrorWithResilienceTimeoutCode()
    {
        var error = SecretsErrors.ResilienceTimeout("test-secret", TimeSpan.FromSeconds(30));

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.ResilienceTimeoutCode);
    }

    [Fact]
    public void DeserializationFailed_ReturnsErrorWithDeserializationFailedCode()
    {
        var error = SecretsErrors.DeserializationFailed("test-secret", typeof(string));

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.DeserializationFailedCode);
    }

    [Fact]
    public void AuditFailed_ReturnsErrorWithAuditFailedCode()
    {
        var error = SecretsErrors.AuditFailed("test-secret");

        var code = error.GetCode().IfNone(string.Empty);
        code.ShouldBe(SecretsErrors.AuditFailedCode);
    }

    #endregion

    #region All Error Codes Are Unique

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        var codes = new[]
        {
            SecretsErrors.NotFoundCode,
            SecretsErrors.AccessDeniedCode,
            SecretsErrors.RotationFailedCode,
            SecretsErrors.CacheFailureCode,
            SecretsErrors.DeserializationFailedCode,
            SecretsErrors.ProviderUnavailableCode,
            SecretsErrors.InjectionFailedCode,
            SecretsErrors.FailoverExhaustedCode,
            SecretsErrors.CircuitBreakerOpenCode,
            SecretsErrors.ResilienceTimeoutCode,
            SecretsErrors.AuditFailedCode
        };

        codes.Distinct().Count().ShouldBe(codes.Length);
    }

    #endregion
}
