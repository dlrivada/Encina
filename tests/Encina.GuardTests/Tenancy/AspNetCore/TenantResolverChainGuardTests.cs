using Encina.Tenancy;
using Encina.Tenancy.AspNetCore;

using Microsoft.AspNetCore.Http;

namespace Encina.GuardTests.Tenancy.AspNetCore;

/// <summary>
/// Guard tests for <see cref="TenantResolverChain"/>.
/// </summary>
public class TenantResolverChainGuardTests
{
    [Fact]
    public void Constructor_NullResolvers_ThrowsArgumentNullException()
    {
        var act = () => new TenantResolverChain(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("resolvers");
    }

    [Fact]
    public void Constructor_EmptyResolvers_HasZeroCount()
    {
        var sut = new TenantResolverChain([]);

        sut.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ResolveAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = new TenantResolverChain([]);

        var act = async () => await sut.ResolveAsync(null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task ResolveAsync_NoResolvers_ReturnsNone()
    {
        var sut = new TenantResolverChain([]);

        var result = await sut.ResolveAsync(new DefaultHttpContext());

        result.ShouldBe(TenantResolution.None);
    }
}
