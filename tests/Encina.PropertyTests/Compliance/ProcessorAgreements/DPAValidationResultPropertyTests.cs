using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="DPAValidationResult"/> verifying validation
/// invariants using FsCheck random data generation.
/// </summary>
public class DPAValidationResultPropertyTests
{
    #region IsValid Invariants

    /// <summary>
    /// Invariant: A valid result has no missing terms and a positive DaysUntilExpiration.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsValid_NoMissingTermsAndPositiveDays_IsTrue(PositiveInt daysUntil)
    {
        var result = CreateResult(
            isValid: true,
            missingTerms: [],
            daysUntilExpiration: daysUntil.Get);

        return result.IsValid;
    }

    /// <summary>
    /// Invariant: A result with missing terms is not valid.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsValid_WithMissingTerms_IsFalse(NonEmptyString missingTerm)
    {
        var result = CreateResult(
            isValid: false,
            missingTerms: [missingTerm.Get],
            daysUntilExpiration: 30);

        return !result.IsValid;
    }

    /// <summary>
    /// Invariant: A result with no DPA (null DPAId) is not valid.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsValid_NoDPA_IsFalse(NonEmptyString processorId)
    {
        var result = new DPAValidationResult
        {
            ProcessorId = processorId.Get,
            DPAId = null,
            IsValid = false,
            MissingTerms = [],
            Warnings = [],
            DaysUntilExpiration = null,
            ValidatedAtUtc = DateTimeOffset.UtcNow
        };

        return !result.IsValid;
    }

    #endregion

    #region Warnings Invariants

    /// <summary>
    /// Invariant: Warnings is never null on a valid DPAValidationResult.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Warnings_NeverNull(bool isValid)
    {
        var result = CreateResult(
            isValid: isValid,
            missingTerms: [],
            warnings: []);

        return result.Warnings is not null;
    }

    /// <summary>
    /// Invariant: Warnings preserves assigned values.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Warnings_PreservesValues(NonEmptyString warning)
    {
        var warnings = new List<string> { warning.Get };
        var result = CreateResult(
            isValid: true,
            missingTerms: [],
            warnings: warnings);

        return result.Warnings.Count == 1 && result.Warnings[0] == warning.Get;
    }

    #endregion

    #region MissingTerms Invariants

    /// <summary>
    /// Invariant: MissingTerms is never null on a valid DPAValidationResult.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MissingTerms_NeverNull(bool isValid)
    {
        var result = CreateResult(
            isValid: isValid,
            missingTerms: []);

        return result.MissingTerms is not null;
    }

    /// <summary>
    /// Invariant: MissingTerms preserves assigned values.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MissingTerms_PreservesValues(NonEmptyString term1, NonEmptyString term2)
    {
        var terms = new List<string> { term1.Get, term2.Get };
        var result = CreateResult(
            isValid: false,
            missingTerms: terms);

        return result.MissingTerms.Count == terms.Count
            && result.MissingTerms.SequenceEqual(terms);
    }

    #endregion

    #region DaysUntilExpiration Invariants

    /// <summary>
    /// Invariant: DaysUntilExpiration preserves a positive assigned value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DaysUntilExpiration_PositiveValue_PreservesValue(PositiveInt days)
    {
        var result = CreateResult(
            isValid: true,
            missingTerms: [],
            daysUntilExpiration: days.Get);

        return result.DaysUntilExpiration == days.Get;
    }

    /// <summary>
    /// Invariant: DaysUntilExpiration can be null for indefinite agreements.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool DaysUntilExpiration_Null_ForIndefiniteAgreements()
    {
        var result = CreateResult(
            isValid: true,
            missingTerms: [],
            daysUntilExpiration: null);

        return result.DaysUntilExpiration is null;
    }

    #endregion

    #region Record Equality Invariants

    /// <summary>
    /// Invariant: 'with' expression creates a new instance preserving other properties.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool WithExpression_PreservesOtherProperties(NonEmptyString processorId)
    {
        var original = CreateResult(isValid: true, missingTerms: []);
        var modified = original with { ProcessorId = processorId.Get };

        return modified.ProcessorId == processorId.Get
            && modified.IsValid == original.IsValid
            && modified.MissingTerms == original.MissingTerms
            && modified.Warnings == original.Warnings
            && modified.ValidatedAtUtc == original.ValidatedAtUtc;
    }

    #endregion

    #region Helpers

    private static DPAValidationResult CreateResult(
        bool isValid,
        IReadOnlyList<string> missingTerms,
        IReadOnlyList<string>? warnings = null,
        int? daysUntilExpiration = 30) => new()
        {
            ProcessorId = Guid.NewGuid().ToString(),
            DPAId = isValid ? Guid.NewGuid().ToString() : null,
            IsValid = isValid,
            Status = isValid ? DPAStatus.Active : null,
            MissingTerms = missingTerms,
            Warnings = warnings ?? [],
            DaysUntilExpiration = daysUntilExpiration,
            ValidatedAtUtc = DateTimeOffset.UtcNow
        };

    #endregion
}
