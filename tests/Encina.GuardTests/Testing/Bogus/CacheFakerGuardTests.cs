using Encina.Testing.Bogus;

namespace Encina.GuardTests.Testing.Bogus;

/// <summary>
/// Guard tests for <see cref="CacheKeyFaker"/>, <see cref="CacheEntryFaker{T}"/>,
/// <see cref="PubSubExtensions"/>, <see cref="CacheValueFaker{T}"/> and <see cref="CacheValueExtensions"/>.
/// </summary>
public sealed class CacheFakerGuardTests
{
    private static readonly Faker NullFaker = null!;

    public static TheoryData<string, Action> NullFakerCases() => new()
    {
        { "PubSubChannel", () => NullFaker.PubSubChannel() },
        { "PubSubChannel with prefix", () => NullFaker.PubSubChannel("prefix") },
        { "PubSubPattern", () => NullFaker.PubSubPattern("prefix") },
        { "CacheStringValue", () => NullFaker.CacheStringValue() },
        { "CacheIntValue", () => NullFaker.CacheIntValue() },
        { "CacheDecimalValue", () => NullFaker.CacheDecimalValue() },
        { "CacheBytesValue", () => NullFaker.CacheBytesValue() },
        { "CacheJsonValue", () => NullFaker.CacheJsonValue() },
        { "CacheStringListValue", () => NullFaker.CacheStringListValue() },
        { "CacheExpiration", () => NullFaker.CacheExpiration() },
        { "CacheSlidingExpiration", () => NullFaker.CacheSlidingExpiration() },
        { "CacheAbsoluteExpiration", () => NullFaker.CacheAbsoluteExpiration() },
    };

    [Theory]
    [MemberData(nameof(NullFakerCases))]
    public void ExtensionMethod_NullFaker_ThrowsArgumentNullExceptionNamingFaker(string method, Action call)
    {
        method.ShouldNotBeNullOrEmpty();
        BogusGuardAssert.Throws<ArgumentNullException>(call, "faker");
    }

    [Fact]
    public void PubSubChannel_InvalidPrefix_ThrowsNamingPrefix()
    {
        var faker = new Faker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(prefix => faker.PubSubChannel(prefix), "prefix");
    }

    [Fact]
    public void PubSubPattern_InvalidPrefix_ThrowsNamingPrefix()
    {
        var faker = new Faker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(prefix => faker.PubSubPattern(prefix), "prefix");
    }

    [Fact]
    public void CacheKeyFaker_WithPrefix_InvalidPrefix_ThrowsNamingPrefix()
    {
        var faker = new CacheKeyFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(prefix => faker.WithPrefix(prefix), "prefix");
    }

    [Fact]
    public void CacheKeyFaker_AsTagged_InvalidTag_ThrowsNamingTag()
    {
        var faker = new CacheKeyFaker();

        BogusGuardAssert.RejectsNullEmptyAndWhitespace(tag => faker.AsTagged(tag), "tag");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void CacheKeyFaker_WithSegments_OutsideOneToFive_ThrowsNamingSegments(int segments)
    {
        var faker = new CacheKeyFaker();

        BogusGuardAssert.Throws<ArgumentOutOfRangeException>(() => faker.WithSegments(segments), "segments");
    }

    [Fact]
    public void CacheEntryFaker_WithValue_NullGenerator_ThrowsNamingValueGenerator()
    {
        var faker = new CacheEntryFaker<string>();

        BogusGuardAssert.Throws<ArgumentNullException>(() => faker.WithValue(null!), "valueGenerator");
    }

    [Fact]
    public void CacheValueFaker_Configure_NullAction_ThrowsNamingConfigure()
    {
        var faker = new CacheValueFaker<string>();

        BogusGuardAssert.Throws<ArgumentNullException>(() => faker.Configure(null!), "configure");
    }
}
