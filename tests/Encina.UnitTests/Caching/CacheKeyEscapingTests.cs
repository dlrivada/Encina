using Encina.Caching;
using Encina.Caching.Memory;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching;

/// <summary>
/// Tenant and user ids are escaped (<c>:</c> becomes <c>%3A</c>, <c>%</c> becomes <c>%25</c>) when they are
/// placed in a key or pattern, so a segment is always one part and every key still matches its own pattern (#1953).
/// </summary>
public sealed class CacheKeyEscapingTests
{
    [Cache(DurationSeconds = 60, VaryByUser = true)]
    public sealed record DefaultKeyQuery(string Value) : IRequest<string>;

    [Cache(DurationSeconds = 60)]
    public sealed record TenantKeyQuery(string Value) : IRequest<string>;

    [Cache(DurationSeconds = 60, VaryByUser = true, KeyTemplate = "items:{Value}")]
    public sealed record TemplateKeyQuery(string Value) : IRequest<string>;

    private static readonly IOptions<CachingOptions> Options_ = Options.Create(new CachingOptions());

    private static IRequestContext Context(string userId, string tenantId) =>
        TestRequestContext.For(TestIdentity.User(userId), tenantId: tenantId);

    [Theory]
    [InlineData("urn:uuid:1", "https://idp.example:acme", "u:urn%3Auuid%3A1", "t:https%3A//idp.example%3Aacme")]
    [InlineData("a%3Ab", "100%", "u:a%253Ab", "t:100%25")]
    public void DefaultKey_EscapesTheSeparatorAndThePercentSign(string userId, string tenantId, string userSegment, string tenantSegment)
    {
        var sut = new DefaultCacheKeyGenerator(Options_);

        var key = sut.GenerateKey<DefaultKeyQuery, string>(new DefaultKeyQuery("q"), Context(userId, tenantId));

        key.ShouldContain(":" + tenantSegment + ":");
        key.ShouldContain(":" + userSegment + ":");
        key.Split(':').Length.ShouldBe(7); // prefix, t, tenant, u, user, type, hash: ids add no extra segments
    }

    [Fact]
    public void TemplateKey_EscapesTheSeparatorAndThePercentSign()
    {
        var sut = new DefaultCacheKeyGenerator(Options_);

        var key = sut.GenerateKey<TemplateKeyQuery, string>(new TemplateKeyQuery("x"), Context("urn:uuid:1", "100%"));

        key.ShouldBe($"{Options_.Value.KeyPrefix}:t:100%25:u:urn%3Auuid%3A1:items:x");
    }

    [Fact]
    public void CacheConfigurationKey_EscapesTheSeparatorAndThePercentSign()
    {
        var configuration = new CacheConfiguration<DefaultKeyQuery> { VaryByUser = true };

        var key = configuration.GenerateKey(new DefaultKeyQuery("q"), Context("urn:uuid:1", "100%"));

        key.ShouldStartWith("t:100%25:u:urn%3Auuid%3A1:DefaultKeyQuery:");
    }

    [Theory]
    [InlineData("urn:uuid:1", "https://idp.example:acme")]
    [InlineData("a%3Ab", "100%")]
    public async Task AKey_IsRemovedByItsOwnPattern_ForIdsWithTheSeparatorOrPercentSign(string userId, string tenantId)
    {
        using var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        var provider = new MemoryCacheProvider(
            memoryCache, Options.Create(new MemoryCacheOptions()), NullLogger<MemoryCacheProvider>.Instance);
        var sut = new DefaultCacheKeyGenerator(Options_);
        var context = Context(userId, tenantId);
        var otherTenant = Context(userId, tenantId + "-other");

        var key = sut.GenerateKey<TenantKeyQuery, string>(new TenantKeyQuery("q"), context);
        var otherKey = sut.GenerateKey<TenantKeyQuery, string>(new TenantKeyQuery("q"), otherTenant);
        var templateKey = sut.GenerateKey<TemplateKeyQuery, string>(new TemplateKeyQuery("x"), context);
        await provider.SetAsync(key, "v", TimeSpan.FromMinutes(1), default);
        await provider.SetAsync(otherKey, "v", TimeSpan.FromMinutes(1), default);
        await provider.SetAsync(templateKey, "v", TimeSpan.FromMinutes(1), default);

        // GeneratePattern covers the tenant's default keys; GeneratePatternFromTemplate covers the template key.
        await provider.RemoveByPatternAsync(sut.GeneratePattern<TenantKeyQuery>(context), default);
        (await provider.ExistsAsync(key, default)).ShouldBeFalse();
        (await provider.ExistsAsync(otherKey, default)).ShouldBeTrue();
        (await provider.ExistsAsync(templateKey, default)).ShouldBeTrue();

        var templatePattern = sut.GeneratePatternFromTemplate("*items:*", new TemplateKeyQuery("x"), context);
        await provider.RemoveByPatternAsync(templatePattern, default);
        (await provider.ExistsAsync(templateKey, default)).ShouldBeFalse();
        (await provider.ExistsAsync(otherKey, default)).ShouldBeTrue();
    }
}
