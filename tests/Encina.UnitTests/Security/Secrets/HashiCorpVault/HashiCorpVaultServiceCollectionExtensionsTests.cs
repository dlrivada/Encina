using Encina.Caching;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.HashiCorpVault;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Encina.UnitTests.Security.Secrets.HashiCorpVault;

public sealed class HashiCorpVaultServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithMockClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(NSubstitute.Substitute.For<ICacheProvider>());

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
        provider.GetService<ISecretReader>().ShouldNotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersISecretWriter()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretWriter>().ShouldNotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersISecretRotator()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretRotator>().ShouldNotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersHashiCorpVaultSecretProvider()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        provider.GetService<HashiCorpVaultSecretProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersWriterWithCachingDecorator()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        var provider = services.BuildServiceProvider();
        var underlying = provider.GetRequiredService<HashiCorpVaultSecretProvider>();
        var writer = provider.GetRequiredService<ISecretWriter>();
        var rotator = provider.GetRequiredService<ISecretRotator>();

        writer.ShouldBeOfType<CachingSecretWriterDecorator>();
        rotator.ShouldBeSameAs(underlying);
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
        options.EnableCaching.ShouldBeFalse();
        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(30));
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
        options.MountPoint.ShouldBe("custom-kv");
        options.VaultAddress.ShouldBe("https://vault.example.com:8200");
    }

    #endregion

    #region Validation

    [Fact]
    public void AddHashiCorpVaultSecrets_MissingVaultAddress_ThrowsOptionsValidationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
            // VaultAddress intentionally left empty
        });

        Should.Throw<OptionsValidationException>(act).Message.ShouldContain("VaultAddress must be configured");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_MissingAuthMethod_ThrowsOptionsValidationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "https://vault.example.com:8200";
            // AuthMethod intentionally left null
        });

        Should.Throw<OptionsValidationException>(act).Message.ShouldMatch(@"AuthMethod.*required");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_WhitespaceVaultAddress_ThrowsOptionsValidationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "   ";
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
        });

        Should.Throw<OptionsValidationException>(act).Message.ShouldContain("VaultAddress must be configured");
    }

    [Theory]
    [InlineData("http://vault.example.com:8200", "HTTPS")]
    [InlineData("https://localhost:8200", "AllowLocalEndpoints")]
    [InlineData("https://169.254.169.254", "metadata")]
    [InlineData("https://[::ffff:169.254.10.1]:8200", "link-local")]
    [InlineData("https://0.0.0.0:8200", "unspecified")]
    public void AddHashiCorpVaultSecrets_UnsafeVaultAddress_ThrowsOptionsValidationException(string address, string reason)
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = address;
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
        });

        Should.Throw<OptionsValidationException>(act).Message.ShouldContain(reason);
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_LocalDevServerWithOptOuts_Registers()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "http://localhost:8200";
            vault.AllowInsecureHttp = true;
            vault.AllowLocalEndpoints = true;
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<HashiCorpVaultOptions>().VaultAddress.ShouldBe("http://localhost:8200");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_WithOptOuts_LogsWarningOnce()
    {
        var logger = new CollectingLoggerProvider();
        var services = CreateServicesWithMockClient();
        services.AddLogging(b => b.AddProvider(logger));
        services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "http://localhost:8200";
            vault.AllowInsecureHttp = true;
            vault.AllowLocalEndpoints = true;
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
        });

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<HashiCorpVaultOptions>();
        _ = provider.GetRequiredService<IOptionsMonitor<HashiCorpVaultOptions>>().CurrentValue;

        logger.Count(5308, LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RegistersOptionsValidator()
    {
        var services = CreateServicesWithMockClient();

        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<HashiCorpVaultOptions>) &&
            d.ImplementationType == typeof(HashiCorpVaultOptionsValidator));
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_ValidOptions_ProviderBuildsWithValidateOnBuildAndScopes()
    {
        var services = CreateServicesWithMockClient();
        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<HashiCorpVaultOptions>().VaultAddress.ShouldBe("https://vault.example.com:8200");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_RealClientFactory_UsesValidatedAddress()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICacheProvider>());
        services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IVaultClient>().Settings.VaultServerUriWithPort
            .ShouldBe("https://vault.example.com:8200");
    }

    #endregion

    #region Chaining and Idempotency

    [Fact]
    public void AddHashiCorpVaultSecrets_ReturnsServiceCollection_ForChaining()
    {
        var services = CreateServicesWithMockClient();

        var result = services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        result.ShouldBeSameAs(services);
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

        Should.NotThrow(act);
    }

    #endregion

    #region Null Guards

    [Fact]
    public void AddHashiCorpVaultSecrets_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddHashiCorpVaultSecrets(ValidVaultConfig);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddHashiCorpVaultSecrets_NullConfigureVault_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddHashiCorpVaultSecrets(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("configureVault");
    }

    #endregion
}
