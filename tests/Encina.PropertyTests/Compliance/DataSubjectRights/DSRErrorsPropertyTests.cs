using Encina.Compliance.DataSubjectRights;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DataSubjectRights;

/// <summary>
/// Property-based tests proving that no <see cref="DSRErrors"/> factory ever embeds an arbitrary,
/// non-empty data subject id in <see cref="EncinaError.Message"/> or in its details (#1415). The
/// message reaches logs, activity tags and <c>ProblemDetails.Detail</c>, so it must describe the
/// subject non-identifyingly regardless of what identifier value is supplied.
/// </summary>
public class DSRErrorsPropertyTests
{
    /// <summary>
    /// Builds a subject id from an arbitrary FsCheck string, appending a fresh GUID so the id can
    /// never coincide with the other arbitrary strings a factory also embeds (reason, field name,
    /// purpose), which would otherwise make the invariant unfalsifiable for short shrunk inputs
    /// like "a" (see #1350's ConsentErrorsPropertyTests for the same gotcha).
    /// </summary>
    private static string MakeSubjectId(NonEmptyString seed) => $"subject-{seed.Get}-{Guid.NewGuid():N}";

    [Property(MaxTest = 100)]
    public bool RestrictionActive_NeverLeaksSubjectId(NonEmptyString subject)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.RestrictionActive(subjectId);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool ErasureFailed_NeverLeaksSubjectId(NonEmptyString subject, NonEmptyString reason)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.ErasureFailed(subjectId, reason.Get);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool ExportFailed_NeverLeaksSubjectId(NonEmptyString subject, NonEmptyString reason)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.ExportFailed(subjectId, ExportFormat.JSON, reason.Get);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool ExemptionApplies_NeverLeaksSubjectId(NonEmptyString subject, NonEmptyString reason)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.ExemptionApplies(subjectId, ErasureExemption.LegalObligation, reason.Get);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool SubjectNotFound_NeverLeaksSubjectId(NonEmptyString subject)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.SubjectNotFound(subjectId);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool LocatorFailed_NeverLeaksSubjectId(NonEmptyString subject, NonEmptyString reason)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.LocatorFailed(subjectId, reason.Get);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool RectificationFailed_NeverLeaksSubjectId(
        NonEmptyString subject, NonEmptyString fieldName, NonEmptyString reason)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.RectificationFailed(subjectId, fieldName.Get, reason.Get);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool ObjectionRejected_NeverLeaksSubjectId(
        NonEmptyString subject, NonEmptyString purpose, NonEmptyString reason)
    {
        var subjectId = MakeSubjectId(subject);
        var error = DSRErrors.ObjectionRejected(subjectId, purpose.Get, reason.Get);

        return NeverLeaks(error, subjectId);
    }

    private static bool NeverLeaks(EncinaError error, string subjectId)
    {
        if (error.Message.Contains(subjectId, StringComparison.Ordinal))
        {
            return false;
        }

        var details = error.GetDetails();
        if (details.ContainsKey("subjectId"))
        {
            return false;
        }

        foreach (var value in details.Values)
        {
            if ((value?.ToString() ?? string.Empty).Contains(subjectId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
