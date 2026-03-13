#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorAgreementOptions"/>.
/// </summary>
public class ProcessorAgreementOptionsTests
{
    #region Default Values

    [Fact]
    public void Defaults_EnforcementMode_ShouldBeWarn()
    {
        var options = new ProcessorAgreementOptions();
        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Warn);
    }

    [Fact]
    public void Defaults_MaxSubProcessorDepth_ShouldBe3()
    {
        var options = new ProcessorAgreementOptions();
        options.MaxSubProcessorDepth.Should().Be(3);
    }

    [Fact]
    public void Defaults_EnableExpirationMonitoring_ShouldBeFalse()
    {
        var options = new ProcessorAgreementOptions();
        options.EnableExpirationMonitoring.Should().BeFalse();
    }

    [Fact]
    public void Defaults_ExpirationCheckInterval_ShouldBeOneHour()
    {
        var options = new ProcessorAgreementOptions();
        options.ExpirationCheckInterval.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Defaults_ExpirationWarningDays_ShouldBe30()
    {
        var options = new ProcessorAgreementOptions();
        options.ExpirationWarningDays.Should().Be(30);
    }

    [Fact]
    public void Defaults_AddHealthCheck_ShouldBeFalse()
    {
        var options = new ProcessorAgreementOptions();
        options.AddHealthCheck.Should().BeFalse();
    }

    #endregion

    #region BlockWithoutValidDPA

    [Fact]
    public void BlockWithoutValidDPA_SetTrue_ShouldSetEnforcementModeToBlock()
    {
        var options = new ProcessorAgreementOptions();
        options.BlockWithoutValidDPA = true;
        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Block);
    }

    [Fact]
    public void BlockWithoutValidDPA_Get_WhenBlock_ShouldReturnTrue()
    {
        var options = new ProcessorAgreementOptions { EnforcementMode = ProcessorAgreementEnforcementMode.Block };
        options.BlockWithoutValidDPA.Should().BeTrue();
    }

    [Fact]
    public void BlockWithoutValidDPA_Get_WhenWarn_ShouldReturnFalse()
    {
        var options = new ProcessorAgreementOptions { EnforcementMode = ProcessorAgreementEnforcementMode.Warn };
        options.BlockWithoutValidDPA.Should().BeFalse();
    }

    #endregion

    #region EnforcementMode Enum Values

    [Theory]
    [InlineData(ProcessorAgreementEnforcementMode.Block, 0)]
    [InlineData(ProcessorAgreementEnforcementMode.Warn, 1)]
    [InlineData(ProcessorAgreementEnforcementMode.Disabled, 2)]
    public void EnforcementMode_ShouldHaveExpectedIntValue(ProcessorAgreementEnforcementMode mode, int expected)
    {
        ((int)mode).Should().Be(expected);
    }

    #endregion
}
