using Encina.Compliance.DataSubjectRights;

using NSubstitute;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="DefaultDataSubjectIdExtractor"/> verifying subject ID extraction
/// priority order: SubjectIdProperty > SubjectId > UserId > context.UserId.
/// </summary>
public class DefaultDataSubjectIdExtractorTests
{
    private readonly DefaultDataSubjectIdExtractor _sut = new();

    private sealed record RequestWithSubjectId(string SubjectId);

    private sealed record RequestWithUserId(string UserId);

    private sealed record RequestWithNoId(string Name);

    [RestrictProcessing(SubjectIdProperty = nameof(RequestWithExplicitId.CustomerId))]
    private sealed record RequestWithExplicitId(string CustomerId);

    [RestrictProcessing(SubjectIdProperty = "NonExistentProperty")]
    private sealed record RequestWithBadProperty(string SubjectId);

    [Fact]
    public void ExtractSubjectId_WithSubjectIdProperty_ReturnsSubjectId()
    {
        var request = new RequestWithSubjectId("subject-123");
        var context = Substitute.For<IRequestContext>();

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe("subject-123");
    }

    [Fact]
    public void ExtractSubjectId_WithUserIdProperty_ReturnsUserId()
    {
        var request = new RequestWithUserId("user-456");
        var context = Substitute.For<IRequestContext>();

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe("user-456");
    }

    [Fact]
    public void ExtractSubjectId_WithNoIdProperty_FallsBackToContextUserId()
    {
        var request = new RequestWithNoId("John");
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("context-user-789");

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe("context-user-789");
    }

    [Fact]
    public void ExtractSubjectId_WithExplicitAttribute_ReturnsCustomerId()
    {
        var request = new RequestWithExplicitId("customer-999");
        var context = Substitute.For<IRequestContext>();

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe("customer-999");
    }

    [Fact]
    public void ExtractSubjectId_WithBadPropertyAttribute_FallsBackToSubjectId()
    {
        var request = new RequestWithBadProperty("subject-111");
        var context = Substitute.For<IRequestContext>();

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBe("subject-111");
    }

    [Fact]
    public void ExtractSubjectId_WithNoMatchAndNullContextUserId_ReturnsNull()
    {
        var request = new RequestWithNoId("John");
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns((string?)null);

        var result = _sut.ExtractSubjectId(request, context);

        result.ShouldBeNull();
    }
}
