using Encina.Compliance.DataSubjectRights;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DataSubjectRights;

/// <summary>
/// Property-based tests for <see cref="DataSubjectRightsOptionsValidator"/> verifying
/// validation invariants hold for arbitrary input values.
/// </summary>
public class DSROptionsValidatorPropertyTests
{
    private readonly DataSubjectRightsOptionsValidator _sut = new();

    /// <summary>
    /// Invariant: Any positive DefaultDeadlineDays with MaxExtensionDays in [0, 60] always succeeds.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ValidRanges_AlwaysSucceed(PositiveInt deadlineDays)
    {
        var extensionDays = Math.Abs(deadlineDays.Get % 61); // 0..60
        var options = new DataSubjectRightsOptions
        {
            DefaultDeadlineDays = deadlineDays.Get,
            MaxExtensionDays = extensionDays
        };

        var result = _sut.Validate(null, options);
        return result.Succeeded;
    }

    /// <summary>
    /// Invariant: Zero or negative DefaultDeadlineDays always fails validation.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool NonPositiveDeadline_AlwaysFails(NegativeInt deadlineDays)
    {
        var options = new DataSubjectRightsOptions
        {
            DefaultDeadlineDays = deadlineDays.Get,
            MaxExtensionDays = 30
        };

        var result = _sut.Validate(null, options);
        return result.Failed;
    }

    /// <summary>
    /// Invariant: MaxExtensionDays greater than 60 always fails validation.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ExtensionDaysAbove60_AlwaysFails(PositiveInt extra)
    {
        var extensionDays = 61 + (extra.Get % 1000);
        var options = new DataSubjectRightsOptions
        {
            DefaultDeadlineDays = 30,
            MaxExtensionDays = extensionDays
        };

        var result = _sut.Validate(null, options);
        return result.Failed;
    }
}
