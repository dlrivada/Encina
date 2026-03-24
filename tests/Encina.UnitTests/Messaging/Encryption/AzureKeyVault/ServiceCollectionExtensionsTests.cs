using Azure.Security.KeyVault.Keys;
using Encina.Messaging.Encryption.AzureKeyVault;
using Encina.Security.Encryption.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Messaging.Encryption.AzureKeyVault;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaMessageEncryptionAzureKeyVault(
            o => { o.VaultUri = new Uri("https://vault.azure.net/"); o.KeyName = "k"; });

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaMessageEncryptionAzureKeyVault(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = new Uri("https://vault.azure.net/");
            o.KeyName = "test-key";
        });

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_RegistersAzureKeyVaultOptions()
    {
        var services = new ServiceCollection();
        var vaultUri = new Uri("https://my-vault.vault.azure.net/");

        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = vaultUri;
            o.KeyName = "my-key";
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;

        options.VaultUri.Should().Be(vaultUri);
        options.KeyName.Should().Be("my-key");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_RegistersKeyClient()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            o.KeyName = "test-key";
        });

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(KeyClient));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_RegistersIKeyProvider()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            o.KeyName = "test-key";
        });

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IKeyProvider));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be(typeof(AzureKeyVaultKeyProvider));
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_NullVaultUri_ThrowsOnResolveKeyClient()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.KeyName = "test-key";
            // VaultUri intentionally null
        });

        var sp = services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<KeyClient>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*VaultUri*");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_PreRegisteredKeyClient_IsNotReplaced()
    {
        var services = new ServiceCollection();
        var existingClient = new KeyClient(
            new Uri("https://existing.vault.azure.net/"),
            new DummyTokenCredential());

        services.AddSingleton(existingClient);

        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = new Uri("https://new.vault.azure.net/");
            o.KeyName = "key";
        });

        var sp = services.BuildServiceProvider();
        var resolved = sp.GetRequiredService<KeyClient>();

        resolved.Should().BeSameAs(existingClient);
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_WithConfigureEncryption_PassesOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAzureKeyVault(
            o =>
            {
                o.VaultUri = new Uri("https://my-vault.vault.azure.net/");
                o.KeyName = "test";
            },
            e =>
            {
                e.EncryptAllMessages = true;
                e.DefaultKeyId = "custom-key";
            });

        var sp = services.BuildServiceProvider();
        var encOptions = sp.GetRequiredService<IOptions<global::Encina.Messaging.Encryption.MessageEncryptionOptions>>().Value;

        encOptions.EncryptAllMessages.Should().BeTrue();
        encOptions.DefaultKeyId.Should().Be("custom-key");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_WithCustomCredential_UsesProvidedCredential()
    {
        var services = new ServiceCollection();
        var credential = new DummyTokenCredential();

        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            o.KeyName = "test";
            o.Credential = credential;
        });

        var sp = services.BuildServiceProvider();

        // KeyClient should resolve without error (uses our credential, not DefaultAzureCredential)
        var act = () => sp.GetRequiredService<KeyClient>();
        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaMessageEncryptionAzureKeyVault_WithClientOptions_UsesProvidedClientOptions()
    {
        var services = new ServiceCollection();
        var clientOptions = new KeyClientOptions();

        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            o.KeyName = "test";
            o.Credential = new DummyTokenCredential();
            o.ClientOptions = clientOptions;
        });

        var sp = services.BuildServiceProvider();

        // Should not throw - uses the custom client options
        var act = () => sp.GetRequiredService<KeyClient>();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Minimal token credential for constructing KeyClient instances in tests.
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
