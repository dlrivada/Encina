using Encina.Messaging.Encryption;
using FluentAssertions;

namespace Encina.UnitTests.Messaging.Encryption;

public class MessageEncryptionOptionsTests
{
    [Fact]
    public void Defaults_Enabled_IsTrue()
    {
        var options = new MessageEncryptionOptions();
        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Defaults_EncryptAllMessages_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.EncryptAllMessages.Should().BeFalse();
    }

    [Fact]
    public void Defaults_DefaultKeyId_IsNull()
    {
        var options = new MessageEncryptionOptions();
        options.DefaultKeyId.Should().BeNull();
    }

    [Fact]
    public void Defaults_UseTenantKeys_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.UseTenantKeys.Should().BeFalse();
    }

    [Fact]
    public void Defaults_TenantKeyPattern_HasExpectedValue()
    {
        var options = new MessageEncryptionOptions();
        options.TenantKeyPattern.Should().Be("tenant-{0}-key");
    }

    [Fact]
    public void Defaults_AuditDecryption_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.AuditDecryption.Should().BeFalse();
    }

    [Fact]
    public void Defaults_AddHealthCheck_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void Defaults_EnableTracing_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.EnableTracing.Should().BeFalse();
    }

    [Fact]
    public void Defaults_EnableMetrics_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.EnableMetrics.Should().BeFalse();
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var options = new MessageEncryptionOptions
        {
            Enabled = false,
            EncryptAllMessages = true,
            DefaultKeyId = "my-key",
            UseTenantKeys = true,
            TenantKeyPattern = "custom-{0}",
            AuditDecryption = true,
            AddHealthCheck = true,
            EnableTracing = true,
            EnableMetrics = true
        };

        options.Enabled.Should().BeFalse();
        options.EncryptAllMessages.Should().BeTrue();
        options.DefaultKeyId.Should().Be("my-key");
        options.UseTenantKeys.Should().BeTrue();
        options.TenantKeyPattern.Should().Be("custom-{0}");
        options.AuditDecryption.Should().BeTrue();
        options.AddHealthCheck.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
    }
}
