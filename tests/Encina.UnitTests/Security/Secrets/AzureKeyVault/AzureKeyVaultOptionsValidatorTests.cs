using Encina.Security.Secrets.AzureKeyVault;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets.AzureKeyVault;

/// <summary>
/// Unit tests for <see cref="AzureKeyVaultOptionsValidator"/> of the secrets provider (#852).
/// </summary>
public sealed class AzureKeyVaultOptionsValidatorTests
{
    private static AzureKeyVaultOptions Options(string? uri, bool insecure = false, bool local = false) => new()
    {
        VaultUri = uri is null ? null : new Uri(uri),
        AllowInsecureHttp = insecure,
        AllowLocalEndpoints = local,
    };

    [Fact]
    public void Validate_PublicHttpsVault_Succeeds()
    {
        new AzureKeyVaultOptionsValidator().Validate(null, Options("https://my-vault.vault.azure.net/")).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_PrivateEndpointAddress_Succeeds()
    {
        new AzureKeyVaultOptionsValidator().Validate(null, Options("https://10.1.2.3/")).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, "must be configured")]
    [InlineData("http://my-vault.vault.azure.net/", "HTTPS")]
    [InlineData("https://localhost:8443/", "loopback")]
    [InlineData("https://[::ffff:169.254.169.254]/", "metadata")]
    public void Validate_UnsafeVault_Fails(string? uri, string reason)
    {
        var result = new AzureKeyVaultOptionsValidator().Validate(null, Options(uri));

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldStartWith("AzureKeyVaultOptions.VaultUri");
        result.FailureMessage.ShouldContain(reason);
    }

    [Fact]
    public void Validate_LocalEmulatorWithOptOuts_SucceedsAndWarnsOnce()
    {
        var logs = new CollectingLoggerProvider();
        using var factory = LoggerFactory.Create(b => b.AddProvider(logs));
        var sut = new AzureKeyVaultOptionsValidator(factory.CreateLogger<AzureKeyVaultOptionsValidator>());
        var options = Options("http://localhost:8443/", insecure: true, local: true);

        sut.Validate(null, options).Succeeded.ShouldBeTrue();
        sut.Validate(null, options).Succeeded.ShouldBeTrue();

        logs.Count(5209, LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new AzureKeyVaultOptionsValidator().Validate(null, null!));
    }
}
