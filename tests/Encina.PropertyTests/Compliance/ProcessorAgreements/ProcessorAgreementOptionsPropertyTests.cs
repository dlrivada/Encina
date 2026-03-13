using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="ProcessorAgreementOptions"/> verifying configuration
/// invariants using FsCheck random data generation.
/// </summary>
public class ProcessorAgreementOptionsPropertyTests
{
    #region BlockWithoutValidDPA Invariants

    /// <summary>
    /// Invariant: BlockWithoutValidDPA getter returns true if and only if EnforcementMode is Block.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BlockWithoutValidDPA_Get_TrueIffEnforcementModeIsBlock()
    {
        var allModes = Enum.GetValues<ProcessorAgreementEnforcementMode>();

        return allModes.All(mode =>
        {
            var options = new ProcessorAgreementOptions { EnforcementMode = mode };
            return options.BlockWithoutValidDPA == (mode == ProcessorAgreementEnforcementMode.Block);
        });
    }

    /// <summary>
    /// Invariant: Setting BlockWithoutValidDPA to true sets EnforcementMode to Block.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BlockWithoutValidDPA_SetTrue_SetsEnforcementModeToBlock()
    {
        var options = new ProcessorAgreementOptions();
        options.BlockWithoutValidDPA = true;
        return options.EnforcementMode == ProcessorAgreementEnforcementMode.Block;
    }

    /// <summary>
    /// Invariant: Setting BlockWithoutValidDPA to false does not change EnforcementMode
    /// (the setter only acts on true).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BlockWithoutValidDPA_SetFalse_DoesNotChangeEnforcementMode()
    {
        var allModes = Enum.GetValues<ProcessorAgreementEnforcementMode>();

        return allModes.All(mode =>
        {
            var options = new ProcessorAgreementOptions { EnforcementMode = mode };
            options.BlockWithoutValidDPA = false;
            return options.EnforcementMode == mode;
        });
    }

    /// <summary>
    /// Invariant: After setting BlockWithoutValidDPA to true, the getter always returns true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BlockWithoutValidDPA_SetTrueThenGet_AlwaysTrue()
    {
        var allModes = Enum.GetValues<ProcessorAgreementEnforcementMode>();

        return allModes.All(initialMode =>
        {
            var options = new ProcessorAgreementOptions { EnforcementMode = initialMode };
            options.BlockWithoutValidDPA = true;
            return options.BlockWithoutValidDPA;
        });
    }

    #endregion

    #region Default Values Invariants

    /// <summary>
    /// Invariant: Default EnforcementMode is Warn.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DefaultEnforcementMode_IsWarn()
    {
        var options = new ProcessorAgreementOptions();
        return options.EnforcementMode == ProcessorAgreementEnforcementMode.Warn;
    }

    /// <summary>
    /// Invariant: Default MaxSubProcessorDepth is 3.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DefaultMaxSubProcessorDepth_IsThree()
    {
        var options = new ProcessorAgreementOptions();
        return options.MaxSubProcessorDepth == 3;
    }

    /// <summary>
    /// Invariant: Default ExpirationWarningDays is 30.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DefaultExpirationWarningDays_IsThirty()
    {
        var options = new ProcessorAgreementOptions();
        return options.ExpirationWarningDays == 30;
    }

    /// <summary>
    /// Invariant: Default TrackAuditTrail is true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DefaultTrackAuditTrail_IsTrue()
    {
        var options = new ProcessorAgreementOptions();
        return options.TrackAuditTrail;
    }

    #endregion

    #region MaxSubProcessorDepth Invariants

    /// <summary>
    /// Invariant: MaxSubProcessorDepth preserves the assigned positive value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MaxSubProcessorDepth_SetValue_AlwaysPreserved(PositiveInt depth)
    {
        var clampedDepth = Math.Min(depth.Get, 10);
        var options = new ProcessorAgreementOptions { MaxSubProcessorDepth = clampedDepth };
        return options.MaxSubProcessorDepth == clampedDepth;
    }

    #endregion

    #region ExpirationWarningDays Invariants

    /// <summary>
    /// Invariant: ExpirationWarningDays preserves the assigned positive value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ExpirationWarningDays_SetValue_AlwaysPreserved(PositiveInt days)
    {
        var options = new ProcessorAgreementOptions { ExpirationWarningDays = days.Get };
        return options.ExpirationWarningDays == days.Get;
    }

    #endregion
}
