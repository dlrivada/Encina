#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Text.Json;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Encina.Security.Secrets;
using Encina.Security.Secrets.AzureKeyVault;
using Shouldly;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.Secrets.AzureKeyVault;

public sealed class AzureKeyVaultSecretProviderTests
{
    private readonly SecretClient _client = Substitute.For<SecretClient>();
    private readonly ILogger<AzureKeyVaultSecretProvider> _logger = Substitute.For<ILogger<AzureKeyVaultSecretProvider>>();
    private readonly AzureKeyVaultSecretProvider _provider;

    public AzureKeyVaultSecretProviderTests()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _provider = new AzureKeyVaultSecretProvider(_client, _logger);
    }

    private static Response<KeyVaultSecret> CreateSecretResponse(string name, string value)
    {
        var secret = SecretModelFactory.KeyVaultSecret(
            new SecretProperties(name),
            value: value);
        return Response.FromValue(secret, Substitute.For<Response>());
    }

    #region GetSecretAsync (string)

    [Fact]
    public async Task GetSecretAsync_ExistingSecret_ReturnsRightWithValue()
    {
        _client.GetSecretAsync("api-key", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("api-key", "secret-value")));

        var result = await _provider.GetSecretAsync("api-key");

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("secret-value"));
    }

    [Fact]
    public async Task GetSecretAsync_NotFound_ReturnsLeftNotFound()
    {
        _client.GetSecretAsync("missing", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(404, "Secret not found"));

        var result = await _provider.GetSecretAsync("missing");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Forbidden_ReturnsLeftAccessDenied()
    {
        _client.GetSecretAsync("restricted", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Forbidden"));

        var result = await _provider.GetSecretAsync("restricted");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Unauthorized_ReturnsLeftAccessDenied()
    {
        _client.GetSecretAsync("restricted", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(401, "Unauthorized"));

        var result = await _provider.GetSecretAsync("restricted");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task GetSecretAsync_ServiceUnavailable_ReturnsLeftProviderUnavailable()
    {
        _client.GetSecretAsync("key", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(503, "Service unavailable"));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public async Task GetSecretAsync_TooManyRequests_ReturnsLeftProviderUnavailable()
    {
        _client.GetSecretAsync("key", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(429, "Too many requests"));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public void GetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync(null!);

        Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public void GetSecretAsync_EmptySecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync("");

        Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public void GetSecretAsync_WhitespaceSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync("   ");

        Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion

    #region GetSecretAsync<T> (typed)

    [Fact]
    public async Task GetSecretAsync_Typed_ValidJson_ReturnsDeserializedObject()
    {
        var json = JsonSerializer.Serialize(new TestConfig { Host = "localhost", Port = 5432 });
        _client.GetSecretAsync("db-config", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("db-config", json)));

        var result = await _provider.GetSecretAsync<TestConfig>("db-config");

        result.IsRight.ShouldBeTrue();
        result.IfRight(v =>
        {
            v.Host.ShouldBe("localhost");
            v.Port.ShouldBe(5432);
        });
    }

    [Fact]
    public async Task GetSecretAsync_Typed_InvalidJson_ReturnsLeftDeserializationFailed()
    {
        _client.GetSecretAsync("bad-json", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("bad-json", "not-valid-json")));

        var result = await _provider.GetSecretAsync<TestConfig>("bad-json");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NullDeserialization_ReturnsLeftDeserializationFailed()
    {
        _client.GetSecretAsync("null-json", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("null-json", "null")));

        var result = await _provider.GetSecretAsync<TestConfig>("null-json");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NotFound_ReturnsLeftNotFound()
    {
        _client.GetSecretAsync("missing-config", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(404, "Not found"));

        var result = await _provider.GetSecretAsync<TestConfig>("missing-config");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public void GetSecretAsync_Typed_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync<TestConfig>(null!);

        Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion

    #region SetSecretAsync

    [Fact]
    public async Task SetSecretAsync_ValidInput_ReturnsRightUnit()
    {
        _client.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("my-secret", "my-value")));

        var result = await _provider.SetSecretAsync("my-secret", "my-value");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SetSecretAsync_Forbidden_ReturnsLeftAccessDenied()
    {
        _client.SetSecretAsync("read-only", "value", Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Forbidden"));

        var result = await _provider.SetSecretAsync("read-only", "value");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task SetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _client.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(500, "Internal server error"));

        var result = await _provider.SetSecretAsync("key", "value");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public void SetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.SetSecretAsync(null!, "value");

        Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public void SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        var act = async () => await _provider.SetSecretAsync("key", null!);

        Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region RotateSecretAsync

    [Fact]
    public async Task RotateSecretAsync_ValidSecret_ReturnsRightUnit()
    {
        _client.GetSecretAsync("rotatable", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("rotatable", "current-value")));
        _client.SetSecretAsync("rotatable", "current-value", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("rotatable", "current-value")));

        var result = await _provider.RotateSecretAsync("rotatable");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RotateSecretAsync_NotFound_ReturnsLeftRotationFailed()
    {
        _client.GetSecretAsync("missing", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(404, "Not found"));

        var result = await _provider.RotateSecretAsync("missing");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_Forbidden_ReturnsLeftRotationFailed()
    {
        _client.GetSecretAsync("restricted", null, null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Forbidden"));

        var result = await _provider.RotateSecretAsync("restricted");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_SetFails_ReturnsLeftRotationFailed()
    {
        _client.GetSecretAsync("key", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("key", "current")));
        _client.SetSecretAsync("key", "current", Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(500, "Write failed"));

        var result = await _provider.RotateSecretAsync("key");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").ShouldBe(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public void RotateSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.RotateSecretAsync(null!);

        Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion

    #region Helpers

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}
