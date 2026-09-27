using Encina.Compliance.GDPR;

using Shouldly;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// The data subject's own identifier must never reach <see cref="EncinaError.Message"/> or any
/// <see cref="EncinaError"/> details value produced by the <see cref="GDPRErrors"/> factories that
/// take a <c>subjectId</c> parameter (#1426). Message reaches
/// <c>Encina.AspNetCore.ProblemDetailsExtensions.ToProblemDetails</c>'s
/// <c>ProblemDetails.Detail</c> unfiltered, and details reaches anything that serializes
/// <see cref="EncinaError"/> metadata. Same defect class as #1350 and #1415, this time in
/// <c>Encina.Compliance.GDPR</c>'s <see cref="GDPRErrors.ConsentNotFound"/>.
/// </summary>
public sealed class GDPRErrorsSubjectIdLeakTests
{
    private const string SubjectId = "patient-42";

    [Fact]
    public void ConsentNotFound_WithSubjectId_NeverLeaksSubjectId()
    {
        var error = GDPRErrors.ConsentNotFound(typeof(string), SubjectId);
        AssertNoSubjectId(error);
    }

    [Fact]
    public void ConsentNotFound_WithoutSubjectId_NeverLeaksSubjectId()
    {
        var error = GDPRErrors.ConsentNotFound(typeof(string));
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
