using Encina.Testing.Bogus;

namespace Encina.UnitTests.Testing.Bogus;

/// <summary>
/// Unit tests for <see cref="CacheKeyFaker"/>.
/// </summary>
public sealed class CacheKeyFakerTests
{
    [Fact]
    public void Generate_Default_ShouldCreateNonEmptyKey()
    {
        var key = new CacheKeyFaker().Generate();
        key.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void WithPrefix_ShouldPrependPrefix()
    {
        var key = new CacheKeyFaker().WithPrefix("products").Generate();
        key.ShouldStartWith("products:");
    }

    [Fact]
    public void WithSegments_ShouldCreateHierarchicalKey()
    {
        var key = new CacheKeyFaker().WithSegments(3).Generate();
        // Hierarchical keys use : separator
        key.Split(':').Length.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void AsPattern_ShouldCreateWildcardPattern()
    {
        var key = new CacheKeyFaker().AsPattern().Generate();
        key.ShouldContain("*");
    }

    [Fact]
    public void AsTagged_ShouldIncludeTag()
    {
        var key = new CacheKeyFaker().AsTagged("featured").Generate();
        key.ShouldContain("featured");
    }

    [Fact]
    public void Generate_Reproducible_WithSameSeed()
    {
        var key1 = new CacheKeyFaker().Generate();
        var key2 = new CacheKeyFaker().Generate();
        key1.ShouldBe(key2);
    }

    [Fact]
    public void Constructor_WithLocale_ShouldUseLocale()
    {
        var faker = new CacheKeyFaker("fr");
        faker.Locale.ShouldBe("fr");
    }

    [Fact]
    public void MethodChaining_ReturnsSameInstance()
    {
        var faker = new CacheKeyFaker();
        faker.WithPrefix("x").ShouldBeSameAs(faker);
        faker.WithSegments(2).ShouldBeSameAs(faker);
        faker.AsPattern().ShouldBeSameAs(faker);
        faker.AsTagged("t").ShouldBeSameAs(faker);
    }
}
