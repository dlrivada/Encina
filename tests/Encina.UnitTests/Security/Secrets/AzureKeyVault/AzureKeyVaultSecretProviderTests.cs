#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Text.Json;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Encina.Security.Secrets;
using Encina.Security.Secrets.AzureKeyVault;
using FluentAssertions;
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
        _client.GetSecretAsync("api-key", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("api-key", "secret-value")));

        var result = await _provider.GetSecretAsync("api-key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("secret-value"));
    }

    [Fact]
    public async Task GetSecretAsync_NotFound_ReturnsLeftNotFound()
    {
        _client.GetSecretAsync("missing", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(404, "Secret not found"));

        var result = await _provider.GetSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Forbidden_ReturnsLeftAccessDenied()
    {
        _client.GetSecretAsync("restricted", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Forbidden"));

        var result = await _provider.GetSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Unauthorized_ReturnsLeftAccessDenied()
    {
        _client.GetSecretAsync("restricted", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(401, "Unauthorized"));

        var result = await _provider.GetSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task GetSecretAsync_ServiceUnavailable_ReturnsLeftProviderUnavailable()
    {
        _client.GetSecretAsync("key", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(503, "Service unavailable"));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public async Task GetSecretAsync_TooManyRequests_ReturnsLeftProviderUnavailable()
    {
        _client.GetSecretAsync("key", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(429, "Too many requests"));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public void GetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetSecretAsync_EmptySecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync("");

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetSecretAsync_WhitespaceSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync("   ");

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetSecretAsync<T> (typed)

    [Fact]
    public async Task GetSecretAsync_Typed_ValidJson_ReturnsDeserializedObject()
    {
        var json = JsonSerializer.Serialize(new TestConfig { Host = "localhost", Port = 5432 });
        _client.GetSecretAsync("db-config", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("db-config", json)));

        var result = await _provider.GetSecretAsync<TestConfig>("db-config");

        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Host.Should().Be("localhost");
            v.Port.Should().Be(5432);
        });
    }

    [Fact]
    public async Task GetSecretAsync_Typed_InvalidJson_ReturnsLeftDeserializationFailed()
    {
        _client.GetSecretAsync("bad-json", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("bad-json", "not-valid-json")));

        var result = await _provider.GetSecretAsync<TestConfig>("bad-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NullDeserialization_ReturnsLeftDeserializationFailed()
    {
        _client.GetSecretAsync("null-json", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("null-json", "null")));

        var result = await _provider.GetSecretAsync<TestConfig>("null-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NotFound_ReturnsLeftNotFound()
    {
        _client.GetSecretAsync("missing-config", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(404, "Not found"));

        var result = await _provider.GetSecretAsync<TestConfig>("missing-config");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public void GetSecretAsync_Typed_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.GetSecretAsync<TestConfig>(null!);

        act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region SetSecretAsync

    [Fact]
    public async Task SetSecretAsync_ValidInput_ReturnsRightUnit()
    {
        _client.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("my-secret", "my-value")));

        var result = await _provider.SetSecretAsync("my-secret", "my-value");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task SetSecretAsync_Forbidden_ReturnsLeftAccessDenied()
    {
        _client.SetSecretAsync("read-only", "value", Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Forbidden"));

        var result = await _provider.SetSecretAsync("read-only", "value");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task SetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _client.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(500, "Internal server error"));

        var result = await _provider.SetSecretAsync("key", "value");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public void SetSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.SetSecretAsync(null!, "value");

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        var act = async () => await _provider.SetSecretAsync("key", null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region RotateSecretAsync

    [Fact]
    public async Task RotateSecretAsync_ValidSecret_ReturnsRightUnit()
    {
        _client.GetSecretAsync("rotatable", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("rotatable", "current-value")));
        _client.SetSecretAsync("rotatable", "current-value", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("rotatable", "current-value")));

        var result = await _provider.RotateSecretAsync("rotatable");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RotateSecretAsync_NotFound_ReturnsLeftRotationFailed()
    {
        _client.GetSecretAsync("missing", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(404, "Not found"));

        var result = await _provider.RotateSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_Forbidden_ReturnsLeftRotationFailed()
    {
        _client.GetSecretAsync("restricted", null, Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(403, "Forbidden"));

        var result = await _provider.RotateSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_SetFails_ReturnsLeftRotationFailed()
    {
        _client.GetSecretAsync("key", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateSecretResponse("key", "current")));
        _client.SetSecretAsync("key", "current", Arg.Any<CancellationToken>())
            .Throws(new RequestFailedException(500, "Write failed"));

        var result = await _provider.RotateSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public void RotateSecretAsync_NullSecretName_ThrowsArgumentException()
    {
        var act = async () => await _provider.RotateSecretAsync(null!);

        act.Should().ThrowAsync<ArgumentException>();
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
