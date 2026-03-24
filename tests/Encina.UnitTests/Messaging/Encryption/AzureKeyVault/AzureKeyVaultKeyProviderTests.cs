#pragma warning disable CA2012 // ValueTask consumed by NSubstitute mock setup

using Azure;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.AzureKeyVault;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Unit tests for <see cref="AzureKeyVaultKeyProvider"/>.
/// Because <see cref="KeyClient"/> is a concrete class without virtual methods,
/// we test constructor guards, ParseKeyId behavior via GetCurrentKeyIdAsync/RotateKeyAsync,
/// and validate the provider adheres to IKeyProvider contract.
/// </summary>
public class AzureKeyVaultKeyProviderTests
{
    private readonly ILogger<AzureKeyVaultKeyProvider> _logger =
        NullLogger<AzureKeyVaultKeyProvider>.Instance;

    // --- Constructor null guard tests ---

    [Fact]
    public void Constructor_NullKeyClient_ThrowsArgumentNullException()
    {
        var options = Options.Create(new AzureKeyVaultOptions { KeyName = "k" });

        Should.Throw<ArgumentNullException>(() => new AzureKeyVaultKeyProvider(null!, options, _logger))
            .ParamName.ShouldBe("keyClient");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var keyClient = CreateMinimalKeyClient();

        Should.Throw<ArgumentNullException>(() => new AzureKeyVaultKeyProvider(keyClient, null!, _logger))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var keyClient = CreateMinimalKeyClient();
        var options = Options.Create(new AzureKeyVaultOptions { KeyName = "k" });

        Should.Throw<ArgumentNullException>(() => new AzureKeyVaultKeyProvider(keyClient, options, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var keyClient = CreateMinimalKeyClient();
        var options = Options.Create(new AzureKeyVaultOptions { KeyName = "test" });

        Should.NotThrow(() => new AzureKeyVaultKeyProvider(keyClient, options, _logger));
    }

    // --- GetKeyAsync null guard tests ---

    [Fact]
    public async Task GetKeyAsync_NullKeyId_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var ex = await Should.ThrowAsync<ArgumentException>(async () => await provider.GetKeyAsync(null!));
        ex.ParamName.ShouldBe("keyId");
    }

    [Fact]
    public async Task GetKeyAsync_EmptyKeyId_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var ex = await Should.ThrowAsync<ArgumentException>(async () => await provider.GetKeyAsync(string.Empty));
        ex.ParamName.ShouldBe("keyId");
    }

    [Fact]
    public async Task GetKeyAsync_WhitespaceKeyId_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var ex = await Should.ThrowAsync<ArgumentException>(async () => await provider.GetKeyAsync("   "));
        ex.ParamName.ShouldBe("keyId");
    }

    // --- GetCurrentKeyIdAsync tests ---

    [Fact]
    public async Task GetCurrentKeyIdAsync_KeyNameNotConfigured_ReturnsLeft()
    {
        var provider = CreateProvider(keyName: null);

        var result = await provider.GetCurrentKeyIdAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("unavailable");
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_EmptyKeyName_ReturnsLeft()
    {
        var provider = CreateProvider(keyName: "");

        var result = await provider.GetCurrentKeyIdAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("unavailable");
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_WhitespaceKeyName_ReturnsLeft()
    {
        var provider = CreateProvider(keyName: "   ");

        var result = await provider.GetCurrentKeyIdAsync();

        result.IsLeft.ShouldBeTrue();
    }

    // --- RotateKeyAsync tests ---

    [Fact]
    public async Task RotateKeyAsync_KeyNameNotConfigured_ReturnsLeft()
    {
        var provider = CreateProvider(keyName: null);

        var result = await provider.RotateKeyAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("unavailable");
    }

    [Fact]
    public async Task RotateKeyAsync_EmptyKeyName_ReturnsLeft()
    {
        var provider = CreateProvider(keyName: "");

        var result = await provider.RotateKeyAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RotateKeyAsync_WhitespaceKeyName_ReturnsLeft()
    {
        var provider = CreateProvider(keyName: "   ");

        var result = await provider.RotateKeyAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.Message.ShouldContain("unavailable");
    }

    // --- IKeyProvider interface conformance ---

    [Fact]
    public void ImplementsIKeyProvider()
    {
        var provider = CreateProvider();
        provider.ShouldBeAssignableTo<IKeyProvider>();
    }

    // --- Helpers ---

    private AzureKeyVaultKeyProvider CreateProvider(string? keyName = "test-key", string? keyVersion = null)
    {
        var keyClient = CreateMinimalKeyClient();
        var options = Options.Create(new AzureKeyVaultOptions
        {
            KeyName = keyName,
            KeyVersion = keyVersion
        });
        return new AzureKeyVaultKeyProvider(keyClient, options, _logger);
    }

    /// <summary>
    /// Creates a minimal KeyClient. Since KeyClient is concrete and most methods
    /// require actual Azure connectivity, we instantiate one with a dummy URI.
    /// Methods that call Azure will throw, but constructor and option checks work fine.
    /// </summary>
    private static KeyClient CreateMinimalKeyClient()
    {
        return new KeyClient(new Uri("https://dummy-vault.vault.azure.net/"), new DummyTokenCredential());
    }

    /// <summary>
    /// A minimal token credential that never actually authenticates.
    /// Used only for constructing KeyClient instances in tests.
    /// </summary>
    private sealed class DummyTokenCredential : Azure.Core.TokenCredential
    {
        public override Azure.Core.AccessToken GetToken(
            Azure.Core.TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            new("dummy-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<Azure.Core.AccessToken> GetTokenAsync(
            Azure.Core.TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new Azure.Core.AccessToken("dummy-token", DateTimeOffset.UtcNow.AddHours(1)));
    }
}
