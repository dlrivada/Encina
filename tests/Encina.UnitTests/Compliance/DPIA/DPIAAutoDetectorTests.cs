#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using Shouldly;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAAutoDetector"/>.
/// </summary>
public class DPIAAutoDetectorTests
{
    private readonly DPIAAutoDetector _sut = new(NullLogger.Instance);

    #region DetectHighRiskTriggers Tests

    [Fact]
    public void DetectHighRiskTriggers_TypeWithNoKeywords_ReturnsEmpty()
    {
        var triggers = _sut.DetectHighRiskTriggers(typeof(SimpleCommand));

        triggers.ShouldBeEmpty();
    }

    [Fact]
    public void DetectHighRiskTriggers_TypeWithSingleKeyword_ReturnsEmpty()
    {
        // Only 1 trigger — below the minimum of 2
        var triggers = _sut.DetectHighRiskTriggers(typeof(BiometricOnlyCommand));

        triggers.ShouldBeEmpty();
    }

    [Fact]
    public void DetectHighRiskTriggers_TypeWithTwoKeywords_ReturnsTriggers()
    {
        var triggers = _sut.DetectHighRiskTriggers(typeof(BiometricHealthCommand));

        triggers.Count.ShouldBeGreaterThanOrEqualTo(2);
        triggers.ShouldContain(HighRiskTriggers.BiometricData);
        triggers.ShouldContain(HighRiskTriggers.HealthData);
    }

    [Fact]
    public void DetectHighRiskTriggers_PropertyNameKeywords_Detected()
    {
        var triggers = _sut.DetectHighRiskTriggers(typeof(CommandWithHighRiskProperties));

        triggers.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void DetectHighRiskTriggers_CaseInsensitive_Matches()
    {
        // "BIOMETRIC" should match case-insensitively
        var triggers = _sut.DetectHighRiskTriggers(typeof(BiometricHealthCommand));

        triggers.ShouldContain(HighRiskTriggers.BiometricData);
    }

    #endregion

    #region IsHighRisk Tests

    [Fact]
    public void IsHighRisk_BelowThreshold_ReturnsFalse()
    {
        _sut.IsHighRisk(typeof(SimpleCommand)).ShouldBeFalse();
    }

    [Fact]
    public void IsHighRisk_AtOrAboveThreshold_ReturnsTrue()
    {
        _sut.IsHighRisk(typeof(BiometricHealthCommand)).ShouldBeTrue();
    }

    [Fact]
    public void IsHighRisk_SingleTrigger_ReturnsFalse()
    {
        _sut.IsHighRisk(typeof(BiometricOnlyCommand)).ShouldBeFalse();
    }

    #endregion

    #region Test Types

    private sealed class SimpleCommand;

    private sealed class BiometricOnlyCommand;

    private sealed class BiometricHealthCommand;

    private sealed class CommandWithHighRiskProperties
    {
        public string? BiometricData { get; set; }
        public string? HealthRecord { get; set; }
    }

    #endregion
}
