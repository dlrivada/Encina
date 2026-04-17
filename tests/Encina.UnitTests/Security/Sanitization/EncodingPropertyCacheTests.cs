using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Attributes;
using Shouldly;

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

        properties.Length.ShouldBe(2);
        properties.Select(p => p.Property.Name).ShouldContain("Title");
        properties.Select(p => p.Property.Name).ShouldContain("JsonData");
    }

    [Fact]
    public void GetProperties_TypeWithNoEncodingAttributes_ReturnsEmpty()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_ReturnsCachedResults()
    {
        var first = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var second = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void GetProperties_ClearCache_ReturnsFreshResults()
    {
        var first = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        EncodingPropertyCache.ClearCache();
        var second = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));

        first.ShouldNotBeSameAs(second);
        first.Length.ShouldBe(second.Length);
    }

    [Fact]
    public void GetProperties_PreservesEncodingContext()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));

        var titleProp = properties.First(p => p.Property.Name == "Title");
        titleProp.Attribute.EncodingContext.ShouldBe(EncodingContext.Html);

        var jsonProp = properties.First(p => p.Property.Name == "JsonData");
        jsonProp.Attribute.EncodingContext.ShouldBe(EncodingContext.JavaScript);
    }

    [Fact]
    public void GetProperties_CompiledGetterWorks()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithEncodedProps { Title = "test-value" };
        var value = titleProp.Getter(instance);

        value.ShouldBe("test-value");
    }

    [Fact]
    public void GetProperties_CompiledSetterWorks()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var titleProp = properties.First(p => p.Property.Name == "Title");

        var instance = new TypeWithEncodedProps { Title = "original" };
        titleProp.Setter(instance, "modified");

        instance.Title.ShouldBe("modified");
    }

    [Fact]
    public void GetProperties_DifferentTypes_CachesIndependently()
    {
        var props1 = EncodingPropertyCache.GetProperties(typeof(TypeWithEncodedProps));
        var props2 = EncodingPropertyCache.GetProperties(typeof(TypeWithNoAttributes));

        props1.Length.ShouldBe(2);
        props2.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_AllEncodingContexts_Discovered()
    {
        var properties = EncodingPropertyCache.GetProperties(typeof(TypeWithAllContexts));

        properties.Length.ShouldBe(3);
        properties.ShouldContain(p => p.Attribute.EncodingContext == EncodingContext.Html);
        properties.ShouldContain(p => p.Attribute.EncodingContext == EncodingContext.JavaScript);
        properties.ShouldContain(p => p.Attribute.EncodingContext == EncodingContext.Url);
    }

    #endregion

    #region GetStringProperties

    [Fact]
    public void GetStringProperties_ReturnsAllStringProperties()
    {
        var properties = EncodingPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        properties.Length.ShouldBe(3);
    }

    [Fact]
    public void GetStringProperties_ReturnsConsistentResults()
    {
        var first = EncodingPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));
        var second = EncodingPropertyCache.GetStringProperties(typeof(TypeWithMixedProps));

        first.Length.ShouldBe(second.Length);
        first.Select(p => p.Name).ShouldBe(second.Select(p => p.Name));
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
