using Encina.Security.Secrets.HashiCorpVault;
using Shouldly;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace Encina.UnitTests.Security.Secrets.HashiCorpVault;

public sealed class HashiCorpVaultOptionsTests
{
    [Fact]
    public void VaultAddress_DefaultsToEmptyString()
    {
        var options = new HashiCorpVaultOptions();

        options.VaultAddress.ShouldBeEmpty();
    }

    [Fact]
    public void AuthMethod_DefaultsToNull()
    {
        var options = new HashiCorpVaultOptions();

        options.AuthMethod.ShouldBeNull();
    }

    [Fact]
    public void MountPoint_DefaultsToSecret()
    {
        var options = new HashiCorpVaultOptions();

        options.MountPoint.ShouldBe("secret");
    }

    [Fact]
    public void VaultAddress_CanBeSet()
    {
        var options = new HashiCorpVaultOptions();

        options.VaultAddress = "https://vault.example.com:8200";

        options.VaultAddress.ShouldBe("https://vault.example.com:8200");
    }

    [Fact]
    public void AuthMethod_CanBeSet()
    {
        var options = new HashiCorpVaultOptions();
        IAuthMethodInfo auth = new TokenAuthMethodInfo("hvs.my-token");

        options.AuthMethod = auth;

        options.AuthMethod.ShouldBeSameAs(auth);
    }

    [Fact]
    public void MountPoint_CanBeSet()
    {
        var options = new HashiCorpVaultOptions();

        options.MountPoint = "custom-kv";

        options.MountPoint.ShouldBe("custom-kv");
    }

    [Fact]
    public void ToString_WhenDefaults_DoesNotThrowAndContainsExpectedDefaults()
    {
        var options = new HashiCorpVaultOptions();

        var str = options.ToString();

        str.ShouldContain("VaultAddress=null");
        str.ShouldContain("MountPoint=secret");
        str.ShouldContain("AuthMethod=null");
    }

    [Fact]
    public void ToString_WhenAuthMethodSet_MasksSecretAndShowsTypeName()
    {
        var options = new HashiCorpVaultOptions
        {
            VaultAddress = "https://vault.example.com:8200",
            AuthMethod = new TokenAuthMethodInfo("hvs.my-super-secret-token")
        };

        var str = options.ToString();

        str.ShouldContain("AuthMethod=*** (TokenAuthMethodInfo)");
        str.ShouldNotContain("hvs.my-super-secret-token");
        str.ShouldContain("VaultAddress=https://vault.example.com:8200");
    }

    [Fact]
    public void ToString_WhenVaultAddressHasCredentials_StripsUserInfo()
    {
        var options = new HashiCorpVaultOptions
        {
            VaultAddress = "https://user:secret-password@vault.example.com:8200",
            AuthMethod = new TokenAuthMethodInfo("hvs.my-token")
        };

        var str = options.ToString();

        str.ShouldContain("VaultAddress=https://vault.example.com:8200");
        str.ShouldNotContain("secret-password");
        str.ShouldNotContain("user:");
    }

    [Fact]
    public void JsonSerialize_ExcludesAuthMethod()
    {
        var options = new HashiCorpVaultOptions
        {
            VaultAddress = "https://vault.example.com:8200",
            AuthMethod = new TokenAuthMethodInfo("hvs.my-super-secret-token")
        };

        var json = System.Text.Json.JsonSerializer.Serialize(options);

        json.ShouldNotContain("AuthMethod");
        json.ShouldNotContain("hvs.my-super-secret-token");
        json.ShouldContain("VaultAddress");
    }
}
