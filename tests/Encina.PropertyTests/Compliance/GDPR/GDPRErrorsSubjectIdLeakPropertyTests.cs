using Encina.Compliance.GDPR;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.GDPR;

/// <summary>
/// Property-based test proving that <see cref="GDPRErrors.ConsentNotFound"/> never embeds an
/// arbitrary, non-empty data subject id in <see cref="EncinaError.Message"/> or in its details
/// (#1426). The message reaches logs, activity tags and <c>ProblemDetails.Detail</c>, so it must
/// describe the subject non-identifyingly regardless of what identifier value is supplied.
/// </summary>
public class GDPRErrorsSubjectIdLeakPropertyTests
{
    /// <summary>
    /// Builds a subject id from an arbitrary FsCheck string, appending a fresh GUID so the id can
    /// never coincide with the other arbitrary strings the factory also embeds (the request type
    /// name), which would otherwise make the invariant unfalsifiable for short shrunk inputs
    /// (see #1350's ConsentErrorsPropertyTests for the same gotcha).
    /// </summary>
    private static string MakeSubjectId(NonEmptyString seed) => $"subject-{seed.Get}-{Guid.NewGuid():N}";

    [Property(MaxTest = 100)]
    public bool ConsentNotFound_NeverLeaksSubjectId(NonEmptyString subject)
    {
        var subjectId = MakeSubjectId(subject);
        var error = GDPRErrors.ConsentNotFound(typeof(string), subjectId);

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
