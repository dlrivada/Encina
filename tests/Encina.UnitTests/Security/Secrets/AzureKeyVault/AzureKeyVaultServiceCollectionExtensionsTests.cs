using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.AzureKeyVault;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.AzureKeyVault;

public sealed class AzureKeyVaultServiceCollectionExtensionsTests
{
    private static readonly Uri VaultUri = new("https://test-vault.vault.azure.net/");

    private static ServiceCollection CreateServicesWithMockClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        // Pre-register a mock SecretClient so the real factory (which needs Azure
        // credentials) is never invoked. TryAddSingleton in the extension method
        // will skip registration because SecretClient is already present.
        services.AddSingleton(Substitute.For<SecretClient>());

        return services;
    }

    #region Service Registration

    [Fact]
    public void AddAzureKeyVaultSecrets_RegistersISecretReader()
    {
        var services = CreateServicesWithMockClient();

        services.AddAzureKeyVaultSecrets(VaultUri);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretReader>().Should().NotBeNull();
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_RegistersISecretWriter()
    {
        var services = CreateServicesWithMockClient();

        services.AddAzureKeyVaultSecrets(VaultUri);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretWriter>().Should().NotBeNull();
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_RegistersISecretRotator()
    {
        var services = CreateServicesWithMockClient();

        services.AddAzureKeyVaultSecrets(VaultUri);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretRotator>().Should().NotBeNull();
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_RegistersAzureKeyVaultSecretProvider()
    {
        var services = CreateServicesWithMockClient();

        services.AddAzureKeyVaultSecrets(VaultUri);

        var provider = services.BuildServiceProvider();
        provider.GetService<AzureKeyVaultSecretProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_WriterAndRotator_ResolveSameProviderInstance()
    {
        var services = CreateServicesWithMockClient();

        services.AddAzureKeyVaultSecrets(VaultUri);

        var provider = services.BuildServiceProvider();
        var underlying = provider.GetRequiredService<AzureKeyVaultSecretProvider>();
        var writer = provider.GetRequiredService<ISecretWriter>();
        var rotator = provider.GetRequiredService<ISecretRotator>();

        writer.Should().BeSameAs(underlying);
        rotator.Should().BeSameAs(underlying);
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddAzureKeyVaultSecrets_ConfiguresVaultUri()
    {
        var services = CreateServicesWithMockClient();

        services.AddAzureKeyVaultSecrets(VaultUri);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;
        options.VaultUri.Should().Be(VaultUri);
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_WithCustomCredential_SetsCredential()
    {
        var services = CreateServicesWithMockClient();
        var credential = Substitute.For<TokenCredential>();

        services.AddAzureKeyVaultSecrets(VaultUri, kv => kv.Credential = credential);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;
        options.Credential.Should().BeSameAs(credential);
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_WithClientOptions_SetsClientOptions()
    {
        var services = CreateServicesWithMockClient();
        var clientOptions = new SecretClientOptions();

        services.AddAzureKeyVaultSecrets(VaultUri, kv => kv.ClientOptions = clientOptions);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;
        options.ClientOptions.Should().BeSameAs(clientOptions);
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_WithSecretsOptions_ConfiguresSecretsOptions()
    {
        var services = CreateServicesWithMockClient();

        services.AddAzureKeyVaultSecrets(VaultUri,
            configureSecrets: o =>
            {
                o.EnableCaching = false;
                o.DefaultCacheDuration = TimeSpan.FromMinutes(30);
            });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecretsOptions>>().Value;
        options.EnableCaching.Should().BeFalse();
        options.DefaultCacheDuration.Should().Be(TimeSpan.FromMinutes(30));
    }

    #endregion

    #region Chaining and Idempotency

    [Fact]
    public void AddAzureKeyVaultSecrets_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddAzureKeyVaultSecrets(VaultUri);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_CalledTwice_DoesNotThrow()
    {
        var services = CreateServicesWithMockClient();

        var act = () =>
        {
            services.AddAzureKeyVaultSecrets(VaultUri);
            services.AddAzureKeyVaultSecrets(VaultUri);
        };

        act.Should().NotThrow();
    }

    #endregion

    #region Null Guards

    [Fact]
    public void AddAzureKeyVaultSecrets_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddAzureKeyVaultSecrets(VaultUri);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_NullVaultUri_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddAzureKeyVaultSecrets(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("vaultUri");
    }

    #endregion
}
