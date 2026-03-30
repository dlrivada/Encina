using Encina.Marten.Projections;

namespace Encina.UnitTests.Marten.Projections;

/// <summary>
/// Unit tests for <see cref="RebuildOptions"/>.
/// </summary>
public sealed class RebuildOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var opts = new RebuildOptions();
        opts.BatchSize.ShouldBe(1000);
        opts.DeleteExisting.ShouldBeTrue();
        opts.OnProgress.ShouldBeNull();
        opts.StartPosition.ShouldBe(0);
        opts.EndPosition.ShouldBeNull();
        opts.RunInBackground.ShouldBeFalse();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var progressCalled = false;
        var opts = new RebuildOptions
        {
            BatchSize = 500,
            DeleteExisting = false,
            OnProgress = (_, _) => progressCalled = true,
            StartPosition = 100,
            EndPosition = 5000,
            RunInBackground = true
        };

        opts.BatchSize.ShouldBe(500);
        opts.DeleteExisting.ShouldBeFalse();
        opts.OnProgress.ShouldNotBeNull();
        opts.StartPosition.ShouldBe(100);
        opts.EndPosition.ShouldBe(5000);
        opts.RunInBackground.ShouldBeTrue();

        opts.OnProgress!(50, 2500);
        progressCalled.ShouldBeTrue();
    }
}
