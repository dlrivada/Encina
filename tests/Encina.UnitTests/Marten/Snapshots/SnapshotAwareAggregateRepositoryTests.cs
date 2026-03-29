using Encina.DomainModeling;
using Encina.Marten;
using Encina.Marten.Snapshots;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten.Snapshots;

public class SnapshotAwareAggregateRepositoryTests
{
    private readonly IDocumentSession _session;
    private readonly ISnapshotStore<TestSnapshotAggregate> _snapshotStore;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<SnapshotAwareAggregateRepository<TestSnapshotAggregate>> _logger;
    private readonly EncinaMartenOptions _martenOptions;
    private readonly IOptions<EncinaMartenOptions> _options;

    public SnapshotAwareAggregateRepositoryTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _snapshotStore = Substitute.For<ISnapshotStore<TestSnapshotAggregate>>();
        _requestContext = Substitute.For<IRequestContext>();
        _logger = NullLogger<SnapshotAwareAggregateRepository<TestSnapshotAggregate>>.Instance;
        _martenOptions = new EncinaMartenOptions
        {
            Metadata = { CorrelationIdEnabled = false, CausationIdEnabled = false, HeadersEnabled = false }
        };
        _martenOptions.Snapshots.Enabled = true;
        _martenOptions.Snapshots.AsyncSnapshotCreation = false;
        _options = Options.Create(_martenOptions);
    }

    private SnapshotAwareAggregateRepository<TestSnapshotAggregate> CreateSut(
        TimeProvider? timeProvider = null)
    {
        return new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, _snapshotStore, _requestContext, _logger, _options,
            timeProvider: timeProvider);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            null!, _snapshotStore, _requestContext, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullSnapshotStore_ThrowsArgumentNullException()
    {
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, null!, _requestContext, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("snapshotStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, _snapshotStore, null!, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, _snapshotStore, _requestContext, null!, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, _snapshotStore, _requestContext, _logger, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    // LoadAsync (by id) tests

    [Fact]
    public async Task LoadAsync_SnapshotStoreReturnsError_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = EncinaErrors.Create("test", "snapshot store error");
        _snapshotStore.GetLatestAsync(id, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<Snapshot<TestSnapshotAggregate>>>(error));

        var sut = CreateSut();

        // Act
        var result = await sut.LoadAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _snapshotStore.GetLatestAsync(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected error"));

        var sut = CreateSut();

        // Act
        var result = await sut.LoadAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // LoadAsync (by id + version) tests

    [Fact]
    public async Task LoadAsync_WithVersion_SnapshotStoreReturnsError_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = EncinaErrors.Create("test", "snapshot store error");
        _snapshotStore.GetAtVersionAsync(id, 5, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<Snapshot<TestSnapshotAggregate>>>(error));

        var sut = CreateSut();

        // Act
        var result = await sut.LoadAsync(id, 5);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_WithVersion_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _snapshotStore.GetAtVersionAsync(id, 5, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.LoadAsync(id, 5);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // SaveAsync tests

    [Fact]
    public async Task SaveAsync_NullAggregate_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.SaveAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task SaveAsync_NoUncommittedEvents_ReturnsRight()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate { Id = Guid.NewGuid() };
        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithEvents_AppendsWithExpectedVersion()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();
        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        _session.Events.Received(1).Append(aggregate.Id, Arg.Any<long>(), Arg.Any<object[]>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithEvents_ClearsUncommittedEvents()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();
        aggregate.UncommittedEvents.Count.ShouldBe(1);

        var sut = CreateSut();

        // Act
        await sut.SaveAsync(aggregate);

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task SaveAsync_VersionMultipleOfSnapshotEvery_CreatesSnapshot()
    {
        // Arrange - set snapshot every 1 event so it always triggers
        _martenOptions.Snapshots.SnapshotEvery = 1;
        var options = Options.Create(_martenOptions);
        var fakeTime = Substitute.For<TimeProvider>();
        fakeTime.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var sut = new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, _snapshotStore, _requestContext, _logger, options,
            timeProvider: fakeTime);

        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();

        _snapshotStore.SaveAsync(Arg.Any<Snapshot<TestSnapshotAggregate>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        _snapshotStore.PruneAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _snapshotStore.Received(1).SaveAsync(
            Arg.Any<Snapshot<TestSnapshotAggregate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_VersionNotMultipleOfSnapshotEvery_DoesNotCreateSnapshot()
    {
        // Arrange - default snapshot every 100, version will be 1
        var fakeTime = Substitute.For<TimeProvider>();
        fakeTime.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var sut = CreateSut(fakeTime);

        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething(); // version will be 1

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _snapshotStore.DidNotReceive().SaveAsync(
            Arg.Any<Snapshot<TestSnapshotAggregate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_ConcurrencyException_WithThrowOnConflictTrue_Throws()
    {
        // Arrange
        _martenOptions.ThrowOnConcurrencyConflict = true;
        var options = Options.Create(_martenOptions);
        var sut = new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, _snapshotStore, _requestContext, _logger, options);

        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyTestException("conflict"));

        // Act & Assert
        await Should.ThrowAsync<ConcurrencyTestException>(() => sut.SaveAsync(aggregate));
    }

    [Fact]
    public async Task SaveAsync_ConcurrencyException_WithThrowOnConflictFalse_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyTestException("conflict"));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldContain("Concurrency conflict"));
    }

    [Fact]
    public async Task SaveAsync_GeneralException_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_SnapshotCreationFails_DoesNotFailSave()
    {
        // Arrange - set snapshot every 1 to trigger
        _martenOptions.Snapshots.SnapshotEvery = 1;
        var options = Options.Create(_martenOptions);
        var fakeTime = Substitute.For<TimeProvider>();
        fakeTime.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var sut = new SnapshotAwareAggregateRepository<TestSnapshotAggregate>(
            _session, _snapshotStore, _requestContext, _logger, options,
            timeProvider: fakeTime);

        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();

        _snapshotStore.SaveAsync(Arg.Any<Snapshot<TestSnapshotAggregate>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("snapshot save failed"));

        // Act - should not throw; snapshot failure is swallowed
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // CreateAsync tests

    [Fact]
    public async Task CreateAsync_NullAggregate_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.CreateAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task CreateAsync_NoUncommittedEvents_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithEvents_StartsStreamAndSaves()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();
        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        _session.Events.Received(1).StartStream<TestSnapshotAggregate>(aggregate.Id, Arg.Any<object[]>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithEvents_ClearsUncommittedEvents()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();
        var sut = CreateSut();

        // Act
        await sut.CreateAsync(aggregate);

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task CreateAsync_StreamCollisionException_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExistingStreamTestException("exists"));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_GeneralException_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestSnapshotAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // Test types

    public sealed class TestSnapshotAggregate : AggregateBase, ISnapshotable<TestSnapshotAggregate>
    {
        public string Name { get; set; } = string.Empty;

        public new Guid Id
        {
            get => base.Id;
            set => base.Id = value;
        }

        public TestSnapshotAggregate() { }

        public void DoSomething()
        {
            RaiseEvent(new TestSnapshotEvent(Guid.NewGuid(), "test"));
        }

        protected override void Apply(object domainEvent)
        {
            if (domainEvent is TestSnapshotEvent e)
            {
                base.Id = e.Id;
                Name = e.Name;
            }
        }
    }

    public sealed record TestSnapshotEvent(Guid Id, string Name);

    private sealed class ConcurrencyTestException(string message) : Exception(message);
    private sealed class ExistingStreamTestException(string message) : Exception(message);
}
