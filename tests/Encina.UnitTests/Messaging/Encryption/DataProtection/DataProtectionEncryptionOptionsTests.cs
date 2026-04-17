using Encina.Messaging.Encryption.DataProtection;
using Shouldly;

namespace Encina.UnitTests.Messaging.Encryption.DataProtection;

public class DataProtectionEncryptionOptionsTests
{
    [Fact]
    public void Defaults_Purpose_HasExpectedValue()
    {
        var options = new DataProtectionEncryptionOptions();
        options.Purpose.ShouldBe("Encina.Messaging.Encryption");
    }

    [Fact]
    public void Purpose_IsSettable()
    {
        var options = new DataProtectionEncryptionOptions { Purpose = "Custom.Purpose" };
        options.Purpose.ShouldBe("Custom.Purpose");
    }

    [Fact]
    public void Purpose_CanBeSetToEmptyString()
    {
        var options = new DataProtectionEncryptionOptions { Purpose = "" };
        options.Purpose.ShouldBeEmpty();
    }

    [Fact]
    public void Purpose_CanBeOverwritten()
    {
        var options = new DataProtectionEncryptionOptions();
        options.Purpose = "First";
        options.Purpose = "Second";
        options.Purpose.ShouldBe("Second");
    }
}
