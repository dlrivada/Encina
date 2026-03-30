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

    [Fact]
    public void ConfigureAggregate_SetsPerAggregateConfig()
    {
        var options = new SnapshotOptions();
        options.ConfigureAggregate<TestSnapAgg>(snapshotEvery: 25, keepSnapshots: 10);

        var config = options.GetConfigFor<TestSnapAgg>();
        config.SnapshotEvery.ShouldBe(25);
        config.KeepSnapshots.ShouldBe(10);
    }

    [Fact]
    public void ConfigureAggregate_ReturnsOptionsForChaining()
    {
        var options = new SnapshotOptions();
        var result = options.ConfigureAggregate<TestSnapAgg>(snapshotEvery: 50);
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ConfigureAggregate_WithoutKeepSnapshots_UsesGlobal()
    {
        var options = new SnapshotOptions { KeepSnapshots = 7 };
        options.ConfigureAggregate<TestSnapAgg>(snapshotEvery: 30);

        var config = options.GetConfigFor<TestSnapAgg>();
        config.SnapshotEvery.ShouldBe(30);
        config.KeepSnapshots.ShouldBe(7);
    }

    [Fact]
    public void ConfigureAggregate_ZeroSnapshotEvery_Throws()
    {
        var options = new SnapshotOptions();
        Should.Throw<ArgumentOutOfRangeException>(() =>
            options.ConfigureAggregate<TestSnapAgg>(snapshotEvery: 0));
    }

    [Fact]
    public void ConfigureAggregate_NegativeKeepSnapshots_Throws()
    {
        var options = new SnapshotOptions();
        Should.Throw<ArgumentOutOfRangeException>(() =>
            options.ConfigureAggregate<TestSnapAgg>(snapshotEvery: 10, keepSnapshots: -1));
    }

    [Fact]
    public void GetConfigFor_Unconfigured_ReturnsDefaults()
    {
        var options = new SnapshotOptions { SnapshotEvery = 200, KeepSnapshots = 5 };
        var config = options.GetConfigFor<TestSnapAgg>();
        config.SnapshotEvery.ShouldBe(200);
        config.KeepSnapshots.ShouldBe(5);
    }

    [Fact]
    public void GetConfigFor_ByType_ReturnsConfig()
    {
        var options = new SnapshotOptions();
        options.ConfigureAggregate<TestSnapAgg>(snapshotEvery: 15, keepSnapshots: 2);

        var config = options.GetConfigFor(typeof(TestSnapAgg));
        config.SnapshotEvery.ShouldBe(15);
        config.KeepSnapshots.ShouldBe(2);
    }

    [Fact]
    public void GetConfigFor_ByType_NullThrows()
    {
        var options = new SnapshotOptions();
        Should.Throw<ArgumentNullException>(() => options.GetConfigFor(null!));
    }

    [Fact]
    public void GetConfigFor_ByType_Unconfigured_ReturnsDefaults()
    {
        var options = new SnapshotOptions { SnapshotEvery = 150 };
        var config = options.GetConfigFor(typeof(TestSnapAgg));
        config.SnapshotEvery.ShouldBe(150);
    }

    public sealed class TestSnapAgg : global::Encina.DomainModeling.AggregateBase, ISnapshotable<TestSnapAgg>
    {
        protected override void Apply(object domainEvent) { }
    }
}
