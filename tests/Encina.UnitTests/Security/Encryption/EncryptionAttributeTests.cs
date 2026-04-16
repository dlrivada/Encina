using Encina.Security.Encryption;
using Shouldly;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptionAttributeTests
{
    #region EncryptAttribute

    [Fact]
    public void EncryptAttribute_Defaults()
    {
        var attr = new EncryptAttribute();

        attr.Algorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        attr.FailOnError.ShouldBeTrue();
        attr.Purpose.ShouldBeNull();
        attr.KeyId.ShouldBeNull();
    }

    [Fact]
    public void EncryptAttribute_SetsPurpose()
    {
        var attr = new EncryptAttribute { Purpose = "User.Email" };

        attr.Purpose.ShouldBe("User.Email");
    }

    [Fact]
    public void EncryptAttribute_SetsKeyId()
    {
        var attr = new EncryptAttribute { KeyId = "custom-key" };

        attr.KeyId.ShouldBe("custom-key");
    }

    [Fact]
    public void EncryptAttribute_InheritsFromEncryptionAttribute()
    {
        var attr = new EncryptAttribute();

        attr.ShouldBeAssignableTo<EncryptionAttribute>();
    }

    [Fact]
    public void EncryptAttribute_CanSetFailOnErrorToFalse()
    {
        var attr = new EncryptAttribute { FailOnError = false };

        attr.FailOnError.ShouldBeFalse();
    }

    #endregion

    #region EncryptedResponseAttribute

    [Fact]
    public void EncryptedResponseAttribute_InheritsFromAttribute()
    {
        var attr = new EncryptedResponseAttribute();

        attr.ShouldBeAssignableTo<Attribute>();
    }

    [Fact]
    public void EncryptedResponseAttribute_CanBeAppliedToClass()
    {
        var type = typeof(TestEncryptedResponseClass);
        var attrs = type.GetCustomAttributes(typeof(EncryptedResponseAttribute), true);

        attrs.Count.ShouldBe(1);
    }

    #endregion

    #region DecryptOnReceiveAttribute

    [Fact]
    public void DecryptOnReceiveAttribute_InheritsFromAttribute()
    {
        var attr = new DecryptOnReceiveAttribute();

        attr.ShouldBeAssignableTo<Attribute>();
    }

    [Fact]
    public void DecryptOnReceiveAttribute_CanBeAppliedToClass()
    {
        var type = typeof(TestDecryptOnReceiveClass);
        var attrs = type.GetCustomAttributes(typeof(DecryptOnReceiveAttribute), true);

        attrs.Count.ShouldBe(1);
    }

    #endregion

    #region Test Types

    [EncryptedResponse]
    private sealed class TestEncryptedResponseClass;

    [DecryptOnReceive]
    private sealed class TestDecryptOnReceiveClass;

    #endregion
}
