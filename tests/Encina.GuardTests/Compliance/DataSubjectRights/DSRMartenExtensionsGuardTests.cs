using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DSRMartenExtensions"/> verifying null parameter handling.
/// </summary>
public class DSRMartenExtensionsGuardTests
{
    [Fact]
    public void AddDSRRequestAggregates_NullServices_ThrowsArgumentNullException()
    {
        var act = () => DSRMartenExtensions.AddDSRRequestAggregates(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }
}
