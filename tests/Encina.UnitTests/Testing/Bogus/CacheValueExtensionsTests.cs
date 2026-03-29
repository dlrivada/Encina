using System.Text.Json;
using Bogus;
using Encina.Testing.Bogus;

namespace Encina.UnitTests.Testing.Bogus;

/// <summary>
/// Unit tests for <see cref="CacheValueExtensions"/> and <see cref="PubSubExtensions"/>.
/// </summary>
public sealed class CacheValueExtensionsTests
{
    private readonly Faker _faker = new();

    #region CacheValueExtensions

    [Fact]
    public void CacheStringValue_ShouldReturnStringWithinRange()
    {
        var value = _faker.CacheStringValue(5, 20);
        value.ShouldNotBeNullOrEmpty();
        value.Length.ShouldBeInRange(5, 20);
    }

    [Fact]
    public void CacheIntValue_ShouldReturnValueWithinRange()
    {
        var value = _faker.CacheIntValue(10, 100);
        value.ShouldBeInRange(10, 100);
    }

    [Fact]
    public void CacheDecimalValue_ShouldReturnValueWithinRange()
    {
        var value = _faker.CacheDecimalValue(1m, 50m);
        value.ShouldBeGreaterThanOrEqualTo(1m);
        value.ShouldBeLessThanOrEqualTo(50m);
    }

    [Fact]
    public void CacheBytesValue_ShouldReturnCorrectLength()
    {
        var bytes = _faker.CacheBytesValue(128);
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBe(128);
    }

    [Fact]
    public void CacheJsonValue_ShouldReturnValidJson()
    {
        var json = _faker.CacheJsonValue(3);
        json.ShouldNotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.EnumerateObject().Count().ShouldBe(3);
    }

    [Fact]
    public void CacheStringListValue_ShouldReturnCorrectCount()
    {
        var list = _faker.CacheStringListValue(4);
        list.ShouldNotBeNull();
        list.Count.ShouldBe(4);
        list.ShouldAllBe(s => !string.IsNullOrEmpty(s));
    }

    [Fact]
    public void CacheExpiration_ShouldReturnTimeSpanInRange()
    {
        var exp = _faker.CacheExpiration(1, 60);
        exp.TotalMinutes.ShouldBeGreaterThanOrEqualTo(1);
        exp.TotalMinutes.ShouldBeLessThanOrEqualTo(60);
    }

    [Fact]
    public void CacheSlidingExpiration_ShouldReturnTimeSpanInRange()
    {
        var exp = _faker.CacheSlidingExpiration(5, 30);
        exp.TotalMinutes.ShouldBeGreaterThanOrEqualTo(5);
        exp.TotalMinutes.ShouldBeLessThanOrEqualTo(30);
    }

    [Fact]
    public void CacheAbsoluteExpiration_ShouldReturnTimeSpanInRange()
    {
        var exp = _faker.CacheAbsoluteExpiration(1, 24);
        exp.TotalHours.ShouldBeGreaterThanOrEqualTo(1);
        exp.TotalHours.ShouldBeLessThanOrEqualTo(24);
    }

    #endregion

    #region PubSubExtensions

    [Fact]
    public void PubSubChannel_ShouldReturnNonEmpty()
    {
        var channel = _faker.PubSubChannel();
        channel.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void PubSubChannel_WithPrefix_ShouldStartWithPrefix()
    {
        var channel = _faker.PubSubChannel("cache");
        channel.ShouldStartWith("cache:");
    }

    [Fact]
    public void PubSubPattern_ShouldContainWildcard()
    {
        var pattern = _faker.PubSubPattern("events");
        pattern.ShouldStartWith("events:");
        pattern.ShouldContain("*");
    }

    #endregion

    #region CacheValueFaker

    [Fact]
    public void CacheValueFaker_Generate_ShouldCreateValue()
    {
        var faker = new CacheValueFaker<TestCacheDto>();
        faker.RuleFor(x => x.Name, f => f.Commerce.ProductName());
        var value = faker.Generate();
        value.ShouldNotBeNull();
        value.Name.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void CacheValueFaker_Configure_ShouldApply()
    {
        var faker = new CacheValueFaker<TestCacheDto>().Configure(f =>
        {
            f.RuleFor(x => x.Name, _ => "fixed");
        });
        var value = faker.Generate();
        value.Name.ShouldBe("fixed");
    }

    private sealed class TestCacheDto
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
