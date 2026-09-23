using Encina.Compliance.DataSubjectRights;

using NSubstitute;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Regression tests for #1149: <see cref="DefaultDataSubjectIdExtractor"/> used to recognize only
/// a <c>string</c>-typed subject-id property. When the request's subject-id property was a
/// <see cref="Guid"/> (a very common shape — e.g. <c>PatientId</c>, <c>CustomerId</c> declared as
/// <c>Guid</c> rather than <c>string</c>), <see cref="DefaultDataSubjectIdExtractor.ExtractSubjectId{TRequest}"/>
/// silently ignored it at every priority step and fell back to <see cref="IRequestContext.UserId"/> —
/// the authenticated caller — instead of failing loudly or returning the real subject.
/// </summary>
public class DefaultDataSubjectIdExtractorGuidSubjectIdTests
{
    private readonly DefaultDataSubjectIdExtractor _sut = new();

    [RestrictProcessing(SubjectIdProperty = nameof(PatientId))]
    private sealed record RequestWithGuidSubjectIdViaAttribute(Guid PatientId);

    private sealed record RequestWithGuidSubjectIdProperty(Guid SubjectId);

    private sealed record RequestWithGuidUserIdProperty(Guid UserId);

    /// <summary>
    /// A <c>[RestrictProcessing(SubjectIdProperty = "PatientId")]</c> request whose <c>PatientId</c>
    /// is a <see cref="Guid"/> is recognized as the data subject.
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithGuidSubjectIdPropertyViaAttribute_ShouldReturnThePatientNotTheCaller()
    {
        var patientId = Guid.NewGuid();
        var request = new RequestWithGuidSubjectIdViaAttribute(patientId);
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42"); // the clinician making the request, NOT the data subject

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe(patientId.ToString());
        result.ShouldNotBe("professional-42");
    }

    /// <summary>
    /// Same fix via the conventional <c>SubjectId</c> property name (priority 2).
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
    /// Same fix via the conventional <c>UserId</c> property name (priority 3).
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

    /// <summary>
    /// A matching property whose value is <c>null</c> is a missing subject, not a reason to fall
    /// back to the authenticated caller.
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithNullGuidSubjectIdProperty_ShouldReturnNull_NotTheCaller()
    {
        var request = new RequestWithNullableGuidSubjectId(null);
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42");

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBeNull();
        result.ShouldNotBe("professional-42");
    }

    /// <summary>
    /// A matching property of a type that is not a supported subject identifier is a configuration
    /// error, not a silent fallback to the caller.
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithUnconvertibleSubjectIdProperty_ShouldThrow()
    {
        var request = new RequestWithUnconvertibleSubjectId(new object());
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42");

        Should.Throw<InvalidOperationException>(() => _sut.ExtractSubjectId(request, context));
    }

    /// <summary>
    /// A strongly-typed id (record struct wrapping a <see cref="Guid"/>) is unwrapped through its
    /// <c>Value</c> property instead of using the compiler-generated <c>ToString()</c>.
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithRecordStructSubjectId_ShouldUnwrapValue()
    {
        var patientId = Guid.NewGuid();
        var request = new RequestWithStronglyTypedSubjectId(new PatientId(patientId));
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42");

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe(patientId.ToString("D"));
    }

    /// <summary>
    /// Numeric <c>0</c> is a valid subject id, not a missing subject.
    /// </summary>
    [Fact]
    public void ExtractSubjectId_WithZeroNumericSubjectId_ShouldReturnZero()
    {
        var request = new RequestWithNumericSubjectId(0);
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("professional-42");

        _sut.ExtractSubjectId(request, context).ShouldBe("0");
    }

    private sealed record RequestWithNullableGuidSubjectId(Guid? SubjectId);

    private sealed record RequestWithUnconvertibleSubjectId(object SubjectId);

    private readonly record struct PatientId(Guid Value);

    private sealed record RequestWithStronglyTypedSubjectId(PatientId SubjectId);

    private sealed record RequestWithNumericSubjectId(long SubjectId);
}
