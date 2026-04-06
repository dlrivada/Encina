using Encina.Compliance.DataResidency;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard tests for <see cref="DataResidencyMartenExtensions"/> verifying null parameter handling.
/// </summary>
public class DataResidencyMartenExtensionsGuardTests
{
    [Fact]
    public void AddDataResidencyAggregates_NullServices_ThrowsArgumentNullException()
    {
        var act = () => DataResidencyMartenExtensions.AddDataResidencyAggregates(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }
}
