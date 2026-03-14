using Encina.Compliance.CrossBorderTransfer.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="TransferValidationOutcome"/> verifying
/// invariants of factory methods across randomized inputs using FsCheck.
/// </summary>
public class TransferValidationOutcomePropertyTests
{
    #region Allow Invariants

    /// <summary>
    /// Invariant: Allow with any non-Blocked basis always produces IsAllowed=true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Allow_IsAlwaysAllowed(PositiveInt basisIndex)
    {
        // Select a non-Blocked basis
        var nonBlockedBases = new[]
        {
            TransferBasis.AdequacyDecision,
            TransferBasis.SCCs,
            TransferBasis.BindingCorporateRules,
            TransferBasis.Derogation
        };
        var basis = nonBlockedBases[basisIndex.Get % nonBlockedBases.Length];

        var outcome = TransferValidationOutcome.Allow(basis);

        return outcome.IsAllowed &&
               outcome.Basis == basis &&
               outcome.BlockReason is null;
    }

    #endregion

    #region Block Invariants

    /// <summary>
    /// Invariant: Block always produces IsAllowed=false with Blocked basis.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Block_IsNeverAllowed(NonEmptyString reason)
    {
        var blockReason = reason.Get.Trim();
        if (string.IsNullOrWhiteSpace(blockReason))
        {
            return true; // Skip whitespace-only
        }

        var outcome = TransferValidationOutcome.Block(blockReason);

        return !outcome.IsAllowed &&
               outcome.Basis == TransferBasis.Blocked &&
               outcome.BlockReason == blockReason &&
               !outcome.TIARequired;
    }

    #endregion

    #region Warnings Invariants

    /// <summary>
    /// Invariant: Allow with warnings preserves all warnings in the outcome.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Allow_WithWarnings_PreservesWarnings(PositiveInt warningCount)
    {
        var count = Math.Min(warningCount.Get, 20); // Cap at reasonable number
        var warnings = Enumerable.Range(1, count)
            .Select(i => $"Warning {i}")
            .ToList()
            .AsReadOnly();

        var outcome = TransferValidationOutcome.Allow(
            TransferBasis.SCCs,
            warnings: warnings);

        return outcome.IsAllowed &&
               outcome.Warnings.Count == count &&
               outcome.Warnings.SequenceEqual(warnings);
    }

    #endregion
}
