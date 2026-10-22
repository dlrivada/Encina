using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.Tenancy.AspNetCore;

/// <summary>
/// Unit tests for TenantResolverChain.
/// </summary>
public class TenantResolverChainTests
{
    [Fact]
    public void Constructor_NullResolvers_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new TenantResolverChain(null!));
    }

    [Fact]
    public void Constructor_EmptyResolvers_CreatesEmptyChain()
    {
        new TenantResolverChain([]).Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_CountsBothGroups()
    {
        var chain = new TenantResolverChain([new TestResolver(100, "a"), new TestResolver(10, "b", authoritative: true)]);

        chain.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ResolveAsync_NullContext_ThrowsArgumentNullException()
    {
        var chain = new TenantResolverChain([]);

        await Should.ThrowAsync<ArgumentNullException>(async () => await chain.ResolveAsync(null!));
    }

    [Fact]
    public async Task ResolveAsync_EmptyChain_ReturnsNone()
    {
        var result = await new TenantResolverChain([]).ResolveAsync(new DefaultHttpContext());

        result.ShouldBe(TenantResolution.None);
        result.TenantId.ShouldBeNull();
        result.IsMismatch.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_NoAuthoritative_FirstNonEmptyByPriorityWins()
    {
        var chain = new TenantResolverChain([new TestResolver(20, "second"), new TestResolver(10, "first")]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("first");
        result.AuthoritativeResolver.ShouldBeNull();
        result.IsMismatch.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_NoAuthoritative_SkipsEmptyValues(string? empty)
    {
        var chain = new TenantResolverChain([new TestResolver(10, empty), new TestResolver(20, "second")]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("second");
    }

    [Fact]
    public async Task ResolveAsync_NoAuthoritative_AllEmpty_ReturnsNone()
    {
        var chain = new TenantResolverChain([new TestResolver(10, null), new TestResolver(20, null)]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.ShouldBe(TenantResolution.None);
    }

    [Fact]
    public async Task ResolveAsync_NoAuthoritative_DisagreeingOthers_IsNotAMismatch()
    {
        var chain = new TenantResolverChain([new TestResolver(10, "A"), new TestResolver(20, "B")]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("A");
        result.IsMismatch.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_NoAuthoritative_StopsAtFirstValue()
    {
        var order = new List<int>();
        var chain = new TenantResolverChain(
        [
            new TrackingResolver(10, null, order),
            new TrackingResolver(20, "found", order),
            new TrackingResolver(30, "not-reached", order),
        ]);

        await chain.ResolveAsync(new DefaultHttpContext());

        order.ShouldBe([10, 20]);
    }

    [Fact]
    public async Task ResolveAsync_AuthoritativeBeatsLowerPriorityNonAuthoritative_WhenValuesAgree()
    {
        // The header-like resolver has the lower priority number (runs "first" in the old chain)
        var chain = new TenantResolverChain([new TestResolver(100, "A"), new TestResolver(110, "A", authoritative: true)]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("A");
        result.AuthoritativeResolver.ShouldBe(nameof(TestResolver));
        result.IsMismatch.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_AuthoritativeOnly_ReturnsItsTenant()
    {
        var chain = new TenantResolverChain([new TestResolver(110, "A", authoritative: true), new TestResolver(100, null)]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("A");
        result.IsMismatch.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_ClaimAndDifferingHeader_IsMismatchWithoutTenant()
    {
        var chain = new TenantResolverChain([new TestResolver(100, "B"), new TestResolver(110, "A", authoritative: true)]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.IsMismatch.ShouldBeTrue();
        result.TenantId.ShouldBeNull();
        result.AuthoritativeResolver.ShouldBe(nameof(TestResolver));
        result.ConflictingResolver.ShouldBe(nameof(TestResolver));
    }

    [Fact]
    public async Task ResolveAsync_ValuesDifferOnlyByCase_IsMismatch()
    {
        var chain = new TenantResolverChain([new TestResolver(100, "acme"), new TestResolver(110, "ACME", authoritative: true)]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.IsMismatch.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveAsync_AuthoritativeOrderedByPriorityAmongThemselves()
    {
        var chain = new TenantResolverChain(
        [
            new TestResolver(30, "late", authoritative: true),
            new TestResolver(10, "early", authoritative: true),
        ]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("early");
    }

    [Fact]
    public async Task ResolveAsync_EmptyAuthoritative_FallsBackToOthers()
    {
        var chain = new TenantResolverChain([new TestResolver(110, null, authoritative: true), new TestResolver(100, "B")]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("B");
        result.IsMismatch.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_WithAuthoritative_EveryNonAuthoritativeStillRuns()
    {
        var order = new List<int>();
        var chain = new TenantResolverChain(
        [
            new TrackingResolver(10, null, order),
            new TrackingResolver(20, "A", order),
            new TrackingResolver(30, null, order),
            new TrackingResolver(5, "A", order, authoritative: true),
        ]);

        var result = await chain.ResolveAsync(new DefaultHttpContext());

        result.TenantId.ShouldBe("A");
        order.ShouldBe([5, 10, 20, 30]);
    }

    private sealed class TestResolver(int priority, string? tenantId, bool authoritative = false) : ITenantResolver
    {
        public int Priority { get; } = priority;

        public bool IsAuthoritative { get; } = authoritative;

        public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(tenantId);
    }

    private sealed class TrackingResolver(int priority, string? tenantId, List<int> order, bool authoritative = false) : ITenantResolver
    {
        public int Priority { get; } = priority;

        public bool IsAuthoritative { get; } = authoritative;

        public ValueTask<string?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            order.Add(Priority);
            return ValueTask.FromResult(tenantId);
        }
    }
}
