using Encina.Compliance.DataResidency.Model;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard clause tests for <see cref="RegionRegistry"/>.
/// </summary>
public class RegionRegistryGuardTests
{
    [Fact]
    public void GetByCode_NullCode_ShouldThrow()
    {
        var act = () => RegionRegistry.GetByCode(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("code");
    }
}
