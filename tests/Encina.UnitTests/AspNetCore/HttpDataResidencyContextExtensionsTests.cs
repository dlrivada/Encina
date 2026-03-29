using Encina.AspNetCore;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="HttpDataResidencyContextExtensions"/>.
/// </summary>
public sealed class HttpDataResidencyContextExtensionsTests
{
    [Fact]
    public void DataRegionKey_ShouldBeCorrectValue()
    {
        HttpDataResidencyContextExtensions.DataRegionKey.ShouldBe("Encina.DataResidency.Region");
    }

    [Fact]
    public void GetDataRegion_NullContext_Throws()
    {
        IRequestContext? ctx = null;
        Should.Throw<ArgumentNullException>(() => ctx!.GetDataRegion());
    }

    [Fact]
    public void GetDataRegion_NoRegionSet_ReturnsNull()
    {
        var ctx = RequestContext.CreateForTest();
        ctx.GetDataRegion().ShouldBeNull();
    }

    [Fact]
    public void GetDataRegion_RegionSet_ReturnsValue()
    {
        var ctx = RequestContext.CreateForTest().WithDataRegion("DE");
        ctx.GetDataRegion().ShouldBe("DE");
    }

    [Fact]
    public void WithDataRegion_NullContext_Throws()
    {
        IRequestContext? ctx = null;
        Should.Throw<ArgumentNullException>(() => ctx!.WithDataRegion("DE"));
    }

    [Fact]
    public void WithDataRegion_ValidRegion_ReturnsNewContextWithRegion()
    {
        var original = RequestContext.CreateForTest();
        var withRegion = original.WithDataRegion("US");

        withRegion.GetDataRegion().ShouldBe("US");
        original.GetDataRegion().ShouldBeNull(); // immutable - original unchanged
    }

    [Fact]
    public void WithDataRegion_NullRegion_SetsNullValue()
    {
        var ctx = RequestContext.CreateForTest().WithDataRegion(null);
        ctx.GetDataRegion().ShouldBeNull();
    }

    [Fact]
    public void WithDataRegion_EmptyRegion_SetsEmptyValue()
    {
        var ctx = RequestContext.CreateForTest().WithDataRegion("");
        // Empty string stored, GetDataRegion returns it as-is
        ctx.GetDataRegion().ShouldBeNullOrEmpty();
    }

    [Fact]
    public void WithDataRegion_OverridesPrevious()
    {
        var ctx = RequestContext.CreateForTest()
            .WithDataRegion("DE")
            .WithDataRegion("FR");
        ctx.GetDataRegion().ShouldBe("FR");
    }
}
