using Azure;
using Azure.Security.KeyVault.Keys;
using Encina.Diagnostics;
using Encina.Messaging.Encryption.AzureKeyVault;
using Encina.Testing;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Unit tests for <see cref="AzureKeyVaultKeyProvider.GetCurrentKeyIdAsync"/> against a substituted
/// <see cref="KeyClient"/>: the id with and without a version, and the Key Vault failure.
/// </summary>
public sealed class AzureKeyVaultKeyProviderCurrentKeyTests
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly KeyClient _keyClient = Substitute.For<KeyClient>();
    private readonly FakeLogger<AzureKeyVaultKeyProvider> _logger = new();

    [Fact]
    public async Task GetCurrentKeyIdAsync_WhenTheKeyHasAVersion_ReturnsNameAndVersion()
    {
        // Arrange
        SetupKey("test-key", "v1");
        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentKeyIdAsync();

        // Assert
        result.ShouldBeSuccess().ShouldBe("test-key/v1");
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_WhenTheKeyHasNoVersion_ReturnsTheName()
    {
        // Arrange
        SetupKey("test-key", null);
        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentKeyIdAsync();

        // Assert
        result.ShouldBeSuccess().ShouldBe("test-key");
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_WhenKeyVaultFails_ReturnsUnavailableAndLogsARedactedException()
    {
        // Arrange
        _keyClient
            .GetKeyAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response<KeyVaultKey>>>(_ => throw new RequestFailedException(500, SentinelMessage));
        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentKeyIdAsync();

        // Assert
        result.ShouldBeError();
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    private AzureKeyVaultKeyProvider CreateProvider() =>
        new(_keyClient, Options.Create(new AzureKeyVaultOptions { KeyName = "test-key" }), _logger);

    private void SetupKey(string name, string? version)
    {
        var properties = KeyModelFactory.KeyProperties(
            id: new Uri($"https://dummy-vault.vault.azure.net/keys/{name}" + (version is null ? string.Empty : $"/{version}")),
            vaultUri: new Uri("https://dummy-vault.vault.azure.net/"),
            name: name,
            version: version);

        var key = KeyModelFactory.KeyVaultKey(properties, key: null!);

        _keyClient
            .GetKeyAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Response.FromValue(key, Substitute.For<Response>())));
    }
}
