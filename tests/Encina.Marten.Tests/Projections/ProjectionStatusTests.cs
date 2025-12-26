using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

public sealed class ProjectionStatusTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var status = new ProjectionStatus();

        // Assert
        status.ProjectionName.ShouldBe(string.Empty);
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
    public void AllProperties_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var status = new ProjectionStatus
        {
            ProjectionName = "TestProjection",
            LastProcessedPosition = 100,
            LastProcessedAtUtc = now,
            State = ProjectionState.Running,
            EventsProcessed = 50,
            ErrorMessage = "Test error",
            StartedAtUtc = now.AddMinutes(-5),
            EventLag = 10,
            IsRebuilding = true,
            RebuildProgressPercent = 75,
        };

        // Assert
        status.ProjectionName.ShouldBe("TestProjection");
        status.LastProcessedPosition.ShouldBe(100);
        status.LastProcessedAtUtc.ShouldBe(now);
        status.State.ShouldBe(ProjectionState.Running);
        status.EventsProcessed.ShouldBe(50);
        status.ErrorMessage.ShouldBe("Test error");
        status.StartedAtUtc.ShouldBe(now.AddMinutes(-5));
        status.EventLag.ShouldBe(10);
        status.IsRebuilding.ShouldBeTrue();
        status.RebuildProgressPercent.ShouldBe(75);
    }

    [Theory]
    [InlineData(ProjectionState.Stopped)]
    [InlineData(ProjectionState.Starting)]
    [InlineData(ProjectionState.Running)]
    [InlineData(ProjectionState.CatchingUp)]
    [InlineData(ProjectionState.Rebuilding)]
    [InlineData(ProjectionState.Paused)]
    [InlineData(ProjectionState.Faulted)]
    [InlineData(ProjectionState.Stopping)]
    public void AllProjectionStates_AreDefined(ProjectionState state)
    {
        // Act
        var status = new ProjectionStatus { State = state };

        // Assert
        status.State.ShouldBe(state);
    }
}
