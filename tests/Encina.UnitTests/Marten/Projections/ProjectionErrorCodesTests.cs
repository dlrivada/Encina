using Encina.Marten.Projections;

namespace Encina.UnitTests.Marten.Projections;

/// <summary>
/// Unit tests for <see cref="ProjectionErrorCodes"/> constants.
/// </summary>
public sealed class ProjectionErrorCodesTests
{
    [Fact]
    public void Prefix_ShouldBePROJECTION()
        => ProjectionErrorCodes.Prefix.ShouldBe("PROJECTION");

    [Fact]
    public void ReadModelNotFound_ShouldStartWithPrefix()
        => ProjectionErrorCodes.ReadModelNotFound.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void StoreFailed_ShouldStartWithPrefix()
        => ProjectionErrorCodes.StoreFailed.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void DeleteFailed_ShouldStartWithPrefix()
        => ProjectionErrorCodes.DeleteFailed.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void QueryFailed_ShouldStartWithPrefix()
        => ProjectionErrorCodes.QueryFailed.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void ApplyFailed_ShouldStartWithPrefix()
        => ProjectionErrorCodes.ApplyFailed.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void RebuildFailed_ShouldStartWithPrefix()
        => ProjectionErrorCodes.RebuildFailed.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void NoHandlerForEvent_ShouldStartWithPrefix()
        => ProjectionErrorCodes.NoHandlerForEvent.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void AlreadyRunning_ShouldStartWithPrefix()
        => ProjectionErrorCodes.AlreadyRunning.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void Cancelled_ShouldStartWithPrefix()
        => ProjectionErrorCodes.Cancelled.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void StatusFailed_ShouldStartWithPrefix()
        => ProjectionErrorCodes.StatusFailed.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void NotRegistered_ShouldStartWithPrefix()
        => ProjectionErrorCodes.NotRegistered.ShouldStartWith(ProjectionErrorCodes.Prefix);

    [Fact]
    public void AllCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            ProjectionErrorCodes.ReadModelNotFound,
            ProjectionErrorCodes.StoreFailed,
            ProjectionErrorCodes.DeleteFailed,
            ProjectionErrorCodes.QueryFailed,
            ProjectionErrorCodes.ApplyFailed,
            ProjectionErrorCodes.RebuildFailed,
            ProjectionErrorCodes.NoHandlerForEvent,
            ProjectionErrorCodes.AlreadyRunning,
            ProjectionErrorCodes.Cancelled,
            ProjectionErrorCodes.StatusFailed,
            ProjectionErrorCodes.NotRegistered
        };
        codes.Distinct().Count().ShouldBe(codes.Length);
    }
}
