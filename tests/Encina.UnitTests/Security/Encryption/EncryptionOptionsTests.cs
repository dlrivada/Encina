using Encina.Security.Encryption;
using Shouldly;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionOptionsTests
{
    [Fact]
    public void DefaultAlgorithm_IsAes256Gcm()
    {
        var options = new EncryptionOptions();

        options.DefaultAlgorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
    }

    [Fact]
    public void FailOnDecryptionError_DefaultsToTrue()
    {
        var options = new EncryptionOptions();

        options.FailOnDecryptionError.ShouldBeTrue();
    }

    [Fact]
    public void AddHealthCheck_DefaultsToFalse()
    {
        var options = new EncryptionOptions();

        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void EnableTracing_DefaultsToFalse()
    {
        var options = new EncryptionOptions();

        options.EnableTracing.ShouldBeFalse();
    }

    [Fact]
    public void EnableMetrics_DefaultsToFalse()
    {
        var options = new EncryptionOptions();

        options.EnableMetrics.ShouldBeFalse();
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var options = new EncryptionOptions
        {
            DefaultAlgorithm = EncryptionAlgorithm.Aes256Gcm,
            FailOnDecryptionError = false,
            AddHealthCheck = true,
            EnableTracing = true,
            EnableMetrics = true
        };

        options.DefaultAlgorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        options.FailOnDecryptionError.ShouldBeFalse();
        options.AddHealthCheck.ShouldBeTrue();
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeTrue();
    }
}
