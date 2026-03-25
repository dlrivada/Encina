using Encina.Compliance.DataSubjectRights;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

public class DataSubjectRightsOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new DataSubjectRightsOptions();

        options.RestrictionEnforcementMode.Should().Be(DSREnforcementMode.Block);
        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void AllEnforcementModes_ShouldBeSettable()
    {
        var options = new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = DSREnforcementMode.Warn
        };
        options.RestrictionEnforcementMode.Should().Be(DSREnforcementMode.Warn);

        options.RestrictionEnforcementMode = DSREnforcementMode.Disabled;
        options.RestrictionEnforcementMode.Should().Be(DSREnforcementMode.Disabled);

        options.RestrictionEnforcementMode = DSREnforcementMode.Block;
        options.RestrictionEnforcementMode.Should().Be(DSREnforcementMode.Block);
    }

    [Fact]
    public void AddHealthCheck_ShouldBeSettable()
    {
        var options = new DataSubjectRightsOptions { AddHealthCheck = true };

        options.AddHealthCheck.Should().BeTrue();
    }
}
