using Encina.Security.Encryption;
using Shouldly;

namespace Encina.UnitTests.Security.Encryption;

public sealed class EncryptedPropertyCacheTests : IDisposable
{
    public EncryptedPropertyCacheTests()
    {
        EncryptedPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        EncryptedPropertyCache.ClearCache();
    }

    [Fact]
    public void GetProperties_TypeWithEncryptedProperties_ReturnsDescriptors()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));

        properties.Length.ShouldBe(2);
        properties.Select(p => p.Property.Name).ShouldContain("Email");
        properties.Select(p => p.Property.Name).ShouldContain("SSN");
    }

    [Fact]
    public void GetProperties_TypeWithNoEncryptedProperties_ReturnsEmpty()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithNoEncryptedProps));

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_ReturnsCachedResults()
    {
        var first = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var second = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void GetProperties_ClearCache_ReturnsFreshResults()
    {
        var first = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        EncryptedPropertyCache.ClearCache();
        var second = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));

        first.ShouldNotBeSameAs(second);
        first.Length.ShouldBe(second.Length);
    }

    [Fact]
    public void GetProperties_PreservesAttributeMetadata()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithPurpose));

        properties.Length.ShouldBe(1);
        properties[0].Attribute.Purpose.ShouldBe("User.Email");
    }

    [Fact]
    public void GetProperties_CompiledSetterWorks()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var emailProp = properties.First(p => p.Property.Name == "Email");

        var instance = new TypeWithEncryptedProps { Email = "original" };
        emailProp.SetValue(instance, "modified");

        instance.Email.ShouldBe("modified");
    }

    [Fact]
    public void GetProperties_CompiledGetterWorks()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var emailProp = properties.First(p => p.Property.Name == "Email");

        var instance = new TypeWithEncryptedProps { Email = "test-value" };
        var value = emailProp.GetValue(instance);

        value.ShouldBe("test-value");
    }

    [Fact]
    public void GetProperties_SkipsReadOnlyProperties()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithReadOnlyEncrypt));

        // ReadOnly property should be skipped (no setter)
        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_DifferentTypes_CachesIndependently()
    {
        var props1 = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var props2 = EncryptedPropertyCache.GetProperties(typeof(TypeWithNoEncryptedProps));

        props1.Length.ShouldBe(2);
        props2.ShouldBeEmpty();
    }

    #region Test Types

    private sealed class TypeWithEncryptedProps
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;

        [Encrypt(Purpose = "SSN")]
        public string SSN { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class TypeWithNoEncryptedProps
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private sealed class TypeWithPurpose
    {
        [Encrypt(Purpose = "User.Email")]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class TypeWithReadOnlyEncrypt
    {
#pragma warning disable CA1822 // Mark members as static - instance property needed for reflection-based property cache tests
        [Encrypt(Purpose = "Computed")]
        public string Computed => "readonly";
#pragma warning restore CA1822
    }

    #endregion
}
