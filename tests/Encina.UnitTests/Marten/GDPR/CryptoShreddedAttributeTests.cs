using Encina.Marten.GDPR;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class CryptoShreddedAttributeTests
{
    [Fact]
    public void SubjectIdProperty_WhenSet_ReturnsValue()
    {
        // Arrange & Act
        var attr = new CryptoShreddedAttribute { SubjectIdProperty = "UserId" };

        // Assert
        attr.SubjectIdProperty.ShouldBe("UserId");
    }

    [Fact]
    public void AttributeUsage_AllowsPropertyTarget()
    {
        // Arrange
        var usage = typeof(CryptoShreddedAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void CryptoShreddedAttribute_IsSealed()
    {
        // Assert
        typeof(CryptoShreddedAttribute).IsSealed.ShouldBeTrue();
    }
}
