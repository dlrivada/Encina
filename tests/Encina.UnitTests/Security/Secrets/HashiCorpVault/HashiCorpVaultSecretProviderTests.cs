using System.Net;
using System.Text.Json;
using Encina.Security.Secrets;
using Encina.Security.Secrets.HashiCorpVault;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SecretsEngines.KeyValue;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace Encina.UnitTests.Security.Secrets.HashiCorpVault;

public sealed class HashiCorpVaultSecretProviderTests
{
    private readonly IVaultClient _client = Substitute.For<IVaultClient>();
    private readonly IVaultClientV1 _v1 = Substitute.For<IVaultClientV1>();
    private readonly ISecretsEngine _secretsEngine = Substitute.For<ISecretsEngine>();
    private readonly IKeyValueSecretsEngine _kvEngine = Substitute.For<IKeyValueSecretsEngine>();
    private readonly IKeyValueSecretsEngineV2 _kvV2 = Substitute.For<IKeyValueSecretsEngineV2>();
    private readonly ILogger<HashiCorpVaultSecretProvider> _logger = Substitute.For<ILogger<HashiCorpVaultSecretProvider>>();
    private readonly HashiCorpVaultSecretProvider _provider;

    public HashiCorpVaultSecretProviderTests()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        // Wire up the VaultSharp interface chain: client.V1.Secrets.KeyValue.V2
        _client.V1.Returns(_v1);
        _v1.Secrets.Returns(_secretsEngine);
        _secretsEngine.KeyValue.Returns(_kvEngine);
        _kvEngine.V2.Returns(_kvV2);

        var options = new HashiCorpVaultOptions { MountPoint = "secret" };
        _provider = new HashiCorpVaultSecretProvider(_client, options, _logger);
    }

    private static Secret<SecretData> CreateSecretResponse(Dictionary<string, object> data)
    {
        var secretData = new SecretData { Data = data };
        return new Secret<SecretData> { Data = secretData };
    }

    private static Secret<CurrentSecretMetadata> CreateWriteResponse() =>
        new();

    #region GetSecretAsync (string)

    [Fact]
    public async Task GetSecretAsync_ExistingSecret_WithDataKey_ReturnsRightWithValue()
    {
        var secret = CreateSecretResponse(new Dictionary<string, object> { ["data"] = "my-secret-value" });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));

        var result = await _provider.GetSecretAsync("api-key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("my-secret-value"));
    }

    [Fact]
    public async Task GetSecretAsync_ExistingSecret_WithoutDataKey_ReturnsJsonSerialized()
    {
        var secret = CreateSecretResponse(new Dictionary<string, object>
        {
            ["host"] = "localhost",
            ["port"] = 5432
        });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));

        var result = await _provider.GetSecretAsync("db-config");

        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Should().Contain("host");
            v.Should().Contain("localhost");
        });
    }

    [Fact]
    public async Task GetSecretAsync_DataKeyWithNonStringValue_ReturnsJsonSerialized()
    {
        var secret = CreateSecretResponse(new Dictionary<string, object> { ["data"] = 42 });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));

        var result = await _provider.GetSecretAsync("numeric-data");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Contain("data"));
    }

    [Fact]
    public async Task GetSecretAsync_NotFound_ReturnsLeftNotFound()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.NotFound, "Secret not found"));

        var result = await _provider.GetSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Forbidden_ReturnsLeftAccessDenied()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.Forbidden, "Permission denied"));

        var result = await _provider.GetSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task GetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.InternalServerError, "Internal error"));

        var result = await _provider.GetSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.ProviderUnavailableCode));
    }

    [Fact]
    public async Task GetSecretAsync_ServiceUnavailable_ReturnsLeftProviderUnavailable()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.ServiceUnavailable, "Vault sealed"));

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
        var secret = CreateSecretResponse(new Dictionary<string, object> { ["data"] = json });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));

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
        var secret = CreateSecretResponse(new Dictionary<string, object> { ["data"] = "not-valid-json" });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));

        var result = await _provider.GetSecretAsync<TestConfig>("bad-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NullDeserialization_ReturnsLeftDeserializationFailed()
    {
        var secret = CreateSecretResponse(new Dictionary<string, object> { ["data"] = "null" });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));

        var result = await _provider.GetSecretAsync<TestConfig>("null-json");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.DeserializationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NotFound_ReturnsLeftNotFound()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.NotFound, "Not found"));

        var result = await _provider.GetSecretAsync<TestConfig>("missing-config");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_AccessDenied_ReturnsLeftAccessDenied()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.Forbidden, "Forbidden"));

        var result = await _provider.GetSecretAsync<TestConfig>("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
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
    public async Task SetSecretAsync_WriteSucceeds_ReturnsRightUnit()
    {
        _kvV2.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(Task.FromResult(CreateWriteResponse()));

        var result = await _provider.SetSecretAsync("my-secret", "my-value");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task SetSecretAsync_WriteSucceeds_PassesDataDictionary()
    {
        _kvV2.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(Task.FromResult(CreateWriteResponse()));

        await _provider.SetSecretAsync("my-secret", "my-value");

        await _kvV2.Received(1).WriteSecretAsync(
            "my-secret",
            Arg.Is<IDictionary<string, object>>(d => d.ContainsKey("data") && (string)d["data"] == "my-value"),
            Arg.Any<int?>(),
            "secret");
    }

    [Fact]
    public async Task SetSecretAsync_Forbidden_ReturnsLeftAccessDenied()
    {
        _kvV2.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.Forbidden, "Permission denied"));

        var result = await _provider.SetSecretAsync("read-only", "value");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task SetSecretAsync_ServiceError_ReturnsLeftProviderUnavailable()
    {
        _kvV2.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.InternalServerError, "Internal error"));

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
        var secret = CreateSecretResponse(new Dictionary<string, object> { ["data"] = "current-value" });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));
        _kvV2.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(Task.FromResult(CreateWriteResponse()));

        var result = await _provider.RotateSecretAsync("rotatable");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RotateSecretAsync_ReadsAndWritesBack()
    {
        var data = new Dictionary<string, object> { ["data"] = "current-value" };
        var secret = CreateSecretResponse(data);

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));
        _kvV2.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(Task.FromResult(CreateWriteResponse()));

        await _provider.RotateSecretAsync("rotatable");

        await _kvV2.Received(1).ReadSecretAsync("rotatable", Arg.Any<int?>(), "secret", Arg.Any<string>());
        await _kvV2.Received(1).WriteSecretAsync("rotatable", data, Arg.Any<int?>(), "secret");
    }

    [Fact]
    public async Task RotateSecretAsync_ReadNotFound_ReturnsLeftRotationFailed()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.NotFound, "Not found"));

        var result = await _provider.RotateSecretAsync("missing");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_ReadForbidden_ReturnsLeftRotationFailed()
    {
        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.Forbidden, "Permission denied"));

        var result = await _provider.RotateSecretAsync("restricted");

        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfNone("").Should().Be(SecretsErrors.RotationFailedCode));
    }

    [Fact]
    public async Task RotateSecretAsync_WriteFails_ReturnsLeftRotationFailed()
    {
        var secret = CreateSecretResponse(new Dictionary<string, object> { ["data"] = "current" });

        _kvV2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(secret));
        _kvV2.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Throws(new VaultApiException(HttpStatusCode.InternalServerError, "Write failed"));

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
