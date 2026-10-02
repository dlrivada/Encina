using Encina.Security.Secrets.HashiCorpVault;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;
using Shouldly;
using VaultSharp.V1.AuthMethods.Token;

namespace Encina.UnitTests.Security.Secrets.HashiCorpVault;

/// <summary>
/// Unit tests for <see cref="HashiCorpVaultOptionsValidator"/> (#852).
/// </summary>
public sealed class HashiCorpVaultOptionsValidatorTests
{
    private static HashiCorpVaultOptions Options(string address, bool insecure = false, bool local = false) => new()
    {
        VaultAddress = address,
        AuthMethod = new TokenAuthMethodInfo("hvs.test"),
        AllowInsecureHttp = insecure,
        AllowLocalEndpoints = local,
    };

    [Fact]
    public void Validate_PublicHttpsAddress_Succeeds()
    {
        new HashiCorpVaultOptionsValidator().Validate(null, Options("https://vault.example.com:8200")).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://vault.example.com:8200", "HTTPS")]
    [InlineData("https://127.0.0.1:8200", "loopback")]
    [InlineData("https://169.254.169.254", "metadata")]
    [InlineData("vault.example.com:8200", "allowed schemes")]
    public void Validate_UnsafeAddress_Fails(string address, string reason)
    {
        var result = new HashiCorpVaultOptionsValidator().Validate(null, Options(address));

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldStartWith("HashiCorpVaultOptions.VaultAddress");
        result.FailureMessage.ShouldContain(reason);
    }

    [Fact]
    public void Validate_LocalDevServerWithOptOuts_Succeeds()
    {
        new HashiCorpVaultOptionsValidator().Validate(null, Options("http://localhost:8200", insecure: true, local: true))
            .Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MissingAuthMethod_Fails()
    {
        var options = Options("https://vault.example.com");
        options.AuthMethod = null;

        new HashiCorpVaultOptionsValidator().Validate(null, options).FailureMessage!.ShouldContain("AuthMethod");
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new HashiCorpVaultOptionsValidator().Validate(null, null!));
    }

    [Fact]
    public void Validate_OptOuts_WarnOncePerOptionsName()
    {
        var logs = new CollectingLoggerProvider();
        using var factory = LoggerFactory.Create(b => b.AddProvider(logs));
        var sut = new HashiCorpVaultOptionsValidator(factory.CreateLogger<HashiCorpVaultOptionsValidator>());
        var options = Options("http://localhost:8200", insecure: true, local: true);

        sut.Validate(null, options);
        sut.Validate(null, options);
        sut.Validate("secondary", options);

        logs.Count(5308, LogLevel.Warning).ShouldBe(2);
    }

    [Fact]
    public void Validate_NoOptOut_DoesNotWarn()
    {
        var logs = new CollectingLoggerProvider();
        using var factory = LoggerFactory.Create(b => b.AddProvider(logs));
        var sut = new HashiCorpVaultOptionsValidator(factory.CreateLogger<HashiCorpVaultOptionsValidator>());

        sut.Validate(null, Options("https://vault.example.com"));

        logs.Count(5308, LogLevel.Warning).ShouldBe(0);
    }
}
