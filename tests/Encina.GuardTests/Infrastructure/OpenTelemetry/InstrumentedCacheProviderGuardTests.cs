using Encina.Caching;
using Encina.OpenTelemetry.QueryCache;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedCacheProvider"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedCacheProviderGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedCacheProvider(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        var inner = Substitute.For<ICacheProvider>();
        Should.NotThrow(() => new InstrumentedCacheProvider(inner));
    }
}
