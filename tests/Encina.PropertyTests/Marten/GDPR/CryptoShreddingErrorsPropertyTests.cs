using Encina.Marten.GDPR;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Marten.GDPR;

/// <summary>
/// Property-based tests proving that no <see cref="CryptoShreddingErrors"/> factory ever embeds
/// an arbitrary, non-empty data subject id in <see cref="EncinaError.Message"/> or in its details
/// (#1415). These factories are pure — no Marten or PostgreSQL is needed to exercise them.
/// </summary>
public class CryptoShreddingErrorsPropertyTests
{
    /// <summary>
    /// Builds a subject id from an arbitrary FsCheck string, appending a fresh GUID so the id can
    /// never coincide with the other arbitrary strings a factory also embeds (property name),
    /// which would otherwise make the invariant unfalsifiable for short shrunk inputs like "a"
    /// (see #1350's ConsentErrorsPropertyTests for the same gotcha).
    /// </summary>
    private static string MakeSubjectId(NonEmptyString seed) => $"subject-{seed.Get}-{Guid.NewGuid():N}";

    [Property(MaxTest = 100)]
    public bool SubjectForgotten_NeverLeaksSubjectId(NonEmptyString subject)
    {
        var subjectId = MakeSubjectId(subject);
        var error = CryptoShreddingErrors.SubjectForgotten(subjectId);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool EncryptionFailed_NeverLeaksSubjectId(NonEmptyString subject, NonEmptyString propertyName)
    {
        var subjectId = MakeSubjectId(subject);
        var error = CryptoShreddingErrors.EncryptionFailed(subjectId, propertyName.Get);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool DecryptionFailed_NeverLeaksSubjectId(NonEmptyString subject, NonEmptyString propertyName)
    {
        var subjectId = MakeSubjectId(subject);
        var error = CryptoShreddingErrors.DecryptionFailed(subjectId, propertyName.Get);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool KeyRotationFailed_NeverLeaksSubjectId(NonEmptyString subject)
    {
        var subjectId = MakeSubjectId(subject);
        var error = CryptoShreddingErrors.KeyRotationFailed(subjectId);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool InvalidSubjectId_NeverLeaksSubjectId(NonEmptyString subject)
    {
        var subjectId = MakeSubjectId(subject);
        var error = CryptoShreddingErrors.InvalidSubjectId(subjectId);

        return NeverLeaks(error, subjectId);
    }

    [Property(MaxTest = 100)]
    public bool KeyAlreadyExists_NeverLeaksSubjectId(NonEmptyString subject)
    {
        var subjectId = MakeSubjectId(subject);
        var error = CryptoShreddingErrors.KeyAlreadyExists(subjectId);

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
