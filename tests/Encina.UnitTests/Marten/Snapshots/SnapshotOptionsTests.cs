using Encina.Marten.Snapshots;

namespace Encina.UnitTests.Marten.Snapshots;

public sealed class SnapshotOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new SnapshotOptions();

        options.Enabled.ShouldBeFalse();
        options.SnapshotEvery.ShouldBe(100);
        options.KeepSnapshots.ShouldBe(3);
        options.AsyncSnapshotCreation.ShouldBeTrue();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new SnapshotOptions
        {
            Enabled = true,
            SnapshotEvery = 50,
            KeepSnapshots = 5,
            AsyncSnapshotCreation = false
        };

        options.Enabled.ShouldBeTrue();
        options.SnapshotEvery.ShouldBe(50);
        options.KeepSnapshots.ShouldBe(5);
        options.AsyncSnapshotCreation.ShouldBeFalse();
    }
}
