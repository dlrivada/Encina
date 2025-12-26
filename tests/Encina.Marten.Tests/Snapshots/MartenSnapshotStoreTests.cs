using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("session");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MartenSnapshotStore<TestSnapshotableAggregate>(
            _session,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task SaveAsync_NullSnapshot_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.SaveAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("snapshot");
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
        result.IsRight.Should().BeTrue();
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
        result.IsLeft.Should().BeTrue();
        result.LeftToSeq().First().Message.Should().Contain("Failed to save snapshot");
    }

    [Fact]
    public async Task PruneAsync_NegativeKeepCount_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = async () => await _sut.PruneAsync(Guid.NewGuid(), -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
