using Azure.Core;
using Azure.Security.KeyVault.Keys;
using Encina.Messaging.Encryption.AzureKeyVault;
using Shouldly;

namespace Encina.UnitTests.Messaging.Encryption.AzureKeyVault;

public class AzureKeyVaultOptionsTests
{
    [Fact]
    public void Defaults_VaultUri_IsNull()
    {
        var options = new AzureKeyVaultOptions();
        options.VaultUri.ShouldBeNull();
    }

    [Fact]
    public void Defaults_KeyName_IsNull()
    {
        var options = new AzureKeyVaultOptions();
        options.KeyName.ShouldBeNull();
    }

    [Fact]
    public void Defaults_KeyVersion_IsNull()
    {
        var options = new AzureKeyVaultOptions();
        options.KeyVersion.ShouldBeNull();
    }

    [Fact]
    public void Defaults_Credential_IsNull()
    {
        var options = new AzureKeyVaultOptions();
        options.Credential.ShouldBeNull();
    }

    [Fact]
    public void Defaults_ClientOptions_IsNull()
    {
        var options = new AzureKeyVaultOptions();
        options.ClientOptions.ShouldBeNull();
    }

    [Fact]
    public void VaultUri_IsSettable()
    {
        var uri = new Uri("https://my-vault.vault.azure.net/");
        var options = new AzureKeyVaultOptions { VaultUri = uri };
        options.VaultUri.ShouldBe(uri);
    }

    [Fact]
    public void KeyName_IsSettable()
    {
        var options = new AzureKeyVaultOptions { KeyName = "my-key" };
        options.KeyName.ShouldBe("my-key");
    }

    [Fact]
    public void KeyVersion_IsSettable()
    {
        var options = new AzureKeyVaultOptions { KeyVersion = "abc123" };
        options.KeyVersion.ShouldBe("abc123");
    }

    [Fact]
    public void Credential_IsSettable()
    {
        var credential = Substitute.For<TokenCredential>();
        var options = new AzureKeyVaultOptions { Credential = credential };
        options.Credential.ShouldBeSameAs(credential);
    }

    [Fact]
    public void ClientOptions_IsSettable()
    {
        var clientOptions = new KeyClientOptions();
        var options = new AzureKeyVaultOptions { ClientOptions = clientOptions };
        options.ClientOptions.ShouldBeSameAs(clientOptions);
    }

    [Fact]
    public void ToString_IncludesVaultUriAndKeyName()
    {
        var options = new AzureKeyVaultOptions
        {
            VaultUri = new Uri("https://my-vault.vault.azure.net/"),
            KeyName = "test-key"
        };

        var result = options.ToString();

        result.ShouldContain("my-vault.vault.azure.net");
        result.ShouldContain("test-key");
    }

    [Fact]
    public void ToString_HandlesNullValues()
    {
        var options = new AzureKeyVaultOptions();

        var result = options.ToString();

        result.ShouldContain("AzureKeyVaultOptions");
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var uri = new Uri("https://vault.azure.net/");
        var credential = Substitute.For<TokenCredential>();
        var clientOptions = new KeyClientOptions();

        var options = new AzureKeyVaultOptions
        {
            VaultUri = uri,
            KeyName = "key-name",
            KeyVersion = "v1",
            Credential = credential,
            ClientOptions = clientOptions
        };

        options.VaultUri.ShouldBe(uri);
        options.KeyName.ShouldBe("key-name");
        options.KeyVersion.ShouldBe("v1");
        options.Credential.ShouldBeSameAs(credential);
        options.ClientOptions.ShouldBeSameAs(clientOptions);
    }
}
