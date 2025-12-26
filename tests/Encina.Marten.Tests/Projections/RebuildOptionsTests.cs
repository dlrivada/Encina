using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

public sealed class RebuildOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new RebuildOptions();

        // Assert
        options.BatchSize.ShouldBe(1000);
        options.DeleteExisting.ShouldBeTrue();
        options.OnProgress.ShouldBeNull();
        options.StartPosition.ShouldBe(0);
        options.EndPosition.ShouldBeNull();
        options.RunInBackground.ShouldBeFalse();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Arrange
        var progressCallback = (int progress, long events) => { };

        // Act
        var options = new RebuildOptions
        {
            BatchSize = 500,
            DeleteExisting = false,
            OnProgress = progressCallback,
            StartPosition = 100,
            EndPosition = 1000,
            RunInBackground = true,
        };

        // Assert
        options.BatchSize.ShouldBe(500);
        options.DeleteExisting.ShouldBeFalse();
        options.OnProgress.ShouldBe(progressCallback);
        options.StartPosition.ShouldBe(100);
        options.EndPosition.ShouldBe(1000);
        options.RunInBackground.ShouldBeTrue();
    }

    [Fact]
    public void OnProgress_CanBeInvoked()
    {
        // Arrange
        var capturedProgress = 0;
        var capturedEvents = 0L;
        var options = new RebuildOptions
        {
            OnProgress = (progress, events) =>
            {
                capturedProgress = progress;
                capturedEvents = events;
            },
        };

        // Act
        options.OnProgress?.Invoke(50, 1000);

        // Assert
        capturedProgress.ShouldBe(50);
        capturedEvents.ShouldBe(1000);
    }
}
