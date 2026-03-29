using Encina.Testing.Bogus;

namespace Encina.UnitTests.Testing.Bogus;

/// <summary>
/// Unit tests for <see cref="CacheEntryFaker{T}"/> and <see cref="CacheEntry{T}"/>.
/// </summary>
public sealed class CacheEntryFakerTests
{
    [Fact]
    public void Generate_Default_ShouldCreateValidEntry()
    {
        var faker = new CacheEntryFaker<string>();
        var entry = faker.Generate();

        entry.ShouldNotBeNull();
        entry.Key.ShouldNotBeNullOrEmpty();
        entry.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void WithExpiration_ShouldSetExpiration()
    {
        var expiry = TimeSpan.FromMinutes(10);
        var entry = new CacheEntryFaker<string>().WithExpiration(expiry).Generate();
        entry.Expiration.ShouldBe(expiry);
    }

    [Fact]
    public void WithSlidingExpiration_ShouldSetBothExpirations()
    {
        var sliding = TimeSpan.FromMinutes(5);
        var absolute = TimeSpan.FromHours(1);
        var entry = new CacheEntryFaker<string>().WithSlidingExpiration(sliding, absolute).Generate();
        entry.SlidingExpiration.ShouldBe(sliding);
        entry.AbsoluteExpiration.ShouldBe(absolute);
    }

    [Fact]
    public void WithTags_ShouldSetTags()
    {
        var entry = new CacheEntryFaker<string>().WithTags("products", "featured").Generate();
        entry.Tags.ShouldContain("products");
        entry.Tags.ShouldContain("featured");
    }

    [Fact]
    public void WithValue_ShouldSetCustomValue()
    {
        var entry = new CacheEntryFaker<string>().WithValue(_ => "custom-value").Generate();
        entry.Value.ShouldBe("custom-value");
    }

    [Fact]
    public void Generate_Reproducible_WithSameSeed()
    {
        var e1 = new CacheEntryFaker<string>().Generate();
        var e2 = new CacheEntryFaker<string>().Generate();
        e1.Key.ShouldBe(e2.Key);
    }

    [Fact]
    public void GenerateBetween_ShouldCreateMultiple()
    {
        var entries = new CacheEntryFaker<string>().GenerateBetween(2, 5);
        entries.Count.ShouldBeInRange(2, 5);
    }

    [Fact]
    public void Constructor_WithLocale_ShouldUseLocale()
    {
        var faker = new CacheEntryFaker<string>("es");
        faker.Locale.ShouldBe("es");
    }

    [Fact]
    public void MethodChaining_ReturnsSameInstance()
    {
        var faker = new CacheEntryFaker<string>();
        faker.WithExpiration(TimeSpan.FromMinutes(1)).ShouldBeSameAs(faker);
        faker.WithSlidingExpiration(TimeSpan.FromMinutes(1)).ShouldBeSameAs(faker);
        faker.WithTags("a").ShouldBeSameAs(faker);
        faker.WithValue(_ => "x").ShouldBeSameAs(faker);
    }
}
