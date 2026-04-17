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
}
