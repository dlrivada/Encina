using Encina.Secrets;
using Encina.Secrets.HashiCorpVault;
using Encina.TestInfrastructure.Extensions;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace Encina.UnitTests.Secrets.Providers;

/// <summary>
/// Unit tests for <see cref="HashiCorpVaultProvider"/>.
/// Verifies that VaultSharp exceptions are correctly mapped to <see cref="EncinaError"/> values
/// following the Railway Oriented Programming pattern.
/// </summary>
public sealed class HashiCorpVaultProviderTests
{
    private const string SecretName = "my-vault-secret";
    private const string SecretValue = "vault-secret-value";
    private const string MountPoint = "secret";

    private readonly IVaultClient _client;
    private readonly IKeyValueSecretsEngineV2 _kvV2;
    private readonly ILogger<HashiCorpVaultProvider> _logger;
    private readonly HashiCorpVaultProvider _sut;

    public HashiCorpVaultProviderTests()
    {
        _client = Substitute.For<IVaultClient>();
        _kvV2 = Substitute.For<IKeyValueSecretsEngineV2>();
        _logger = Substitute.For<ILogger<HashiCorpVaultProvider>>();

        // Wire the property chain so _client.V1.Secrets.KeyValue.V2 returns our mock
        var v1 = Substitute.For<VaultSharp.V1.IVaultClientV1>();
        var secretsEngine = Substitute.For<VaultSharp.V1.SecretsEngines.ISecretsEngine>();
        var kvEngine = Substitute.For<VaultSharp.V1.SecretsEngines.KeyValue.IKeyValueSecretsEngine>();

        _client.V1.Returns(v1);
        v1.Secrets.Returns(secretsEngine);
        secretsEngine.KeyValue.Returns(kvEngine);
        kvEngine.V2.Returns(_kvV2);

        var options = Options.Create(new HashiCorpVaultOptions { MountPoint = MountPoint });
        _sut = new HashiCorpVaultProvider(_client, options, _logger);
    }

