using Encina.Security.Encryption;
using FluentAssertions;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionAttributeTests
{
    #region EncryptAttribute

    [Fact]
    public void EncryptAttribute_Defaults()
    {
        var attr = new EncryptAttribute();

        attr.Algorithm.Should().Be(EncryptionAlgorithm.Aes256Gcm);
        attr.FailOnError.Should().BeTrue();
        attr.Purpose.Should().BeNull();
        attr.KeyId.Should().BeNull();
    }

    [Fact]
    public void EncryptAttribute_SetsPurpose()
    {
        var attr = new EncryptAttribute { Purpose = "User.Email" };

        attr.Purpose.Should().Be("User.Email");
    }

    [Fact]
    public void EncryptAttribute_SetsKeyId()
    {
        var attr = new EncryptAttribute { KeyId = "custom-key" };

        attr.KeyId.Should().Be("custom-key");
    }

    [Fact]
    public void EncryptAttribute_InheritsFromEncryptionAttribute()
    {
        var attr = new EncryptAttribute();

        attr.Should().BeAssignableTo<EncryptionAttribute>();
    }

    [Fact]
    public void EncryptAttribute_CanSetFailOnErrorToFalse()
    {
        var attr = new EncryptAttribute { FailOnError = false };

        attr.FailOnError.Should().BeFalse();
    }

    #endregion

    #region EncryptedResponseAttribute

    [Fact]
    public void EncryptedResponseAttribute_InheritsFromAttribute()
    {
        var attr = new EncryptedResponseAttribute();

        attr.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void EncryptedResponseAttribute_CanBeAppliedToClass()
    {
        var type = typeof(TestEncryptedResponseClass);
        var attrs = type.GetCustomAttributes(typeof(EncryptedResponseAttribute), true);

        attrs.Should().HaveCount(1);
    }

    #endregion

    #region DecryptOnReceiveAttribute

    [Fact]
    public void DecryptOnReceiveAttribute_InheritsFromAttribute()
    {
        var attr = new DecryptOnReceiveAttribute();

        attr.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void DecryptOnReceiveAttribute_CanBeAppliedToClass()
    {
        var type = typeof(TestDecryptOnReceiveClass);
        var attrs = type.GetCustomAttributes(typeof(DecryptOnReceiveAttribute), true);

        attrs.Should().HaveCount(1);
    }

    #endregion

    #region Test Types

    [EncryptedResponse]
    private sealed class TestEncryptedResponseClass;

    [DecryptOnReceive]
    private sealed class TestDecryptOnReceiveClass;

    #endregion
}
