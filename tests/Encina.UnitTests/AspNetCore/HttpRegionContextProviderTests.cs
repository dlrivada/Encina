using Encina.AspNetCore;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="HttpRegionContextProvider"/>.
/// </summary>
public sealed class HttpRegionContextProviderTests
{
    private static HttpRegionContextProvider CreateProvider(
        HttpContext? httpContext = null,
        DataResidencyOptions? residency = null,
        EncinaAspNetCoreOptions? aspNetOptions = null)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new HttpRegionContextProvider(
            accessor,
            Options.Create(residency ?? new DataResidencyOptions()),
            Options.Create(aspNetOptions ?? new EncinaAspNetCoreOptions()),
            NullLogger<HttpRegionContextProvider>.Instance);
    }

    [Fact]
    public async Task GetCurrentRegionAsync_HeaderPresent_KnownRegion_ReturnsRegistryRegion()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Data-Region"] = "DE";
        var provider = CreateProvider(http);

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.Code.ShouldBe("DE");
                r.IsEU.ShouldBeTrue();
            },
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_HeaderPresent_KnownRegion_LowerCase_ReturnsRegistryRegion()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Data-Region"] = "de";
        var provider = CreateProvider(http);

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Code.ShouldBe("DE"),
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_HeaderPresent_UnknownRegion_ReturnsCustomRegion()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Data-Region"] = "AZURE-WESTEU";
        var provider = CreateProvider(http);

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.Code.ShouldBe("AZURE-WESTEU");
                r.ProtectionLevel.ShouldBe(DataProtectionLevel.Unknown);
            },
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_HeaderPresent_TwoLetterUnknown_UsesCodeAsCountry()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Data-Region"] = "ZZ";
        var provider = CreateProvider(http);

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.Code.ShouldBe("ZZ");
                r.Country.ShouldBe("ZZ");
            },
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_HeaderWhitespaceTrimmed()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Data-Region"] = "  DE  ";
        var provider = CreateProvider(http);

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Code.ShouldBe("DE"),
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_HeaderEmpty_FallsBackToDefault()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Data-Region"] = "";
        var provider = CreateProvider(http, new DataResidencyOptions { DefaultRegion = RegionRegistry.US });

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Code.ShouldBe("US"),
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_NoHeader_FallsBackToDefault()
    {
        var http = new DefaultHttpContext();
        var provider = CreateProvider(http, new DataResidencyOptions { DefaultRegion = RegionRegistry.FR });

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Code.ShouldBe("FR"),
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_NoHttpContext_FallsBackToDefault()
    {
        var provider = CreateProvider(httpContext: null,
            residency: new DataResidencyOptions { DefaultRegion = RegionRegistry.JP });

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Code.ShouldBe("JP"),
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }

    [Fact]
    public async Task GetCurrentRegionAsync_NoHeaderNoDefault_ReturnsLeftError()
    {
        var http = new DefaultHttpContext();
        var provider = CreateProvider(http, new DataResidencyOptions { DefaultRegion = null });

        var result = await provider.GetCurrentRegionAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentRegionAsync_NoHttpContextNoDefault_ReturnsLeftError()
    {
        var provider = CreateProvider(httpContext: null,
            residency: new DataResidencyOptions { DefaultRegion = null });

        var result = await provider.GetCurrentRegionAsync();

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentRegionAsync_CustomHeaderName_ReadsFromCustomHeader()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-My-Region"] = "ES";
        var provider = CreateProvider(
            http,
            aspNetOptions: new EncinaAspNetCoreOptions { DataRegionHeaderName = "X-My-Region" });

        var result = await provider.GetCurrentRegionAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Code.ShouldBe("ES"),
            Left: _ => throw new Xunit.Sdk.XunitException("Expected Right"));
    }
}
