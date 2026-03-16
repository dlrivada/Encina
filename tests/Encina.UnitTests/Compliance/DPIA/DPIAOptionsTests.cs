#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAOptions"/>.
/// </summary>
public class DPIAOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Defaults_EnforcementMode_IsWarn()
    {
        var options = new DPIAOptions();

        options.EnforcementMode.Should().Be(DPIAEnforcementMode.Warn);
    }

    [Fact]
    public void Defaults_DefaultReviewPeriod_Is365Days()
    {
        var options = new DPIAOptions();

        options.DefaultReviewPeriod.Should().Be(TimeSpan.FromDays(365));
    }

    [Fact]
    public void Defaults_PublishNotifications_IsTrue()
    {
        var options = new DPIAOptions();

        options.PublishNotifications.Should().BeTrue();
    }

    [Fact]
    public void Defaults_EnableExpirationMonitoring_IsFalse()
    {
        var options = new DPIAOptions();

        options.EnableExpirationMonitoring.Should().BeFalse();
    }

    [Fact]
    public void Defaults_ExpirationCheckInterval_Is1Hour()
    {
        var options = new DPIAOptions();

        options.ExpirationCheckInterval.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Defaults_AutoRegisterFromAttributes_IsFalse()
    {
        var options = new DPIAOptions();

        options.AutoRegisterFromAttributes.Should().BeFalse();
    }

    [Fact]
    public void Defaults_AutoDetectHighRisk_IsFalse()
    {
        var options = new DPIAOptions();

        options.AutoDetectHighRisk.Should().BeFalse();
    }

    [Fact]
    public void Defaults_AssembliesToScan_IsEmpty()
    {
        var options = new DPIAOptions();

        options.AssembliesToScan.Should().BeEmpty();
    }

    [Fact]
    public void Defaults_DPOEmail_IsNull()
    {
        var options = new DPIAOptions();

        options.DPOEmail.Should().BeNull();
    }

    [Fact]
    public void Defaults_DPOName_IsNull()
    {
        var options = new DPIAOptions();

        options.DPOName.Should().BeNull();
    }

    [Fact]
    public void Defaults_AddHealthCheck_IsFalse()
    {
        var options = new DPIAOptions();

        options.AddHealthCheck.Should().BeFalse();
    }

    #endregion

    #region BlockWithoutDPIA Tests

    [Fact]
    public void BlockWithoutDPIA_Get_WhenBlockMode_ReturnsTrue()
    {
        var options = new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block };

        options.BlockWithoutDPIA.Should().BeTrue();
    }

    [Fact]
    public void BlockWithoutDPIA_Get_WhenWarnMode_ReturnsFalse()
    {
        var options = new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Warn };

        options.BlockWithoutDPIA.Should().BeFalse();
    }

    [Fact]
    public void BlockWithoutDPIA_SetTrue_SetsEnforcementModeToBlock()
    {
        var options = new DPIAOptions();

        options.BlockWithoutDPIA = true;

        options.EnforcementMode.Should().Be(DPIAEnforcementMode.Block);
    }

    [Fact]
    public void BlockWithoutDPIA_SetFalse_DoesNotChangeEnforcementMode()
    {
        var options = new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block };

        options.BlockWithoutDPIA = false;

        // Setting false does NOT change mode (by design)
        options.EnforcementMode.Should().Be(DPIAEnforcementMode.Block);
    }

    #endregion
}
