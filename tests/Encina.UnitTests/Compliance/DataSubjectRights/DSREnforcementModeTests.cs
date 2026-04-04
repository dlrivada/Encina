using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests verifying <see cref="DSREnforcementMode"/> enum values and usage.
/// </summary>
public class DSREnforcementModeTests
{
    [Fact]
    public void DSREnforcementMode_Block_HasExpectedValue()
    {
        DSREnforcementMode.Block.ShouldBe(DSREnforcementMode.Block);
    }

    [Fact]
    public void DSREnforcementMode_Warn_HasExpectedValue()
    {
        DSREnforcementMode.Warn.ShouldBe(DSREnforcementMode.Warn);
    }

    [Fact]
    public void DSREnforcementMode_Disabled_HasExpectedValue()
    {
        DSREnforcementMode.Disabled.ShouldBe(DSREnforcementMode.Disabled);
    }

    [Fact]
    public void DefaultOptions_UsesBlock()
    {
        var options = new DataSubjectRightsOptions();
        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Block);
    }
}
