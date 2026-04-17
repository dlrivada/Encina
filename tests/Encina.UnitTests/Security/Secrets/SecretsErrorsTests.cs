using Encina.Security.Secrets;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsErrorsTests
{
    #region NotFound

    [Fact]
    public void NotFound_ReturnsErrorWithCorrectCode()
    {
        var error = SecretsErrors.NotFound("my-secret");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.NotFoundCode);
        error.Message.ShouldContain("my-secret");
    }

    [Fact]
    public void NotFound_IncludesSecretNameInDetails()
    {
        var error = SecretsErrors.NotFound("db-conn");

        var details = error.GetDetails();
        details.ShouldContainKey("secretName");
        details["secretName"].ShouldBe("db-conn");
    }

    #endregion

    #region AccessDenied

    [Fact]
    public void AccessDenied_WithReason_IncludesReasonInMessage()
    {
        var error = SecretsErrors.AccessDenied("api-key", "insufficient permissions");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.AccessDeniedCode);
        error.Message.ShouldContain("api-key");
        error.Message.ShouldContain("insufficient permissions");
    }

    [Fact]
    public void AccessDenied_WithoutReason_HasGenericMessage()
    {
        var error = SecretsErrors.AccessDenied("api-key");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.AccessDeniedCode);
        error.Message.ShouldContain("api-key");
    }

    [Fact]
    public void AccessDenied_IncludesReasonInDetails()
    {
        var error = SecretsErrors.AccessDenied("api-key", "forbidden");

        var details = error.GetDetails();
        details.ShouldContainKey("reason");
        details["reason"].ShouldBe("forbidden");
    }

    #endregion

    #region RotationFailed

    [Fact]
    public void RotationFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.RotationFailed("db-password");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.RotationFailedCode);
        error.Message.ShouldContain("db-password");
    }

    [Fact]
    public void RotationFailed_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("rotation error");
        var error = SecretsErrors.RotationFailed("db-password", exception: ex);

        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void RotationFailed_WithReason_IncludesReasonInMessage()
    {
        var error = SecretsErrors.RotationFailed("db-password", "timeout");

        error.Message.ShouldContain("timeout");
    }

    #endregion

    #region CacheFailure

    [Fact]
    public void CacheFailure_ReturnsCorrectCode()
    {
        var error = SecretsErrors.CacheFailure("my-secret");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.CacheFailureCode);
        error.Message.ShouldContain("my-secret");
    }

    [Fact]
    public void CacheFailure_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("cache error");
        var error = SecretsErrors.CacheFailure("my-secret", ex);

        error.Exception.IsSome.ShouldBeTrue();
    }

    #endregion

    #region DeserializationFailed

    [Fact]
    public void DeserializationFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.DeserializationFailed("config", typeof(string));

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.DeserializationFailedCode);
        error.Message.ShouldContain("config");
        error.Message.ShouldContain("String");
    }

    [Fact]
    public void DeserializationFailed_IncludesTargetTypeInDetails()
    {
        var error = SecretsErrors.DeserializationFailed("config", typeof(int));

        var details = error.GetDetails();
        details.ShouldContainKey("targetType");
        details["targetType"].ShouldBe(typeof(int).FullName);
    }

    [Fact]
    public void DeserializationFailed_WithException_PreservesException()
    {
        var ex = new System.Text.Json.JsonException("bad json");
        var error = SecretsErrors.DeserializationFailed("config", typeof(string), ex);

        error.Exception.IsSome.ShouldBeTrue();
    }

    #endregion

    #region ProviderUnavailable

    [Fact]
    public void ProviderUnavailable_ReturnsCorrectCode()
    {
        var error = SecretsErrors.ProviderUnavailable("AzureKeyVault");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.ProviderUnavailableCode);
        error.Message.ShouldContain("AzureKeyVault");
    }

    [Fact]
    public void ProviderUnavailable_IncludesProviderNameInDetails()
    {
        var error = SecretsErrors.ProviderUnavailable("AWSSecretsManager");

        var details = error.GetDetails();
        details.ShouldContainKey("providerName");
        details["providerName"].ShouldBe("AWSSecretsManager");
    }

    #endregion

    #region InjectionFailed

    [Fact]
    public void InjectionFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.InjectionFailed("api-key", "ApiKey");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.InjectionFailedCode);
        error.Message.ShouldContain("api-key");
        error.Message.ShouldContain("ApiKey");
    }

    [Fact]
    public void InjectionFailed_IncludesPropertyNameInDetails()
    {
        var error = SecretsErrors.InjectionFailed("api-key", "ApiKey");

        var details = error.GetDetails();
        details.ShouldContainKey("propertyName");
        details["propertyName"].ShouldBe("ApiKey");
    }

    #endregion

    #region FailoverExhausted

    [Fact]
    public void FailoverExhausted_ReturnsCorrectCode()
    {
        var error = SecretsErrors.FailoverExhausted("my-secret", 3);

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.FailoverExhaustedCode);
        error.Message.ShouldContain("3");
        error.Message.ShouldContain("my-secret");
    }

    [Fact]
    public void FailoverExhausted_IncludesProviderCountInDetails()
    {
        var error = SecretsErrors.FailoverExhausted("my-secret", 5);

        var details = error.GetDetails();
        details.ShouldContainKey("providerCount");
        details["providerCount"].ShouldBe(5);
    }

    #endregion

    #region AuditFailed

    [Fact]
    public void AuditFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.AuditFailed("api-key");

        error.GetCode().IfNone("").ShouldBe(SecretsErrors.AuditFailedCode);
        error.Message.ShouldContain("api-key");
    }

    #endregion

    #region Error Code Conventions

    [Theory]
    [InlineData(SecretsErrors.NotFoundCode)]
    [InlineData(SecretsErrors.AccessDeniedCode)]
    [InlineData(SecretsErrors.RotationFailedCode)]
    [InlineData(SecretsErrors.CacheFailureCode)]
    [InlineData(SecretsErrors.DeserializationFailedCode)]
    [InlineData(SecretsErrors.ProviderUnavailableCode)]
    [InlineData(SecretsErrors.InjectionFailedCode)]
    [InlineData(SecretsErrors.FailoverExhaustedCode)]
    [InlineData(SecretsErrors.AuditFailedCode)]
    public void AllErrorCodes_StartWithSecretsPrefix(string code)
    {
        code.ShouldStartWith("secrets.");
    }

    [Fact]
    public void AllErrors_ContainStageMetadata()
    {
        var errors = new[]
        {
            SecretsErrors.NotFound("s"),
            SecretsErrors.AccessDenied("s"),
            SecretsErrors.RotationFailed("s"),
            SecretsErrors.CacheFailure("s"),
            SecretsErrors.DeserializationFailed("s", typeof(string)),
            SecretsErrors.ProviderUnavailable("p"),
            SecretsErrors.InjectionFailed("s", "p"),
            SecretsErrors.FailoverExhausted("s", 1),
            SecretsErrors.AuditFailed("s")
        };

        foreach (var error in errors)
        {
            var details = error.GetDetails();
            details.ShouldContainKey("stage");
            details["stage"].ShouldBe("secrets");
        }
    }

    #endregion
}
