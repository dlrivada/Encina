using Encina.Compliance.DataSubjectRights;

using NSubstitute;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Spike reproduction for finding N3 (verification spike, branch spike/verify-request-context):
/// <see cref="DefaultDataSubjectIdExtractor"/> only recognizes a <c>string</c>-typed subject-id
/// property. When the request's subject-id property is a <see cref="Guid"/> (a very common shape —
/// e.g. <c>PatientId</c>, <c>CustomerId</c> declared as <c>Guid</c> rather than <c>string</c>),
/// <see cref="DefaultDataSubjectIdExtractor.ExtractSubjectId{TRequest}"/> silently ignores it at
/// every priority step and falls back to <see cref="IRequestContext.UserId"/> — the authenticated
/// caller — instead of failing loudly or returning the real subject.
/// </summary>
/// <remarks>
/// Root cause: <c>DefaultDataSubjectIdExtractor.ResolveProperty</c>
/// (src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectIdExtractor.cs, priority steps 1-3)
/// requires <c>specified.PropertyType == typeof(string)</c> /
/// <c>subjectIdProp.PropertyType == typeof(string)</c> / <c>userIdProp.PropertyType == typeof(string)</c>
/// before accepting a candidate property. A <see cref="Guid"/>-typed property fails all three checks,
/// so <c>ResolveProperty</c> returns <c>null</c> and <c>ExtractSubjectId</c> falls through to
/// <c>context.UserId</c> (line 49) — treating whoever is authenticated (e.g. a clinician/professional
/// acting on behalf of a patient) as the data subject of the request.
/// </remarks>
public class DefaultDataSubjectIdExtractorGuidSubjectIdSpikeTests
{
    private readonly DefaultDataSubjectIdExtractor _sut = new();

    [RestrictProcessing(SubjectIdProperty = nameof(PatientId))]
    private sealed record RequestWithGuidSubjectIdViaAttribute(Guid PatientId);

    private sealed record RequestWithGuidSubjectIdProperty(Guid SubjectId);

    private sealed record RequestWithGuidUserIdProperty(Guid UserId);

    /// <summary>
    /// CONFIRMED (N3): a <c>[RestrictProcessing(SubjectIdProperty = "PatientId")]</c> request whose
    /// <c>PatientId</c> is a <see cref="Guid"/> should be recognized as the data subject. Instead, the
    /// property is rejected for not being a <c>string</c>, and the authenticated professional's
    /// <see cref="IRequestContext.UserId"/> is returned in its place.
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithGuidSubjectIdPropertyViaAttribute_ShouldReturnThePatientNotTheCaller()
    {
        var patientId = Guid.NewGuid();
        var request = new RequestWithGuidSubjectIdViaAttribute(patientId);
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42"); // the clinician making the request, NOT the data subject

        var result = _sut.ExtractSubjectId(request, context);

        // Expected (correct) behavior: the explicitly configured Guid subject-id property wins.
        // Actual (current, buggy) behavior: returns "professional-42" — the authenticated caller.
        result.ShouldBe(patientId.ToString());
        result.ShouldNotBe("professional-42");
    }

    /// <summary>
    /// Same defect via the conventional <c>SubjectId</c> property name (priority 2), which is also
    /// gated on <c>PropertyType == typeof(string)</c>.
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithGuidSubjectIdConventionProperty_ShouldReturnThePatientNotTheCaller()
    {
        var patientId = Guid.NewGuid();
        var request = new RequestWithGuidSubjectIdProperty(patientId);
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42");

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe(patientId.ToString());
        result.ShouldNotBe("professional-42");
    }

    /// <summary>
    /// Same defect via the conventional <c>UserId</c> property name (priority 3).
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithGuidUserIdConventionProperty_ShouldReturnTheRequestOwnerNotTheCaller()
    {
        var subjectUserId = Guid.NewGuid();
        var request = new RequestWithGuidUserIdProperty(subjectUserId);
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42");

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe(subjectUserId.ToString());
        result.ShouldNotBe("professional-42");
    }
}
