using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Attributes;
using Shouldly;

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

        properties.Length.ShouldBe(2);
        properties.Select(p => p.Property.Name).ShouldContain("Title");
        properties.Select(p => p.Property.Name).ShouldContain("Content");
    }

    [Fact]
    public void GetProperties_TypeWithNoSanitizationAttributes_ReturnsEmpty()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_ReturnsCachedResults()
    {
        var first = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var second = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void GetProperties_ClearCache_ReturnsFreshResults()
    {
        var first = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        SanitizationPropertyCache.ClearCache();
        var second = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));

        first.ShouldNotBeSameAs(second);
        first.Length.ShouldBe(second.Length);
    }

    [Fact]
    public void GetProperties_PreservesAttributeType()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));

        var titleProp = properties.First(p => p.Property.Name == "Title");
        titleProp.Attribute.SanitizationType.ShouldBe(SanitizationType.Html);

        var contentProp = properties.First(p => p.Property.Name == "Content");
        contentProp.Attribute.SanitizationType.ShouldBe(SanitizationType.Sql);
    }

    [Fact]
    public void GetProperties_CompiledGetterWorks()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithSanitizedProps { Title = "test-value" };
        var value = titleProp.Getter(instance);

        value.ShouldBe("test-value");
    }

    [Fact]
    public void GetProperties_CompiledSetterWorks()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithSanitizedProps { Title = "original" };
        titleProp.Setter(instance, "modified");

        instance.Title.ShouldBe("modified");
    }

    [Fact]
    public void GetProperties_SkipsNonStringProperties()
    {
        var properties = SanitizationPropertyCache.GetProperties(typeof(TypeWithNonStringAttribute));

        // Non-string properties are skipped even if decorated
        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_DifferentTypes_CachesIndependently()
    {
        var props1 = SanitizationPropertyCache.GetProperties(typeof(TypeWithSanitizedProps));
        var props2 = SanitizationPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        props1.Length.ShouldBe(2);
        props2.ShouldBeEmpty();
    }

    #endregion

    #region GetStringProperties

    [Fact]
    public void GetStringProperties_ReturnsAllStringProperties()
    {
        var properties = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        properties.Length.ShouldBe(3); // Title, Content, Description (all strings)
    }

    [Fact]
    public void GetStringProperties_ExcludesNonStringProperties()
    {
        var properties = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        properties.Select(p => p.Name).ShouldNotContain("Age");
    }

    [Fact]
    public void GetStringProperties_ReturnsConsistentResults()
    {
        var first = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));
        var second = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        first.Length.ShouldBe(second.Length);
        first.Select(p => p.Name).ShouldBe(second.Select(p => p.Name));
    }

    [Fact]
    public void GetStringProperties_TypeWithNoStrings_ReturnsEmpty()
    {
        var properties = SanitizationPropertyCache.GetStringProperties(typeof(TypeWithNoStrings));

        properties.ShouldBeEmpty();
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
