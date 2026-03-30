using Encina.AspNetCore;
using Encina.Compliance.DataResidency;
using Microsoft.AspNetCore.Http;

namespace Encina.GuardTests.AspNetCore;

public class HttpRegionContextProviderGuardTests
{
    [Fact]
    public void Constructor_NullHttpContextAccessor_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new HttpRegionContextProvider(
                null!,
                Options.Create(new DataResidencyOptions()),
                Options.Create(new EncinaAspNetCoreOptions()),
                NullLogger<HttpRegionContextProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullDataResidencyOptions_Throws()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();

        Should.Throw<ArgumentNullException>(() =>
            new HttpRegionContextProvider(
                accessor,
                null!,
                Options.Create(new EncinaAspNetCoreOptions()),
                NullLogger<HttpRegionContextProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullAspNetCoreOptions_Throws()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();

        Should.Throw<ArgumentNullException>(() =>
            new HttpRegionContextProvider(
                accessor,
                Options.Create(new DataResidencyOptions()),
                null!,
                NullLogger<HttpRegionContextProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();

        Should.Throw<ArgumentNullException>(() =>
            new HttpRegionContextProvider(
                accessor,
                Options.Create(new DataResidencyOptions()),
                Options.Create(new EncinaAspNetCoreOptions()),
                null!));
    }
}
