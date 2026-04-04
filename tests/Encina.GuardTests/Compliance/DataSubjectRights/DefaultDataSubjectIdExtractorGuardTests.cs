using Encina.Compliance.DataSubjectRights;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DefaultDataSubjectIdExtractor"/> verifying null parameter handling.
/// </summary>
public class DefaultDataSubjectIdExtractorGuardTests
{
    private readonly DefaultDataSubjectIdExtractor _sut = new();

    [Fact]
    public void ExtractSubjectId_NullRequest_ThrowsArgumentNullException()
    {
        var context = Substitute.For<IRequestContext>();

        var act = () => _sut.ExtractSubjectId<object>(null!, context);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("request");
    }

    [Fact]
    public void ExtractSubjectId_NullContext_ThrowsArgumentNullException()
    {
        var request = new object();

        var act = () => _sut.ExtractSubjectId(request, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
    }
}
