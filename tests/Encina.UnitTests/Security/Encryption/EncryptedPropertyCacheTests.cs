using Encina.Security.Encryption;
using FluentAssertions;

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

        properties.Should().HaveCount(2);
        properties.Select(p => p.Property.Name).Should().Contain("Email");
        properties.Select(p => p.Property.Name).Should().Contain("SSN");
    }

    [Fact]
    public void GetProperties_TypeWithNoEncryptedProperties_ReturnsEmpty()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithNoEncryptedProps));

        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_ReturnsCachedResults()
    {
        var first = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var second = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void GetProperties_ClearCache_ReturnsFreshResults()
    {
        var first = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        EncryptedPropertyCache.ClearCache();
        var second = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));

        first.Should().NotBeSameAs(second);
        first.Should().HaveCount(second.Length);
    }

    [Fact]
    public void GetProperties_PreservesAttributeMetadata()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithPurpose));

        properties.Should().HaveCount(1);
        properties[0].Attribute.Purpose.Should().Be("User.Email");
    }

    [Fact]
    public void GetProperties_CompiledSetterWorks()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var emailProp = properties.First(p => p.Property.Name == "Email");

        var instance = new TypeWithEncryptedProps { Email = "original" };
        emailProp.SetValue(instance, "modified");

        instance.Email.Should().Be("modified");
    }

    [Fact]
    public void GetProperties_CompiledGetterWorks()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var emailProp = properties.First(p => p.Property.Name == "Email");

        var instance = new TypeWithEncryptedProps { Email = "test-value" };
        var value = emailProp.GetValue(instance);

        value.Should().Be("test-value");
    }

    [Fact]
    public void GetProperties_SkipsReadOnlyProperties()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(TypeWithReadOnlyEncrypt));

        // ReadOnly property should be skipped (no setter)
        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_DifferentTypes_CachesIndependently()
    {
        var props1 = EncryptedPropertyCache.GetProperties(typeof(TypeWithEncryptedProps));
        var props2 = EncryptedPropertyCache.GetProperties(typeof(TypeWithNoEncryptedProps));

        props1.Should().HaveCount(2);
        props2.Should().BeEmpty();
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
