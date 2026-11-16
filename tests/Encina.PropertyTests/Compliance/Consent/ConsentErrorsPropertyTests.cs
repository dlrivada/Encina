using Encina.Compliance.Consent;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Consent;

/// <summary>
/// Property-based tests proving that no <see cref="ConsentErrors"/> factory ever embeds an
/// arbitrary, non-empty data subject id in <see cref="EncinaError.Message"/> (#1350). The
/// message reaches logs, activity tags and <c>ProblemDetails.Detail</c>, so it must describe the
/// subject non-identifyingly regardless of what identifier value is supplied.
/// </summary>
public class ConsentErrorsPropertyTests
{
    /// <summary>
    /// Builds a subject id from an arbitrary FsCheck string, appending a fresh GUID so the id can
    /// never coincide with the other arbitrary strings a factory also embeds (purpose, version
    /// ids), which would otherwise make the invariant unfalsifiable for short shrunk inputs like
    /// "a".
    /// </summary>
    private static string MakeSubjectId(NonEmptyString seed) => $"subject-{seed.Get}-{Guid.NewGuid():N}";

    /// <summary>
    /// Invariant: <see cref="ConsentErrors.MissingConsent"/> never embeds the subject id in the
    /// message, for any non-empty subject id.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool MissingConsent_MessageNeverContainsSubjectId(NonEmptyString subject, NonEmptyString purpose)
    {
        var subjectId = MakeSubjectId(subject);
        var error = ConsentErrors.MissingConsent(subjectId, purpose.Get);

        return !error.Message.Contains(subjectId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invariant: <see cref="ConsentErrors.ConsentExpired"/> never embeds the subject id in the
    /// message, for any non-empty subject id.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ConsentExpired_MessageNeverContainsSubjectId(NonEmptyString subject, NonEmptyString purpose)
    {
        var subjectId = MakeSubjectId(subject);
        var error = ConsentErrors.ConsentExpired(subjectId, purpose.Get, DateTimeOffset.UtcNow);

        return !error.Message.Contains(subjectId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invariant: <see cref="ConsentErrors.ConsentWithdrawn"/> never embeds the subject id in the
    /// message, for any non-empty subject id.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ConsentWithdrawn_MessageNeverContainsSubjectId(NonEmptyString subject, NonEmptyString purpose)
    {
        var subjectId = MakeSubjectId(subject);
        var error = ConsentErrors.ConsentWithdrawn(subjectId, purpose.Get, DateTimeOffset.UtcNow);

        return !error.Message.Contains(subjectId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invariant: <see cref="ConsentErrors.RequiresReconsent"/> never embeds the subject id in the
    /// message, for any non-empty subject id.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RequiresReconsent_MessageNeverContainsSubjectId(
        NonEmptyString subject,
        NonEmptyString purpose,
        NonEmptyString currentVersionId,
        NonEmptyString consentedVersionId)
    {
        var subjectId = MakeSubjectId(subject);
        var error = ConsentErrors.RequiresReconsent(
            subjectId, purpose.Get, currentVersionId.Get, consentedVersionId.Get);

        return !error.Message.Contains(subjectId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invariant: <see cref="ConsentErrors.VersionMismatch"/> never embeds the subject id in the
    /// message, for any non-empty subject id.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool VersionMismatch_MessageNeverContainsSubjectId(
        NonEmptyString subject,
        NonEmptyString purpose,
        NonEmptyString expectedVersionId,
        NonEmptyString actualVersionId)
    {
        var subjectId = MakeSubjectId(subject);
        var error = ConsentErrors.VersionMismatch(
            subjectId, purpose.Get, expectedVersionId.Get, actualVersionId.Get);

        return !error.Message.Contains(subjectId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invariant (#3771): <see cref="ConsentErrors.ReconsentRequired"/> always carries its code and
    /// the InvalidRequest category, names <c>ProvideReconsentAsync</c> and both versions, for any
    /// purpose and versions.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ReconsentRequired_IsCodedInvalidRequestPointingToProvideReconsent(
        NonEmptyString purpose, NonEmptyString currentVersionId, NonEmptyString grantedVersionId)
    {
        var error = ConsentErrors.ReconsentRequired(purpose.Get, currentVersionId.Get, grantedVersionId.Get);

        return error.GetCode().Exists(c => c == ConsentErrors.ReconsentRequiredCode)
            && error.GetCategory() == ErrorCategory.InvalidRequest
            && error.Message.Contains("ProvideReconsentAsync", StringComparison.Ordinal)
            && error.Message.Contains(currentVersionId.Get, StringComparison.Ordinal)
            && error.Message.Contains(grantedVersionId.Get, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invariant (#3771): <see cref="ConsentErrors.ActInFuture"/> always carries its code and the
    /// InvalidRequest category, and its message names the purpose, for any purpose and tolerance.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ActInFuture_IsCodedInvalidRequest(NonEmptyString purpose, PositiveInt minutes)
    {
        var error = ConsentErrors.ActInFuture(purpose.Get, TimeSpan.FromMinutes(minutes.Get));

        return error.GetCode().Exists(c => c == ConsentErrors.ActInFutureCode)
            && error.GetCategory() == ErrorCategory.InvalidRequest
            && error.Message.Contains(purpose.Get, StringComparison.Ordinal);
    }
}
