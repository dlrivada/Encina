using Encina.Security.Secrets;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsErrorsTests
{
    #region NotFound

    [Fact]
    public void NotFound_ReturnsErrorWithCorrectCode()
    {
        var error = SecretsErrors.NotFound("my-secret");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode);
        error.Message.Should().Contain("my-secret");
    }

    [Fact]
    public void NotFound_IncludesSecretNameInDetails()
    {
        var error = SecretsErrors.NotFound("db-conn");

        var details = error.GetDetails();
        details.Should().ContainKey("secretName");
        details["secretName"].Should().Be("db-conn");
    }

    #endregion

    #region AccessDenied

    [Fact]
    public void AccessDenied_WithReason_IncludesReasonInMessage()
    {
        var error = SecretsErrors.AccessDenied("api-key", "insufficient permissions");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode);
        error.Message.Should().Contain("api-key");
        error.Message.Should().Contain("insufficient permissions");
    }

    [Fact]
    public void AccessDenied_WithoutReason_HasGenericMessage()
    {
        var error = SecretsErrors.AccessDenied("api-key");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode);
        error.Message.Should().Contain("api-key");
    }

    [Fact]
    public void AccessDenied_IncludesReasonInDetails()
    {
        var error = SecretsErrors.AccessDenied("api-key", "forbidden");

        var details = error.GetDetails();
        details.Should().ContainKey("reason");
        details["reason"].Should().Be("forbidden");
    }

    #endregion

    #region RotationFailed

    [Fact]
    public void RotationFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.RotationFailed("db-password");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode);
        error.Message.Should().Contain("db-password");
    }

    [Fact]
    public void RotationFailed_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("rotation error");
        var error = SecretsErrors.RotationFailed("db-password", exception: ex);

        error.Exception.IsSome.Should().BeTrue();
    }

    [Fact]
    public void RotationFailed_WithReason_IncludesReasonInMessage()
    {
        var error = SecretsErrors.RotationFailed("db-password", "timeout");

        error.Message.Should().Contain("timeout");
    }

    #endregion

    #region CacheFailure

    [Fact]
    public void CacheFailure_ReturnsCorrectCode()
    {
        var error = SecretsErrors.CacheFailure("my-secret");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.CacheFailureCode);
        error.Message.Should().Contain("my-secret");
    }

    [Fact]
    public void CacheFailure_WithException_PreservesException()
    {
        var ex = new InvalidOperationException("cache error");
        var error = SecretsErrors.CacheFailure("my-secret", ex);

        error.Exception.IsSome.Should().BeTrue();
    }

    #endregion

    #region DeserializationFailed

    [Fact]
    public void DeserializationFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.DeserializationFailed("config", typeof(string));

        error.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode);
        error.Message.Should().Contain("config");
        error.Message.Should().Contain("String");
    }

    [Fact]
    public void DeserializationFailed_IncludesTargetTypeInDetails()
    {
        var error = SecretsErrors.DeserializationFailed("config", typeof(int));

        var details = error.GetDetails();
        details.Should().ContainKey("targetType");
        details["targetType"].Should().Be(typeof(int).FullName);
    }

    [Fact]
    public void DeserializationFailed_WithException_PreservesException()
    {
        var ex = new System.Text.Json.JsonException("bad json");
        var error = SecretsErrors.DeserializationFailed("config", typeof(string), ex);

        error.Exception.IsSome.Should().BeTrue();
    }

    #endregion

    #region ProviderUnavailable

    [Fact]
    public void ProviderUnavailable_ReturnsCorrectCode()
    {
        var error = SecretsErrors.ProviderUnavailable("AzureKeyVault");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode);
        error.Message.Should().Contain("AzureKeyVault");
    }

    [Fact]
    public void ProviderUnavailable_IncludesProviderNameInDetails()
    {
        var error = SecretsErrors.ProviderUnavailable("AWSSecretsManager");

        var details = error.GetDetails();
        details.Should().ContainKey("providerName");
        details["providerName"].Should().Be("AWSSecretsManager");
    }

    #endregion

    #region InjectionFailed

    [Fact]
    public void InjectionFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.InjectionFailed("api-key", "ApiKey");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.InjectionFailedCode);
        error.Message.Should().Contain("api-key");
        error.Message.Should().Contain("ApiKey");
    }

    [Fact]
    public void InjectionFailed_IncludesPropertyNameInDetails()
    {
        var error = SecretsErrors.InjectionFailed("api-key", "ApiKey");

        var details = error.GetDetails();
        details.Should().ContainKey("propertyName");
        details["propertyName"].Should().Be("ApiKey");
    }

    #endregion

    #region FailoverExhausted

    [Fact]
    public void FailoverExhausted_ReturnsCorrectCode()
    {
        var error = SecretsErrors.FailoverExhausted("my-secret", 3);

        error.GetCode().IfNone("").Should().Be(SecretsErrors.FailoverExhaustedCode);
        error.Message.Should().Contain("3");
        error.Message.Should().Contain("my-secret");
    }

    [Fact]
    public void FailoverExhausted_IncludesProviderCountInDetails()
    {
        var error = SecretsErrors.FailoverExhausted("my-secret", 5);

        var details = error.GetDetails();
        details.Should().ContainKey("providerCount");
        details["providerCount"].Should().Be(5);
    }

    #endregion

    #region AuditFailed

    [Fact]
    public void AuditFailed_ReturnsCorrectCode()
    {
        var error = SecretsErrors.AuditFailed("api-key");

        error.GetCode().IfNone("").Should().Be(SecretsErrors.AuditFailedCode);
        error.Message.Should().Contain("api-key");
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
        code.Should().StartWith("secrets.");
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
            details.Should().ContainKey("stage");
            details["stage"].Should().Be("secrets");
        }
    }

    #endregion
}
