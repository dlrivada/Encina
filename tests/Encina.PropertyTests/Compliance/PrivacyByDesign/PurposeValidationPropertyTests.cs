using Encina.Compliance.PrivacyByDesign.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.PrivacyByDesign;

/// <summary>
/// Property-based tests for <see cref="PurposeValidationResult"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class PurposeValidationPropertyTests
{
    #region IsValid Invariants

    /// <summary>
    /// Invariant: IsValid is true if and only if ViolatingFields is empty.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool PurposeValidation_IsValid_IffNoViolatingFields(NonEmptyString purpose)
    {
        var allowedFields = new List<string> { "Field1", "Field2" };
        var violatingFields = new List<string>();
        var isValid = violatingFields.Count == 0;

        var result = new PurposeValidationResult(
            DeclaredPurpose: purpose.Get,
            AllowedFields: allowedFields,
            ViolatingFields: violatingFields,
            IsValid: isValid);

        return result.IsValid == (result.ViolatingFields.Count == 0);
    }

    #endregion

    #region AllowedFields Preservation

    /// <summary>
    /// Invariant: AllowedFields preserves all entries passed to the constructor.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool PurposeValidation_AllowedFields_PreservesAllEntries(NonEmptyString[] fieldNames)
    {
        // Deduplicate and filter empty strings for meaningful test data.
        var fields = fieldNames
            .Select(f => f.Get)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct()
            .ToList();

        if (fields.Count == 0)
        {
            return true; // Skip degenerate case
        }

        var result = new PurposeValidationResult(
            DeclaredPurpose: "TestPurpose",
            AllowedFields: fields,
            ViolatingFields: [],
            IsValid: true);

        return result.AllowedFields.Count == fields.Count
            && fields.All(f => result.AllowedFields.Contains(f));
    }

    #endregion

    #region ViolatingFields Preservation

    /// <summary>
    /// Invariant: ViolatingFields preserves all entries passed to the constructor.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool PurposeValidation_ViolatingFields_PreservesAllEntries(NonEmptyString[] fieldNames)
    {
        var fields = fieldNames
            .Select(f => f.Get)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct()
            .ToList();

        if (fields.Count == 0)
        {
            return true; // Skip degenerate case
        }

        var result = new PurposeValidationResult(
            DeclaredPurpose: "TestPurpose",
            AllowedFields: [],
            ViolatingFields: fields,
            IsValid: false);

        return result.ViolatingFields.Count == fields.Count
            && fields.All(f => result.ViolatingFields.Contains(f));
    }

    #endregion
}
