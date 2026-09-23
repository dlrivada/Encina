using Encina.Compliance.GDPR;

using NSubstitute;

namespace Encina.UnitTests.Compliance.GDPR.Abstractions;

/// <summary>
/// Documents the intended, unaffected behavior of <see cref="DefaultLawfulBasisSubjectIdExtractor"/>
/// with respect to #1149 (non-string subject-id properties on <c>DefaultDataSubjectIdExtractor</c>
/// and <c>ConsentRequiredPipelineBehavior</c> being silently ignored in favor of the authenticated
/// caller).
/// </summary>
/// <remarks>
/// Unlike <c>Encina.Compliance.DataSubjectRights.DefaultDataSubjectIdExtractor</c> and
/// <c>Encina.Compliance.Consent.ConsentRequiredPipelineBehavior</c>, this default implementation
/// does not inspect the request's properties at all — by design, it always returns
/// <see cref="IRequestContext.UserId"/>, and <see cref="LawfulBasisAttribute"/> has no
/// <c>SubjectIdProperty</c>. It therefore does not share the reflection/type-gating bug those two
/// had, and needed no code change for #1149. Applications that need lawful-basis subject
/// resolution from a request property (e.g. a Guid patient id) register a custom
/// <see cref="ILawfulBasisSubjectIdExtractor"/>, per the interface's own documented example.
/// </remarks>
public class DefaultLawfulBasisSubjectIdExtractorTests
{
    private readonly DefaultLawfulBasisSubjectIdExtractor _sut = new();

    private sealed record RequestWithGuidPatientId(Guid PatientId);

    [Fact]
    public void ExtractSubjectId_AlwaysReturnsContextUserId_RegardlessOfRequestShape()
    {
        var request = new RequestWithGuidPatientId(Guid.NewGuid());
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42");

        var result = _sut.ExtractSubjectId(request, context);

        // By design: this default never reads request properties, so it is unaffected by #1149.
        result.ShouldBe("professional-42");
    }

    [Fact]
    public void ExtractSubjectId_NullContext_ThrowsArgumentNullException()
    {
        var request = new RequestWithGuidPatientId(Guid.NewGuid());

        Should.Throw<ArgumentNullException>(() => _sut.ExtractSubjectId<RequestWithGuidPatientId>(request, null!));
    }
}
