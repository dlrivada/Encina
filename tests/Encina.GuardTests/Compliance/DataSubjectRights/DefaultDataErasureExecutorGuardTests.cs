using Encina.Compliance.DataSubjectRights;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DefaultDataErasureExecutor"/> to verify null and invalid parameter handling.
/// </summary>
public class DefaultDataErasureExecutorGuardTests
{
    private readonly DefaultDataErasureExecutor _executor;

    public DefaultDataErasureExecutorGuardTests()
    {
        _executor = new DefaultDataErasureExecutor(
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureStrategy>(),
            NullLogger<DefaultDataErasureExecutor>.Instance);
    }

    #region Constructor Guard Tests

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null locator.
    /// </summary>
    [Fact]
    public void Constructor_NullLocator_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataErasureExecutor(
            null!,
            Substitute.For<IDataErasureStrategy>(),
            NullLogger<DefaultDataErasureExecutor>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("locator");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null strategy.
    /// </summary>
    [Fact]
    public void Constructor_NullStrategy_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataErasureExecutor(
            Substitute.For<IPersonalDataLocator>(),
            null!,
            NullLogger<DefaultDataErasureExecutor>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("strategy");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataErasureExecutor(
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureStrategy>(),
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region EraseAsync Guard Tests

    /// <summary>
    /// Verifies that EraseAsync throws <see cref="ArgumentException"/> for invalid subjectId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EraseAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };
        var act = async () => await _executor.EraseAsync(subjectId!, scope);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    /// <summary>
    /// Verifies that EraseAsync throws <see cref="ArgumentNullException"/> for null scope.
    /// </summary>
    [Fact]
    public async Task EraseAsync_NullScope_ThrowsArgumentNullException()
    {
        var act = async () => await _executor.EraseAsync("subject-1", null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("scope");
    }

    #endregion
}
