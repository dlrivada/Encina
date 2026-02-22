using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.HashiCorpVault;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Encina.UnitTests.Security.Secrets.HashiCorpVault;

public sealed class HashiCorpVaultServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithMockClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        // Pre-register a mock IVaultClient so the real factory (which needs
        // a running Vault server) is never invoked. TryAddSingleton in the
        // extension method will skip registration because IVaultClient is already present.
        services.AddSingleton(Substitute.For<IVaultClient>());

        return services;
    }

    private static Action<HashiCorpVaultOptions> ValidVaultConfig => vault =>
    {
        vault.VaultAddress = "https://vault.example.com:8200";
        vault.AuthMethod = new TokenAuthMethodInfo("hvs.test-token");
    };

    #region Service Registration

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersISecretReader()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretReader>().Should().NotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersISecretWriter()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretWriter>().Should().NotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersISecretRotator()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretRotator>().Should().NotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersHashiCorpVaultSecretProvider()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        provider.GetService<HashiCorpVaultSecretProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_WriterAndRotator_ResolveSameProviderInstance()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        var underlying = provider.GetRequiredService<HashiCorpVaultSecretProvider>();
        var writer = provider.GetRequiredService<ISecretWriter>();
        var rotator = provider.GetRequiredService<ISecretRotator>();

        writer.Should().BeSameAs(underlying);
        rotator.Should().BeSameAs(underlying);
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddHashiCorpVaultSecrets_WithSecretsOptions_ConfiguresSecretsOptions()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(
            ValidVaultConfig,
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

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersOptionsAsSingleton()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "https://vault.example.com:8200";
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
            vault.MountPoint = "custom-kv";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<HashiCorpVaultOptions>();
        options.MountPoint.Should().Be("custom-kv");
        options.VaultAddress.Should().Be("https://vault.example.com:8200");
    }

    #endregion

    #region Validation

    [Fact]
    public void AddHashiCorpVaultSecrets_MissingVaultAddress_ThrowsInvalidOperationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
            // VaultAddress intentionally left empty
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*VaultAddress*required*");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_MissingAuthMethod_ThrowsInvalidOperationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "https://vault.example.com:8200";
            // AuthMethod intentionally left null
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AuthMethod*required*");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_WhitespaceVaultAddress_ThrowsInvalidOperationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "   ";
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*VaultAddress*required*");
    }

    #endregion

    #region Chaining and Idempotency

    [Fact]
    public void AddHashiCorpVaultSecrets_ReturnsServiceCollection_ForChaining()
    {
        var services = CreateServicesWithMockClient();

        var result = services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_CalledTwice_DoesNotThrow()
    {
        var services = CreateServicesWithMockClient();

        var act = () =>
        {
            services.AddHashiCorpVaultSecrets(ValidVaultConfig);
            services.AddHashiCorpVaultSecrets(ValidVaultConfig);
        };

        act.Should().NotThrow();
    }

    #endregion

    #region Null Guards

    [Fact]
    public void AddHashiCorpVaultSecrets_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_NullConfigureVault_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddHashiCorpVaultSecrets(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureVault");
    }

    #endregion
}
