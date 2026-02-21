using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Attributes;
using FluentAssertions;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class EncodingPropertyCacheTests : IDisposable
{
    public EncodingPropertyCacheTests()
    {
        EncodingPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        EncodingPropertyCache.ClearCache();
    }

    #region GetProperties

    [Fact]
    public void GetProperties_TypeWithEncodingAttributes_ReturnsDescriptors()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));

        properties.Should().HaveCount(2);
        properties.Select(p => p.Property.Name).Should().Contain("Title");
        properties.Select(p => p.Property.Name).Should().Contain("JsonData");
    }

    [Fact]
    public void GetProperties_TypeWithNoEncodingAttributes_ReturnsEmpty()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_ReturnsCachedResults()
    {
        var first = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var second = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void GetProperties_ClearCache_ReturnsFreshResults()
    {
        var first = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        EncodingPropertyCache.ClearCache();
        var second = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));

        first.Should().NotBeSameAs(second);
        first.Should().HaveCount(second.Length);
    }

    [Fact]
    public void GetProperties_PreservesEncodingContext()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));

        var titleProp = properties.First(p => p.Property.Name == "Title");
        titleProp.Attribute.EncodingContext.Should().Be(EncodingContext.Html);

        var jsonProp = properties.First(p => p.Property.Name == "JsonData");
        jsonProp.Attribute.EncodingContext.Should().Be(EncodingContext.JavaScript);
    }

    [Fact]
    public void GetProperties_CompiledGetterWorks()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithEncodedProps { Title = "test-value" };
        var value = titleProp.Getter(instance);

        value.Should().Be("test-value");
    }

    [Fact]
    public void GetProperties_CompiledSetterWorks()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithEncodedProps { Title = "original" };
        titleProp.Setter(instance, "modified");

        instance.Title.Should().Be("modified");
    }

    [Fact]
    public void GetProperties_DifferentTypes_CachesIndependently()
    {
        var props1 = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var props2 = EncodingPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        props1.Should().HaveCount(2);
        props2.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_AllEncodingContexts_Discovered()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithAllContexts));

        properties.Should().HaveCount(3);
        properties.Should().Contain(p => p.Attribute.EncodingContext == EncodingContext.Html);
        properties.Should().Contain(p => p.Attribute.EncodingContext == EncodingContext.JavaScript);
        properties.Should().Contain(p => p.Attribute.EncodingContext == EncodingContext.Url);
    }

    #endregion

    #region GetStringProperties

    [Fact]
    public void GetStringProperties_ReturnsAllStringProperties()
    {
        var properties = EncodingPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        properties.Should().HaveCount(3);
    }

    [Fact]
    public void GetStringProperties_ReturnsConsistentResults()
    {
        var first = EncodingPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));
        var second = EncodingPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        first.Should().HaveCount(second.Length);
        first.Select(p => p.Name).Should().BeEquivalentTo(second.Select(p => p.Name));
    }

    #endregion

    #region Test Types

    private sealed class TypeWithEncodedProps
    {
        [EncodeForHtml]
        public string Title { get; set; } = string.Empty;

        [EncodeForJavaScript]
        public string JsonData { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    private sealed class TypeWithNoAttributes
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private sealed class TypeWithAllContexts
    {
        [EncodeForHtml]
        public string HtmlContent { get; set; } = string.Empty;

        [EncodeForJavaScript]
        public string JsContent { get; set; } = string.Empty;

        [EncodeForUrl]
        public string UrlContent { get; set; } = string.Empty;
    }

    private sealed class TypeWithMixedProps
    {
        [EncodeForHtml]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    #endregion
}
