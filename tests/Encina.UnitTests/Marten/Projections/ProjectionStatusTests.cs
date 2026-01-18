using Encina.Marten.Projections;

namespace Encina.UnitTests.Marten.Projections;

public sealed class ProjectionStatusTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var status = new ProjectionStatus();

        status.ProjectionName.ShouldBeEmpty();
        status.LastProcessedPosition.ShouldBe(0);
        status.LastProcessedAtUtc.ShouldBeNull();
        status.State.ShouldBe(ProjectionState.Stopped);
        status.EventsProcessed.ShouldBe(0);
        status.ErrorMessage.ShouldBeNull();
        status.StartedAtUtc.ShouldBeNull();
        status.EventLag.ShouldBeNull();
        status.IsRebuilding.ShouldBeFalse();
        status.RebuildProgressPercent.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var now = DateTime.UtcNow;

        var status = new ProjectionStatus
        {
            ProjectionName = "OrderProjection",
            LastProcessedPosition = 1000,
            LastProcessedAtUtc = now,
            State = ProjectionState.Running,
            EventsProcessed = 500,
            ErrorMessage = null,
            StartedAtUtc = now.AddHours(-1),
            EventLag = 10,
            IsRebuilding = false,
            RebuildProgressPercent = null
        };

        status.ProjectionName.ShouldBe("OrderProjection");
        status.LastProcessedPosition.ShouldBe(1000);
        status.State.ShouldBe(ProjectionState.Running);
        status.EventsProcessed.ShouldBe(500);
        status.EventLag.ShouldBe(10);
    }

    [Fact]
    public void RebuildingState_SetsCorrectProperties()
    {
        var status = new ProjectionStatus
        {
            State = ProjectionState.Rebuilding,
            IsRebuilding = true,
            RebuildProgressPercent = 75
        };

        status.State.ShouldBe(ProjectionState.Rebuilding);
        status.IsRebuilding.ShouldBeTrue();
        status.RebuildProgressPercent.ShouldBe(75);
    }

    [Fact]
    public void FaultedState_HasErrorMessage()
    {
        var status = new ProjectionStatus
        {
            State = ProjectionState.Faulted,
            ErrorMessage = "Database connection failed"
        };

        status.State.ShouldBe(ProjectionState.Faulted);
        status.ErrorMessage.ShouldBe("Database connection failed");
    }
}
