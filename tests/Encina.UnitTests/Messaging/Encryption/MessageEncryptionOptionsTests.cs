using Encina.Messaging.Encryption;
using Shouldly;

namespace Encina.UnitTests.Messaging.Encryption;

public class MessageEncryptionOptionsTests
{
    [Fact]
    public void Defaults_Enabled_IsTrue()
    {
        var options = new MessageEncryptionOptions();
        options.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_EncryptAllMessages_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.EncryptAllMessages.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_DefaultKeyId_IsNull()
    {
        var options = new MessageEncryptionOptions();
        options.DefaultKeyId.ShouldBeNull();
    }

    [Fact]
    public void Defaults_UseTenantKeys_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.UseTenantKeys.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_TenantKeyPattern_HasExpectedValue()
    {
        var options = new MessageEncryptionOptions();
        options.TenantKeyPattern.ShouldBe("tenant-{0}-key");
    }

    [Fact]
    public void Defaults_AuditDecryption_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.AuditDecryption.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_AddHealthCheck_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_EnableTracing_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.EnableTracing.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_EnableMetrics_IsFalse()
    {
        var options = new MessageEncryptionOptions();
        options.EnableMetrics.ShouldBeFalse();
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

        options.Enabled.ShouldBeFalse();
        options.EncryptAllMessages.ShouldBeTrue();
        options.DefaultKeyId.ShouldBe("my-key");
        options.UseTenantKeys.ShouldBeTrue();
        options.TenantKeyPattern.ShouldBe("custom-{0}");
        options.AuditDecryption.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeTrue();
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeTrue();
    }
}
