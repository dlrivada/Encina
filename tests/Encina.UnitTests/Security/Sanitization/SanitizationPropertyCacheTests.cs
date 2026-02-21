using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Attributes;
using FluentAssertions;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationPropertyCacheTests : IDisposable
{
    public SanitizationPropertyCacheTests()
    {
        SanitizationPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        SanitizationPropertyCache.ClearCache();
    }

    #region GetProperties

    [Fact]
    public void GetProperties_TypeWithSanitizationAttributes_ReturnsDescriptors()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));

        properties.Should().HaveCount(2);
        properties.Select(p => p.Property.Name).Should().Contain("Title");
        properties.Select(p => p.Property.Name).Should().Contain("Content");
    }

    [Fact]
    public void GetProperties_TypeWithNoSanitizationAttributes_ReturnsEmpty()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_ReturnsCachedResults()
    {
        var first = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var second = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void GetProperties_ClearCache_ReturnsFreshResults()
    {
        var first = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        SanitizationPropertyCache.ClearCache();
        var second = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));

        first.Should().NotBeSameAs(second);
        first.Should().HaveCount(second.Length);
    }

    [Fact]
    public void GetProperties_PreservesAttributeType()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));

        var titleProp = properties.First(p => p.Property.Name == "Title");
        titleProp.Attribute.SanitizationType.Should().Be(SanitizationType.Html);

        var contentProp = properties.First(p => p.Property.Name == "Content");
        contentProp.Attribute.SanitizationType.Should().Be(SanitizationType.Sql);
    }

    [Fact]
    public void GetProperties_CompiledGetterWorks()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithSanitizedProps { Title = "test-value" };
        var value = titleProp.Getter(instance);

        value.Should().Be("test-value");
    }

    [Fact]
    public void GetProperties_CompiledSetterWorks()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithSanitizedProps { Title = "original" };
        titleProp.Setter(instance, "modified");

        instance.Title.Should().Be("modified");
    }

    [Fact]
    public void GetProperties_SkipsNonStringProperties()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithNonStringAttribute));

        // Non-string properties are skipped even if decorated
        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_DifferentTypes_CachesIndependently()
    {
        var props1 = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var props2 = SanitizationPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        props1.Should().HaveCount(2);
        props2.Should().BeEmpty();
    }

    #endregion

    #region GetStringProperties

    [Fact]
    public void GetStringProperties_ReturnsAllStringProperties()
    {
        var properties = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        properties.Should().HaveCount(3); // Title, Content, Description (all strings)
    }

    [Fact]
    public void GetStringProperties_ExcludesNonStringProperties()
    {
        var properties = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        properties.Select(p => p.Name).Should().NotContain("Age");
    }

    [Fact]
    public void GetStringProperties_ReturnsConsistentResults()
    {
        var first = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));
        var second = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        first.Should().HaveCount(second.Length);
        first.Select(p => p.Name).Should().BeEquivalentTo(second.Select(p => p.Name));
    }

    [Fact]
    public void GetStringProperties_TypeWithNoStrings_ReturnsEmpty()
    {
        var properties = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithNoStrings));

        properties.Should().BeEmpty();
    }

    #endregion

    #region Test Types

    private sealed class TypeWithSanitizedProps
    {
        [SanitizeHtml]
        public string Title { get; set; } = string.Empty;

        [SanitizeSql]
        public string Content { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    private sealed class TypeWithNoAttributes
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private sealed class TypeWithNonStringAttribute
    {
        [SanitizeHtml]
        public int NotAString { get; set; }
    }

    private sealed class TypeWithMixedProps
    {
        [SanitizeHtml]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private sealed class TypeWithNoStrings
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}
