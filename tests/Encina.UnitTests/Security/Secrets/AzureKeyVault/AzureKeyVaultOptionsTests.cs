using Azure.Security.KeyVault.Secrets;
using Encina.Security.Secrets.AzureKeyVault;
using FluentAssertions;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.AzureKeyVault;

public sealed class AzureKeyVaultOptionsTests
{
    [Fact]
    public void VaultUri_DefaultsToNull()
    {
        var options = new AzureKeyVaultOptions();

        options.VaultUri.Should().BeNull();
    }

    [Fact]
    public void Credential_DefaultsToNull()
    {
        var options = new AzureKeyVaultOptions();

        options.Credential.Should().BeNull();
    }

    [Fact]
    public void ClientOptions_DefaultsToNull()
    {
        var options = new AzureKeyVaultOptions();

        options.ClientOptions.Should().BeNull();
    }

    [Fact]
    public void VaultUri_CanBeSet()
    {
        var options = new AzureKeyVaultOptions();
        var uri = new Uri("https://my-vault.vault.azure.net/");

        options.VaultUri = uri;

        options.VaultUri.Should().Be(uri);
    }

    [Fact]
    public void Credential_CanBeSet()
    {
        var options = new AzureKeyVaultOptions();
        var credential = Substitute.For<Azure.Core.TokenCredential>();

        options.Credential = credential;

        options.Credential.Should().BeSameAs(credential);
    }

    [Fact]
    public void ClientOptions_CanBeSet()
    {
        var options = new AzureKeyVaultOptions();
        var clientOptions = new SecretClientOptions();

        options.ClientOptions = clientOptions;

        options.ClientOptions.Should().BeSameAs(clientOptions);
    }
}
