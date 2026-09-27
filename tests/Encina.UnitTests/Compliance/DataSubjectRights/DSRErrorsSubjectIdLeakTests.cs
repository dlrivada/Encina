using Encina.Compliance.DataSubjectRights;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// The data subject's own identifier must never reach <see cref="EncinaError.Message"/> or any
/// <see cref="EncinaError"/> details value produced by the <see cref="DSRErrors"/> factories that
/// take a <c>subjectId</c> parameter (#1415). Message reaches
/// <c>Encina.AspNetCore.ProblemDetailsExtensions.ToProblemDetails</c>'s
/// <c>ProblemDetails.Detail</c> unfiltered, and details reaches anything that serializes
/// <see cref="EncinaError"/> metadata.
/// </summary>
public sealed class DSRErrorsSubjectIdLeakTests
{
    private const string SubjectId = "patient-42";

    [Fact]
    public void RestrictionActive_NeverLeaksSubjectId()
    {
        var error = DSRErrors.RestrictionActive(SubjectId);
        AssertNoSubjectId(error);
    }

    [Fact]
    public void ErasureFailed_NeverLeaksSubjectId()
    {
        var error = DSRErrors.ErasureFailed(SubjectId, "Database error");
        AssertNoSubjectId(error);
    }

    [Fact]
    public void ExportFailed_NeverLeaksSubjectId()
    {
        var error = DSRErrors.ExportFailed(SubjectId, ExportFormat.JSON, "Serialization error");
        AssertNoSubjectId(error);
    }

    [Fact]
    public void ExemptionApplies_NeverLeaksSubjectId()
    {
        var error = DSRErrors.ExemptionApplies(SubjectId, ErasureExemption.LegalObligation, "Tax records");
        AssertNoSubjectId(error);
    }

    [Fact]
    public void SubjectNotFound_NeverLeaksSubjectId()
    {
        var error = DSRErrors.SubjectNotFound(SubjectId);
        AssertNoSubjectId(error);
    }

    [Fact]
    public void LocatorFailed_NeverLeaksSubjectId()
    {
        var error = DSRErrors.LocatorFailed(SubjectId, "Connection timeout");
        AssertNoSubjectId(error);
    }

    [Fact]
    public void RectificationFailed_NeverLeaksSubjectId()
    {
        var error = DSRErrors.RectificationFailed(SubjectId, "Email", "Invalid format");
        AssertNoSubjectId(error);
    }

    [Fact]
    public void ObjectionRejected_NeverLeaksSubjectId()
    {
        var error = DSRErrors.ObjectionRejected(SubjectId, "Marketing", "Compelling interest");
        AssertNoSubjectId(error);
    }

    private static void AssertNoSubjectId(EncinaError error)
    {
        error.Message.ShouldNotContain(SubjectId);

        var details = error.GetDetails();
        details.ContainsKey("subjectId").ShouldBeFalse();
        foreach (var value in details.Values)
        {
            (value?.ToString() ?? string.Empty).ShouldNotContain(SubjectId);
        }
    }
}
