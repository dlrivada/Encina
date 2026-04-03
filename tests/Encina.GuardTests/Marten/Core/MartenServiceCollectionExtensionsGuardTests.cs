using Encina.Marten;

namespace Encina.GuardTests.Marten.Core;

/// <summary>
/// Additional guard tests for <see cref="ServiceCollectionExtensions"/> covering
/// AddProjection, AddEventUpcaster, and AddSnapshotableAggregate.
/// </summary>
public class MartenServiceCollectionExtensionsGuardTests
{
    #region AddProjection Guards

    [Fact]
    public void AddProjection_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddProjection<TestProjection, TestReadModel>());

    [Fact]
    public void AddProjection_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();
        Should.NotThrow(() => services.AddProjection<TestProjection, TestReadModel>());
    }

    #endregion

    #region AddEventUpcaster Guards

    [Fact]
    public void AddEventUpcaster_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEventUpcaster<TestUpcaster>());

    [Fact]
    public void AddEventUpcaster_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();
        Should.NotThrow(() => services.AddEventUpcaster<TestUpcaster>());
    }

    #endregion

    #region AddSnapshotableAggregate Guards

    [Fact]
    public void AddSnapshotableAggregate_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddSnapshotableAggregate<TestSnapshotableAggregate>());

    [Fact]
    public void AddSnapshotableAggregate_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();
        Should.NotThrow(() => services.AddSnapshotableAggregate<TestSnapshotableAggregate>());
    }

    #endregion

    #region AddEncinaMarten Parameterless

    [Fact]
    public void AddEncinaMarten_Parameterless_DoesNotThrow()
    {
        var services = new ServiceCollection();
        Should.NotThrow(() => services.AddEncinaMarten());
    }

    #endregion

    #region Test Types

    public class TestReadModel : global::Encina.Marten.Projections.IReadModel
    {
        public Guid Id { get; set; }
    }

    public class TestProjection : global::Encina.Marten.Projections.IProjection<TestReadModel>
    {
        public string ProjectionName => "TestProjection";
    }

    public class TestUpcaster : global::Encina.Marten.Versioning.IEventUpcaster
    {
        public string SourceEventTypeName => "TestEvent_v1";
        public Type TargetEventType => typeof(object);
        public Type SourceEventType => typeof(string);
    }

    public class TestSnapshotableAggregate : global::Encina.DomainModeling.AggregateBase,
        global::Encina.Marten.Snapshots.ISnapshotable<TestSnapshotableAggregate>
    {
        protected override void Apply(object domainEvent) { }

        public global::Encina.Marten.Snapshots.Snapshot<TestSnapshotableAggregate> CreateSnapshot()
            => new(Id, Version, this, DateTime.UtcNow);

        public static TestSnapshotableAggregate RestoreFromSnapshot(
            global::Encina.Marten.Snapshots.Snapshot<TestSnapshotableAggregate> snapshot)
            => snapshot.State;
    }

    #endregion
}
