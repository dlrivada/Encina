using Encina.Compliance.DataSubjectRights;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

public class DataSubjectRightsOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new DataSubjectRightsOptions();

        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Block);
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void AllEnforcementModes_ShouldBeSettable()
    {
        var options = new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = DSREnforcementMode.Warn
        };
        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Warn);

        options.RestrictionEnforcementMode = DSREnforcementMode.Disabled;
        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Disabled);

        options.RestrictionEnforcementMode = DSREnforcementMode.Block;
        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Block);
    }

    [Fact]
    public void AddHealthCheck_ShouldBeSettable()
    {
        var options = new DataSubjectRightsOptions { AddHealthCheck = true };

        options.AddHealthCheck.ShouldBeTrue();
    }
}
