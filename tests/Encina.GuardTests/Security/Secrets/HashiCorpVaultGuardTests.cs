using Encina.Security.Secrets.HashiCorpVault;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Encina.GuardTests.Security.Secrets;

/// <summary>
/// Guard clause tests for Encina.Security.Secrets.HashiCorpVault types.
/// Verifies that null and invalid arguments are properly rejected.
/// </summary>
public class HashiCorpVaultGuardTests
{
    #region HashiCorpVaultSecretProvider Constructor Guards

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        var options = new HashiCorpVaultOptions();
        var logger = Substitute.For<ILogger<HashiCorpVaultSecretProvider>>();

        var act = () => new HashiCorpVaultSecretProvider(null!, options, logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("client");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var client = Substitute.For<IVaultClient>();
        var logger = Substitute.For<ILogger<HashiCorpVaultSecretProvider>>();

        var act = () => new HashiCorpVaultSecretProvider(client, null!, logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var client = Substitute.For<IVaultClient>();
        var options = new HashiCorpVaultOptions();

        var act = () => new HashiCorpVaultSecretProvider(client, options, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetSecretAsync (string) Guards

    [Fact]
    public async Task GetSecretAsync_NullName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.GetSecretAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretAsync_EmptyName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.GetSecretAsync("");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretAsync_WhitespaceName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.GetSecretAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    #endregion

    #region GetSecretAsync<T> (typed) Guards

    [Fact]
    public async Task GetSecretAsync_Typed_NullName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.GetSecretAsync<TestConfig>(null!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretAsync_Typed_EmptyName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.GetSecretAsync<TestConfig>("");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretAsync_Typed_WhitespaceName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.GetSecretAsync<TestConfig>("   ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    #endregion

    #region SetSecretAsync Guards

    [Fact]
    public async Task SetSecretAsync_NullName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.SetSecretAsync(null!, "value");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task SetSecretAsync_EmptyName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.SetSecretAsync("", "value");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task SetSecretAsync_NullValue_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.SetSecretAsync("key", null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("value");
    }

    #endregion

    #region RotateSecretAsync Guards

    [Fact]
    public async Task RotateSecretAsync_NullName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.RotateSecretAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task RotateSecretAsync_EmptyName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.RotateSecretAsync("");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    [Fact]
    public async Task RotateSecretAsync_WhitespaceName_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        var act = async () => await provider.RotateSecretAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("secretName");
    }

    #endregion

    #region ServiceCollectionExtensions Guards

    [Fact]
    public void AddHashiCorpVaultSecrets_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddHashiCorpVaultSecrets(vault =>
        {
            vault.VaultAddress = "https://vault.example.com:8200";
            vault.AuthMethod = new TokenAuthMethodInfo("hvs.test");
        });

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

    #region Helpers

    private static HashiCorpVaultSecretProvider CreateProvider()
    {
        var client = Substitute.For<IVaultClient>();
        var options = new HashiCorpVaultOptions();
        var logger = Substitute.For<ILogger<HashiCorpVaultSecretProvider>>();
        return new HashiCorpVaultSecretProvider(client, options, logger);
    }

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}
