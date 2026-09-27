using Encina;
using Encina.Marten.GDPR;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

/// <summary>
/// The data subject's own identifier must never reach <see cref="EncinaError.Message"/> or any
/// <see cref="EncinaError"/> details value produced by the <see cref="CryptoShreddingErrors"/>
/// factories that take a <c>subjectId</c> parameter (#1415). Message reaches
/// <c>Encina.AspNetCore.ProblemDetailsExtensions.ToProblemDetails</c>'s
/// <c>ProblemDetails.Detail</c> unfiltered, and details reaches anything that serializes
/// <see cref="EncinaError"/> metadata.
/// </summary>
public sealed class CryptoShreddingErrorsSubjectIdLeakTests
{
    private const string SubjectId = "patient-42";

    [Fact]
    public void SubjectForgotten_NeverLeaksSubjectId()
    {
        var error = CryptoShreddingErrors.SubjectForgotten(SubjectId);
        AssertNoSubjectId(error);
    }

    [Fact]
    public void EncryptionFailed_NeverLeaksSubjectId()
    {
        var error = CryptoShreddingErrors.EncryptionFailed(SubjectId, "Email");
        AssertNoSubjectId(error);
    }

    [Fact]
    public void DecryptionFailed_NeverLeaksSubjectId()
    {
        var error = CryptoShreddingErrors.DecryptionFailed(SubjectId, "Email");
        AssertNoSubjectId(error);
    }

    [Fact]
    public void KeyRotationFailed_NeverLeaksSubjectId()
    {
        var error = CryptoShreddingErrors.KeyRotationFailed(SubjectId);
        AssertNoSubjectId(error);
    }

    [Fact]
    public void InvalidSubjectId_NeverLeaksSubjectId()
    {
        var error = CryptoShreddingErrors.InvalidSubjectId(SubjectId);
        AssertNoSubjectId(error);
    }

    [Fact]
    public void InvalidSubjectId_NullInput_NeverLeaksSubjectId()
    {
        var error = CryptoShreddingErrors.InvalidSubjectId(null);

        var details = error.GetDetails();
        details.ContainsKey("subjectId").ShouldBeFalse();
    }

    [Fact]
    public void KeyAlreadyExists_NeverLeaksSubjectId()
    {
        var error = CryptoShreddingErrors.KeyAlreadyExists(SubjectId);
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
