using Encina.Security.Encryption;
using FluentAssertions;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionOptionsTests
{
    [Fact]
    public void DefaultAlgorithm_IsAes256Gcm()
    {
        var options = new EncryptionOptions();

        options.DefaultAlgorithm.Should().Be(EncryptionAlgorithm.Aes256Gcm);
    }

    [Fact]
    public void FailOnDecryptionError_DefaultsToTrue()
    {
        var options = new EncryptionOptions();

        options.FailOnDecryptionError.Should().BeTrue();
    }

    [Fact]
    public void AddHealthCheck_DefaultsToFalse()
    {
        var options = new EncryptionOptions();

        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void EnableTracing_DefaultsToFalse()
    {
        var options = new EncryptionOptions();

        options.EnableTracing.Should().BeFalse();
    }

    [Fact]
    public void EnableMetrics_DefaultsToFalse()
    {
        var options = new EncryptionOptions();

        options.EnableMetrics.Should().BeFalse();
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

        options.DefaultAlgorithm.Should().Be(EncryptionAlgorithm.Aes256Gcm);
        options.FailOnDecryptionError.Should().BeFalse();
        options.AddHealthCheck.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
    }
}
