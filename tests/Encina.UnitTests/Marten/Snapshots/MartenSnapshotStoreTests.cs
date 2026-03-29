using Encina.DomainModeling;
using Encina.Marten.Snapshots;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.Marten.Snapshots;

public class MartenSnapshotStoreTests
{
    private readonly IDocumentSession _session;
    private readonly ILogger<MartenSnapshotStore<TestSnapshotAggregate>> _logger;

    public MartenSnapshotStoreTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _logger = NullLogger<MartenSnapshotStore<TestSnapshotAggregate>>.Instance;
    }

    private MartenSnapshotStore<TestSnapshotAggregate> CreateSut()
    {
        return new MartenSnapshotStore<TestSnapshotAggregate>(_session, _logger);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new MartenSnapshotStore<TestSnapshotAggregate>(null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MartenSnapshotStore<TestSnapshotAggregate>(_session, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    // SaveAsync tests

    [Fact]
    public async Task SaveAsync_NullSnapshot_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.SaveAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task SaveAsync_ValidSnapshot_StoresEnvelopeAndSaves()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate { Id = Guid.NewGuid(), Version = 10 };
        var snapshot = new Snapshot<TestSnapshotAggregate>(
            aggregate.Id, 10, aggregate, DateTime.UtcNow);

        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(snapshot);

        // Assert
        result.IsRight.ShouldBeTrue();
        _session.Received(1).Store(Arg.Any<SnapshotEnvelope<TestSnapshotAggregate>>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate { Id = Guid.NewGuid(), Version = 10 };
        var snapshot = new Snapshot<TestSnapshotAggregate>(
            aggregate.Id, 10, aggregate, DateTime.UtcNow);

        _session.When(s => s.Store(Arg.Any<SnapshotEnvelope<TestSnapshotAggregate>>()))
            .Do(_ => throw new InvalidOperationException("store error"));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(snapshot);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // PruneAsync tests

    [Fact]
    public async Task PruneAsync_NegativeKeepCount_ThrowsArgumentOutOfRangeException()
    {
        var sut = CreateSut();
        var act = () => sut.PruneAsync(Guid.NewGuid(), -1);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task PruneAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.Query<SnapshotEnvelope<TestSnapshotAggregate>>()
            .Throws(new InvalidOperationException("query error"));

        var sut = CreateSut();

        // Act
        var result = await sut.PruneAsync(id, 3);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // DeleteAllAsync tests

    [Fact]
    public async Task DeleteAllAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.Query<SnapshotEnvelope<TestSnapshotAggregate>>()
            .Throws(new InvalidOperationException("query error"));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAllAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ExistsAsync tests

    [Fact]
    public async Task ExistsAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.Query<SnapshotEnvelope<TestSnapshotAggregate>>()
            .Throws(new InvalidOperationException("query error"));

        var sut = CreateSut();

        // Act
        var result = await sut.ExistsAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // CountAsync tests

    [Fact]
    public async Task CountAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.Query<SnapshotEnvelope<TestSnapshotAggregate>>()
            .Throws(new InvalidOperationException("query error"));

        var sut = CreateSut();

        // Act
        var result = await sut.CountAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // Test types

    public sealed class TestSnapshotAggregate : AggregateBase, ISnapshotable<TestSnapshotAggregate>
    {
        public new Guid Id
        {
            get => base.Id;
            set => base.Id = value;
        }

        protected override void Apply(object domainEvent) { }
    }
}
