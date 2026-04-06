using Encina.Compliance.CrossBorderTransfer;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="CrossBorderTransferMartenExtensions"/> verifying null parameter handling.
/// </summary>
public class CrossBorderTransferMartenExtensionsGuardTests
{
    [Fact]
    public void AddCrossBorderTransferAggregates_NullServices_ThrowsArgumentNullException()
    {
        var act = () => CrossBorderTransferMartenExtensions.AddCrossBorderTransferAggregates(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }
}
