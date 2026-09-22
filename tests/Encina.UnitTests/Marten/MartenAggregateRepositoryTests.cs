using Encina.DomainModeling;
using Encina.Marten;
using JasperFx.Events;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.Marten;

public class MartenAggregateRepositoryTests
{
    private readonly IDocumentSession _session;
    private readonly IDocumentStore _store;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<MartenAggregateRepository<TestAggregate>> _logger;
    private readonly IOptions<EncinaMartenOptions> _options;

    public MartenAggregateRepositoryTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _store = Substitute.For<IDocumentStore>();
        _requestContext = Substitute.For<IRequestContext>();
        _logger = NullLogger<MartenAggregateRepository<TestAggregate>>.Instance;
        _options = Options.Create(new EncinaMartenOptions
        {
            Metadata = { CorrelationIdEnabled = false, CausationIdEnabled = false, HeadersEnabled = false }
        });

        _session.DocumentStore.Returns(_store);
    }

    private MartenAggregateRepository<TestAggregate> CreateSut()
    {
        return new MartenAggregateRepository<TestAggregate>(
            _session, _requestContext, _logger, _options);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new MartenAggregateRepository<TestAggregate>(
            null!, _requestContext, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var act = () => new MartenAggregateRepository<TestAggregate>(
            _session, null!, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MartenAggregateRepository<TestAggregate>(
            _session, _requestContext, null!, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new MartenAggregateRepository<TestAggregate>(
            _session, _requestContext, _logger, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithMetadataEnabled_CreatesEnrichmentService()
    {
        // Arrange - default options have metadata enabled
        var optionsWithMetadata = Options.Create(new EncinaMartenOptions());

        // Act - should not throw
        var sut = new MartenAggregateRepository<TestAggregate>(
            _session, _requestContext, _logger, optionsWithMetadata);

        sut.ShouldNotBeNull();
    }

    // LoadAsync (by id + version) tests

    [Fact]
    public async Task LoadAsync_WithVersion_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(
            id, version: 3, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = CreateSut();

        // Act
        var result = await sut.LoadAsync(id, 3);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_WithVersion_NullResult_ReturnsLeft()
    {
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(
            id, version: 2, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IEvent>());

        var sut = CreateSut();
        var result = await sut.LoadAsync(id, 2);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_WithVersion_Success_ReturnsAggregateAtVersion()
    {
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(
            id, version: 5, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .Returns(StreamOf(id, 5));

        var sut = CreateSut();
        var result = await sut.LoadAsync(id, 5);

        result.IsRight.ShouldBeTrue();
        result.IfRight(a =>
        {
            a.Version.ShouldBe(5);
            a.Id.ShouldBe(id);
            a.UncommittedEvents.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task LoadAsync_StreamOfAnotherAggregateType_ReturnsAggregateNotFound()
    {
        // A stream whose events TestAggregate does not know: replay folds them as no-ops and never sets Id
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(id, version: 0, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .Returns([new Event<ForeignEvent>(new ForeignEvent()) { Version = 1, StreamId = id }]);
        var sut = CreateSut();

        var result = await sut.LoadAsync(id);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldContain("does not belong to aggregate TestAggregate"));
    }

    [Fact]
    public async Task LoadAsync_WithVersion_StreamOfAnotherAggregateType_ReturnsAggregateNotFound()
    {
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(id, version: 1, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .Returns([new Event<ForeignEvent>(new ForeignEvent()) { Version = 1, StreamId = id }]);
        var sut = CreateSut();

        var result = await sut.LoadAsync(id, 1);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_ById_NullResult_ReturnsLeft()
    {
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(
            id, version: 0, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IEvent>());

        var sut = CreateSut();
        var result = await sut.LoadAsync(id);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_ById_Success_ReplaysEventsAndSetsVersion()
    {
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(
            id, version: 0, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .Returns(StreamOf(id, 3));

        var sut = CreateSut();
        var result = await sut.LoadAsync(id);

        result.IsRight.ShouldBeTrue();
        result.IfRight(a =>
        {
            a.Version.ShouldBe(3);
            a.Id.ShouldBe(id);
            a.UncommittedEvents.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task LoadAsync_ById_ExceptionThrown_ReturnsLeft()
    {
        var id = Guid.NewGuid();
        _session.Events.FetchStreamAsync(
            id, version: 0, timestamp: null, fromVersion: 0, token: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        var sut = CreateSut();
        var result = await sut.LoadAsync(id);

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
    public async Task SaveAsync_NoUncommittedEvents_ReturnsRightUnit()
    {
        // Arrange
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };
        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithEvents_AppendsAndSaves()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        // Expected version passed to Marten is the stream version AFTER the append (= aggregate.Version)
        _session.Events.Received(1).Append(aggregate.Id, (long)aggregate.Version, Arg.Any<object[]>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_WithEvents_ClearsUncommittedEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        aggregate.UncommittedEvents.Count.ShouldBe(1);
        var sut = CreateSut();

        // Act
        await sut.SaveAsync(aggregate);

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task SaveAsync_ConcurrencyException_WithThrowOnConflictTrue_Throws()
    {
        // Arrange
        var options = Options.Create(new EncinaMartenOptions
        {
            ThrowOnConcurrencyConflict = true,
            Metadata = { CorrelationIdEnabled = false, CausationIdEnabled = false, HeadersEnabled = false }
        });

        var sut = new MartenAggregateRepository<TestAggregate>(
            _session, _requestContext, _logger, options);

        var aggregate = new TestAggregate();
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
        var aggregate = new TestAggregate();
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
    public async Task SaveAsync_NonConcurrencyException_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("general error"));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldContain("Failed to save"));
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
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };
        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldContain("without any events"));
    }

    [Fact]
    public async Task CreateAsync_WithEvents_StartsStreamAndSaves()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
        _session.Events.Received(1).StartStream<TestAggregate>(aggregate.Id, Arg.Any<object[]>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithEvents_ClearsUncommittedEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
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
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExistingStreamTestException("already exists"));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldContain("Stream already exists"));
    }

    [Fact]
    public async Task CreateAsync_NonStreamCollisionException_ReturnsLeft()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        _session.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("general error"));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(aggregate);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldContain("Failed to create"));
    }

    /// <summary>Builds a fake Marten stream of <paramref name="count"/> events for <paramref name="id"/>.</summary>
    private static List<IEvent> StreamOf(Guid id, int count)
    {
        var events = new List<IEvent>(count);
        for (var i = 1; i <= count; i++)
        {
            events.Add(new Event<TestEvent>(new TestEvent(id)) { Version = i, StreamId = id });
        }

        return events;
    }

    // Test types

    public sealed class TestAggregate : AggregateBase
    {
        public new Guid Id
        {
            get => base.Id;
            set => base.Id = value;
        }

        public void DoSomething()
        {
            RaiseEvent(new TestEvent(Guid.NewGuid()));
        }

        protected override void Apply(object domainEvent)
        {
            if (domainEvent is TestEvent te)
            {
                base.Id = te.Id;
            }
        }
    }

    public sealed record TestEvent(Guid Id);

    public sealed record ForeignEvent;

    // Custom exception types that match Marten's naming conventions
    private sealed class ConcurrencyTestException(string message) : Exception(message);
    private sealed class ExistingStreamTestException(string message) : Exception(message);
}
