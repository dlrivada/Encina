using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.Marten.Tests.Snapshots;

public sealed class MartenSnapshotStoreTests
{
    private readonly IDocumentSession _session;
    private readonly ILogger<MartenSnapshotStore<TestSnapshotableAggregate>> _logger;
    private readonly MartenSnapshotStore<TestSnapshotableAggregate> _sut;

    public MartenSnapshotStoreTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        _sut = new MartenSnapshotStore<TestSnapshotableAggregate>(_session, _logger);
    }

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MartenSnapshotStore<TestSnapshotableAggregate>(
            null!,
            _logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MartenSnapshotStore<TestSnapshotableAggregate>(
            _session,
            null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task SaveAsync_NullSnapshot_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = () => _sut.SaveAsync(null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("snapshot");
    }

    [Fact]
    public async Task SaveAsync_ValidSnapshot_StoresEnvelope()
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(), 10, aggregate, DateTime.UtcNow);

        // Act
        var result = await _sut.SaveAsync(snapshot);

        // Assert
        result.IsRight.ShouldBeTrue();
        _session.Received(1).Store(Arg.Any<SnapshotEnvelope<TestSnapshotableAggregate>>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WhenSessionThrows_ReturnsError()
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(), 10, aggregate, DateTime.UtcNow);

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _sut.SaveAsync(snapshot);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftToSeq().First().Message.ShouldContain("Failed to save snapshot");
    }

    [Fact]
    public async Task PruneAsync_NegativeKeepCount_ThrowsArgumentOutOfRangeException()
    {
        // Act
        Func<Task> act = () => _sut.PruneAsync(Guid.NewGuid(), -1);

        // Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }
}
