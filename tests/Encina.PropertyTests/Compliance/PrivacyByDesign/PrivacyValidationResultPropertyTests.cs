using Encina.Compliance.PrivacyByDesign.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.PrivacyByDesign;

/// <summary>
/// Property-based tests for <see cref="PrivacyValidationResult"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class PrivacyValidationResultPropertyTests
{
    #region IsCompliant Invariants

    /// <summary>
    /// Invariant: IsCompliant is true when there are no violations.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCompliant_TrueWhenNoViolations(NonEmptyString requestTypeName)
    {
        var result = new PrivacyValidationResult
        {
            RequestTypeName = requestTypeName.Get,
            Violations = [],
            ValidatedAtUtc = DateTimeOffset.UtcNow
        };

        return result.IsCompliant;
    }

    /// <summary>
    /// Invariant: IsCompliant is false when there is at least one violation.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCompliant_FalseWhenHasViolations(NonEmptyString requestTypeName, NonEmptyString fieldName)
    {
        var violation = new PrivacyViolation(
            FieldName: fieldName.Get,
            ViolationType: PrivacyViolationType.DataMinimization,
            Message: "Test violation",
            Severity: MinimizationSeverity.Warning);

        var result = new PrivacyValidationResult
        {
            RequestTypeName = requestTypeName.Get,
            Violations = [violation],
            ValidatedAtUtc = DateTimeOffset.UtcNow
        };

        return !result.IsCompliant;
    }

    #endregion

    #region Violations Preservation

    /// <summary>
    /// Invariant: The Violations list preserves all entries passed to the constructor.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Violations_PreservesAllEntries(NonEmptyString[] fieldNames)
    {
        var uniqueFields = fieldNames
            .Select(f => f.Get)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct()
            .ToList();

        if (uniqueFields.Count == 0)
        {
            return true; // Skip degenerate case
        }

        var violations = uniqueFields
            .Select(f => new PrivacyViolation(
                FieldName: f,
                ViolationType: PrivacyViolationType.DataMinimization,
                Message: $"Violation for {f}",
                Severity: MinimizationSeverity.Warning))
            .ToList();

        var result = new PrivacyValidationResult
        {
            RequestTypeName = "TestRequest",
            Violations = violations,
            ValidatedAtUtc = DateTimeOffset.UtcNow
        };

        return result.Violations.Count == violations.Count
            && violations.All(v => result.Violations.Contains(v));
    }

    #endregion
}
