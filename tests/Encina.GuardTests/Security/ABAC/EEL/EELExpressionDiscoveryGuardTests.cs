using System.Reflection;

using Encina.Security.ABAC.EEL;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.EEL;

/// <summary>
/// Guard clause tests for <see cref="EELExpressionDiscovery"/> (internal, tested via public APIs).
/// </summary>
public class EELExpressionDiscoveryGuardTests
{
    [Fact]
    public void Discover_NullAssemblies_ThrowsArgumentNullException()
    {
        var act = () => EELExpressionDiscovery.Discover(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("assemblies");
    }

    [Fact]
    public void Discover_EmptyAssemblies_ReturnsEmptyList()
    {
        var result = EELExpressionDiscovery.Discover(Enumerable.Empty<Assembly>());
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void Discover_AssemblyWithNoAttributes_ReturnsEmptyList()
    {
        // System.Object assembly has no RequireConditionAttribute
        var result = EELExpressionDiscovery.Discover([typeof(object).Assembly]);
        result.Count.ShouldBe(0);
    }
}
