using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard clause tests for <see cref="ResidencyPolicyBuilder"/> methods.
/// </summary>
public class ResidencyPolicyBuilderGuardTests
{
    [Fact]
    public void AllowRegions_NullRegions_ShouldThrow()
    {
        var options = new DataResidencyOptions();
        options.AddPolicy("data", builder =>
        {
            Should.Throw<ArgumentNullException>(() => builder.AllowRegions(null!));
            // Add at least one region so the policy is valid
            builder.AllowRegions(RegionRegistry.DE);
        });
    }

    [Fact]
    public void AllowTransferBasis_NullBases_ShouldThrow()
    {
        var options = new DataResidencyOptions();
        options.AddPolicy("data", builder =>
        {
            Should.Throw<ArgumentNullException>(() => builder.AllowTransferBasis(null!));
            builder.AllowRegions(RegionRegistry.DE);
        });
    }
}
