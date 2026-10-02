using Azure.Security.KeyVault.Secrets;
using Encina.Caching;
using Encina.Security.Secrets.AzureKeyVault;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets.AzureKeyVault;

/// <summary>
/// Registration-level endpoint validation for <see cref="ServiceCollectionExtensions.AddAzureKeyVaultSecrets"/> (#852).
/// </summary>
public sealed class AzureKeyVaultEndpointValidationRegistrationTests
{
    private static readonly Uri VaultUri = new("https://test-vault.vault.azure.net/");

    private static ServiceCollection CreateServices(ILoggerProvider? logs = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            if (logs is not null)
            {
                b.AddProvider(logs);
            }
        });
        services.AddSingleton(Substitute.For<ICacheProvider>());
        return services;
    }

    [Theory]
    [InlineData("http://test-vault.vault.azure.net/", "HTTPS")]
    [InlineData("https://127.0.0.1/", "AllowLocalEndpoints")]
    [InlineData("https://169.254.169.254/", "metadata")]
    public void AddAzureKeyVaultSecrets_UnsafeVaultUri_ThrowsAtRegistration(string uri, string reason)
    {
        var services = CreateServices();

        Should.Throw<OptionsValidationException>(() => services.AddAzureKeyVaultSecrets(new Uri(uri)))
            .Message.ShouldContain(reason);
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_LocalEmulatorWithOptOuts_RegistersAndWarnsOnce()
    {
        var logs = new CollectingLoggerProvider();
        var services = CreateServices(logs);
        services.AddAzureKeyVaultSecrets(
            new Uri("http://localhost:8443/"),
            kv =>
            {
                kv.AllowInsecureHttp = true;
                kv.AllowLocalEndpoints = true;
            });

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;
        _ = provider.GetRequiredService<IOptionsMonitor<AzureKeyVaultOptions>>().CurrentValue;

        logs.Count(5209, LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_RegistersOptionsValidator()
    {
        var services = CreateServices();

        services.AddAzureKeyVaultSecrets(VaultUri);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<AzureKeyVaultOptions>) &&
            d.ImplementationType == typeof(AzureKeyVaultOptionsValidator));
    }

    [Fact]
    public void AddAzureKeyVaultSecrets_ValidOptions_ProviderBuildsWithValidateOnBuildAndScopes()
    {
        var services = CreateServices();
        services.AddAzureKeyVaultSecrets(VaultUri);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<SecretClient>().VaultUri.ShouldBe(VaultUri);
    }
}