    // ---------------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        var options = Options.Create(new HashiCorpVaultOptions());
        var act = () => new HashiCorpVaultProvider(null!, options, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("client");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new HashiCorpVaultProvider(_client, null!, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = Options.Create(new HashiCorpVaultOptions());
        var act = () => new HashiCorpVaultProvider(_client, options, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    // ---------------------------------------------------------------------------
    // GetSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretAsync_WhenClientSucceeds_ReturnsRightWithSecret()
    {
        // Arrange
        SetupReadSecret(SecretName, SecretValue, version: 1);

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        var retrieved = result.ShouldBeSuccess();
        retrieved.Name.ShouldBe(SecretName);
        retrieved.Value.ShouldBe(SecretValue);
    }

    [Fact]
    public async Task GetSecretAsync_WhenNotFound_ReturnsLeftWithNotFoundCode()
    {
        // Arrange
        SetupReadSecretThrows(System.Net.HttpStatusCode.NotFound, "Secret not found.");

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        SetupReadSecretThrows(System.Net.HttpStatusCode.Forbidden, "Permission denied.");

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretAsync_WhenOtherVaultError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        SetupReadSecretThrows(System.Net.HttpStatusCode.InternalServerError, "Internal server error.");

        // Act
        var result = await _sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // GetSecretVersionAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretVersionAsync_WhenVersionIsValidInteger_ReturnsRightWithSecret()
    {
        // Arrange
        const string version = "3";
        SetupReadSecret(SecretName, SecretValue, version: 3);

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, version);

        // Assert
        var retrieved = result.ShouldBeSuccess();
        retrieved.Name.ShouldBe(SecretName);
        retrieved.Version.ShouldBe("3");
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenVersionIsNotParseable_ReturnsLeftWithVersionNotFoundCode()
    {
        // Arrange - non-integer version string cannot be parsed for KV v2
        const string invalidVersion = "not-a-number";

        // Act - no SDK call needed; the provider short-circuits on parse failure
        var result = await _sut.GetSecretVersionAsync(SecretName, invalidVersion);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.VersionNotFoundCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenNotFound_ReturnsLeftWithVersionNotFoundCode()
    {
        // Arrange
        SetupReadSecretThrows(System.Net.HttpStatusCode.NotFound, "Version not found.");

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, "99");

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.VersionNotFoundCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        SetupReadSecretThrows(System.Net.HttpStatusCode.Forbidden, "Permission denied.");

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, "1");

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task GetSecretVersionAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        SetupReadSecretThrows(System.Net.HttpStatusCode.ServiceUnavailable, "Service unavailable.");

        // Act
        var result = await _sut.GetSecretVersionAsync(SecretName, "2");

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // SetSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SetSecretAsync_WhenClientSucceeds_ReturnsRightWithMetadata()
    {
        // Arrange
        var writeMetadata = new CurrentSecretMetadata
        {
            Version = 2,
            CreatedTime = "2026-01-01T00:00:00Z"
        };
        var writeResult = new Secret<CurrentSecretMetadata> { Data = writeMetadata };

        _kvV2.WriteSecretAsync(
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, object>>(),
                Arg.Any<int?>(),
                Arg.Any<string>())
            .Returns(Task.FromResult(writeResult));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        var metadata = result.ShouldBeSuccess();
        metadata.Name.ShouldBe(SecretName);
        metadata.Version.ShouldBe("2");
    }

    [Fact]
    public async Task SetSecretAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _kvV2.WriteSecretAsync(
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, object>>(),
                Arg.Any<int?>(),
                Arg.Any<string>())
            .Returns<Task<Secret<CurrentSecretMetadata>>>(_ =>
                throw new VaultApiException(System.Net.HttpStatusCode.Forbidden, "Permission denied."));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task SetSecretAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _kvV2.WriteSecretAsync(
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, object>>(),
                Arg.Any<int?>(),
                Arg.Any<string>())
            .Returns<Task<Secret<CurrentSecretMetadata>>>(_ =>
                throw new VaultApiException(System.Net.HttpStatusCode.InternalServerError, "Internal server error."));

        // Act
        var result = await _sut.SetSecretAsync(SecretName, SecretValue);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // DeleteSecretAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSecretAsync_WhenClientSucceeds_ReturnsRightWithUnit()
    {
        // Arrange
        _kvV2.DeleteMetadataAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenNotFound_ReturnsLeftWithNotFoundCode()
    {
        // Arrange
        _kvV2.DeleteMetadataAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns<Task>(_ => throw new VaultApiException(System.Net.HttpStatusCode.NotFound, "Secret not found."));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenForbidden_ReturnsLeftWithAccessDeniedCode()
    {
        // Arrange
        _kvV2.DeleteMetadataAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns<Task>(_ => throw new VaultApiException(System.Net.HttpStatusCode.Forbidden, "Permission denied."));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.AccessDeniedCode);
    }

    [Fact]
    public async Task DeleteSecretAsync_WhenOtherError_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _kvV2.DeleteMetadataAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns<Task>(_ => throw new VaultApiException(System.Net.HttpStatusCode.InternalServerError, "Internal error."));

        // Act
        var result = await _sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // ListSecretsAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ListSecretsAsync_WhenClientSucceeds_ReturnsRightWithNames()
    {
        // Arrange
        var pathsData = new VaultSharp.V1.Commons.ListInfo { Keys = ["secret-a", "secret-b"] };
        var pathsResult = new Secret<VaultSharp.V1.Commons.ListInfo> { Data = pathsData };

        _kvV2.ReadSecretPathsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(_ => Task.FromResult(pathsResult));

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        var names = result.ShouldBeSuccess().ToList();
        names.Count.ShouldBe(2);
        names.ShouldContain("secret-a");
        names.ShouldContain("secret-b");
    }

    [Fact]
    public async Task ListSecretsAsync_WhenNotFound_ReturnsRightWithEmptyList()
    {
        // Arrange - 404 during list means no secrets; provider returns empty enumerable
        _kvV2.ReadSecretPathsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns<Task<Secret<VaultSharp.V1.Commons.ListInfo>>>(_ =>
                throw new VaultApiException(System.Net.HttpStatusCode.NotFound, "No secrets."));

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert - 404 on list is treated as empty, not an error
        var names = result.ShouldBeSuccess();
        names.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListSecretsAsync_WhenForbidden_ReturnsLeftWithProviderUnavailableCode()
    {
        // Arrange
        _kvV2.ReadSecretPathsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns<Task<Secret<VaultSharp.V1.Commons.ListInfo>>>(_ =>
                throw new VaultApiException(System.Net.HttpStatusCode.Forbidden, "Permission denied."));

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // ExistsAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExistsAsync_WhenFound_ReturnsTrue()
    {
        // Arrange
        var metadataData = new FullSecretMetadata { CreatedTime = "2026-01-01T00:00:00Z" };
        var metadataResult = new Secret<FullSecretMetadata> { Data = metadataData };

        _kvV2.ReadSecretMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(metadataResult));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeSuccess(true);
    }

    [Fact]
    public async Task ExistsAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange - 404 returns Right(false), not Left
        _kvV2.ReadSecretMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns<Task<Secret<FullSecretMetadata>>>(_ =>
                throw new VaultApiException(System.Net.HttpStatusCode.NotFound, "Not found."));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert - must be Right(false), not Left
        result.ShouldBeSuccess(false);
    }

    [Fact]
    public async Task ExistsAsync_WhenForbidden_ReturnsLeft()
    {
        // Arrange
        _kvV2.ReadSecretMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns<Task<Secret<FullSecretMetadata>>>(_ =>
                throw new VaultApiException(System.Net.HttpStatusCode.Forbidden, "Permission denied."));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    [Fact]
    public async Task ExistsAsync_WhenOtherError_ReturnsLeft()
    {
        // Arrange
        _kvV2.ReadSecretMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns<Task<Secret<FullSecretMetadata>>>(_ =>
                throw new VaultApiException(System.Net.HttpStatusCode.InternalServerError, "Internal error."));

        // Act
        var result = await _sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.ProviderUnavailableCode);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Sets up the ReadSecretAsync mock to return a successful result.
    /// VaultSharp's <c>IKeyValueSecretsEngineV2.ReadSecretAsync</c> returns
    /// <c>Task&lt;Secret&lt;SecretData&gt;&gt;</c>. The non-generic <c>SecretData</c>
    /// inherits from <c>SecretData&lt;IDictionary&lt;string, object&gt;&gt;</c>.
    /// </summary>
    private void SetupReadSecret(string name, string value, int version)
    {
        var secretData = new SecretData
        {
            Data = new Dictionary<string, object> { ["data"] = value },
            Metadata = new CurrentSecretMetadata { Version = version }
        };
        Secret<SecretData> secret = new() { Data = secretData };

        _kvV2.ReadSecretAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<string>(),
                Arg.Any<string?>())
            .Returns(Task.FromResult(secret));
    }

    /// <summary>
    /// Sets up the ReadSecretAsync mock to throw a <see cref="VaultApiException"/>.
    /// </summary>
    private void SetupReadSecretThrows(System.Net.HttpStatusCode statusCode, string message)
    {
        _kvV2.ReadSecretAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<string>(),
                Arg.Any<string?>())
            .Returns<Task<Secret<SecretData>>>(_ =>
                throw new VaultApiException(statusCode, message));
    }
}
