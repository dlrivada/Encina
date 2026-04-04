using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Deep method-level guard tests for <see cref="DefaultDataErasureExecutor"/>.
/// </summary>
public class DefaultDataErasureExecutorMethodGuardTests
{
    private readonly DefaultDataErasureExecutor _sut;

    public DefaultDataErasureExecutorMethodGuardTests()
    {
        _sut = new DefaultDataErasureExecutor(
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureStrategy>(),
            NullLogger<DefaultDataErasureExecutor>.Instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EraseAsync_NullOrWhitespaceSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = () => _sut.EraseAsync(subjectId!, new ErasureScope { Reason = ErasureReason.NoLongerNecessary }).AsTask();
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task EraseAsync_NullScope_ThrowsArgumentNullException()
    {
        var act = () => _sut.EraseAsync("subject-1", null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("scope");
    }
}
