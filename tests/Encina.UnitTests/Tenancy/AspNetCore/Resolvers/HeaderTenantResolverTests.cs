using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Tenancy.AspNetCore.Resolvers;

/// <summary>
/// Unit tests for HeaderTenantResolver.
/// </summary>
public class HeaderTenantResolverTests
{
    private readonly HeaderTenantResolver _resolver;
    private readonly TenancyAspNetCoreOptions _options;
    private readonly FakeLogger<HeaderTenantResolver> _logger = new();

    public HeaderTenantResolverTests()
    {
        _options = new TenancyAspNetCoreOptions();
        _options.HeaderResolver.Enabled = true;
        _options.HeaderResolver.TrustAnyPeer = true;
        _resolver = new HeaderTenantResolver(Options.Create(_options), _logger);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new HeaderTenantResolver(null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new HeaderTenantResolver(Options.Create(_options), null!));
    }

    [Fact]
    public void Priority_ReturnsConfiguredPriority()
    {
        _resolver.Priority.ShouldBe(HeaderTenantResolver.DefaultPriority);
    }

    [Fact]
    public void IsAuthoritative_IsFalse()
    {
        _resolver.IsAuthoritative.ShouldBeFalse();
    }

    [Fact]
    public void HeaderResolverOptions_AreOffAndUntrustedByDefault()
    {
        var defaults = new TenancyAspNetCoreOptions().HeaderResolver;

        defaults.Enabled.ShouldBeFalse();
        defaults.TrustAnyPeer.ShouldBeFalse();
        defaults.TrustedProxies.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_WithValidHeader_ReturnsTenantId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "acme-corp";

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBe("acme-corp");
    }

    [Fact]
    public async Task ResolveAsync_WithCustomHeaderName_ReturnsTenantId()
    {
        _options.HeaderResolver.HeaderName = "X-Organization-ID";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Organization-ID"] = "contoso";

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBe("contoso");
    }

    [Fact]
    public async Task ResolveAsync_HeaderNotPresent_ReturnsNull()
    {
        var result = await _resolver.ResolveAsync(new DefaultHttpContext());

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_EmptyOrWhitespaceHeader_ReturnsNull(string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = value;

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsNull()
    {
        _options.HeaderResolver.Enabled = false;
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "acme-corp";

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_NullContext_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () => await _resolver.ResolveAsync(null!));
    }

    [Fact]
    public void Priority_WithCustomValue_ReturnsCustomValue()
    {
        _options.HeaderResolver.Priority = 50;
        var resolver = new HeaderTenantResolver(Options.Create(_options), _logger);

        resolver.Priority.ShouldBe(50);
    }

    [Fact]
    public async Task ResolveAsync_PeerInTrustedProxy_ReturnsHeader()
    {
        _options.HeaderResolver.TrustAnyPeer = false;
        _options.HeaderResolver.TrustedProxies.Add(IPNetwork.Parse("10.0.0.0/24"));
        var context = ContextFrom("10.0.0.7", "acme");

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBe("acme");
    }

    [Fact]
    public async Task ResolveAsync_PeerIsIPv4MappedIPv6InTrustedProxy_ReturnsHeader()
    {
        _options.HeaderResolver.TrustAnyPeer = false;
        _options.HeaderResolver.TrustedProxies.Add(IPNetwork.Parse("10.0.0.0/24"));
        var context = ContextFrom("::ffff:10.0.0.7", "acme");

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBe("acme");
    }

    [Fact]
    public async Task ResolveAsync_PeerOutsideTrustedProxies_IgnoresHeaderAndLogsDebug()
    {
        _options.HeaderResolver.TrustAnyPeer = false;
        _options.HeaderResolver.TrustedProxies.Add(IPNetwork.Parse("10.0.0.0/24"));
        var context = ContextFrom("203.0.113.9", "acme");

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBeNull();
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Debug);
        record.Id.Id.ShouldBe(3850);
        record.Message.ShouldNotContain("acme");
    }

    [Fact]
    public async Task ResolveAsync_NoPeerAddressAndNothingTrusted_FailsClosed()
    {
        _options.HeaderResolver.TrustAnyPeer = false;
        _options.HeaderResolver.TrustedProxies.Add(IPNetwork.Parse("0.0.0.0/0"));
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "acme";

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_TrustAnyPeer_HonorsHeaderWithoutPeerAddress()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "acme";

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBe("acme");
        _logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ResolveAsync_NoTrustConfigured_IgnoresHeader()
    {
        _options.HeaderResolver.TrustAnyPeer = false;
        var context = ContextFrom("10.0.0.7", "acme");

        var result = await _resolver.ResolveAsync(context);

        result.ShouldBeNull();
    }

    private static DefaultHttpContext ContextFrom(string peer, string tenant)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Headers["X-Tenant-ID"] = tenant;
        return context;
    }
}
