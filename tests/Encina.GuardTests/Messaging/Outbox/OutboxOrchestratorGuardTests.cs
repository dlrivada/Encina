using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.Outbox;

/// <summary>
/// Guard clause tests for OutboxOrchestrator constructor and method parameters.
/// </summary>
public class OutboxOrchestratorGuardTests
{
    private readonly IOutboxStore _store = Substitute.For<IOutboxStore>();
    private readonly OutboxOptions _options = new();
    private readonly ILogger<OutboxOrchestrator> _logger = NullLogger<OutboxOrchestrator>.Instance;
    private readonly IOutboxMessageFactory _messageFactory = Substitute.For<IOutboxMessageFactory>();
    private readonly IMessageSerializer _messageSerializer = Substitute.For<IMessageSerializer>();

    private OutboxOrchestrator CreateSut() =>
        new(_store, _options, _logger, _messageFactory, _messageSerializer);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new OutboxOrchestrator(null!, _options, _logger, _messageFactory, _messageSerializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new OutboxOrchestrator(_store, null!, _logger, _messageFactory, _messageSerializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new OutboxOrchestrator(_store, _options, null!, _messageFactory, _messageSerializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new OutboxOrchestrator(_store, _options, _logger, null!, _messageSerializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("messageFactory");
    }

    [Fact]
    public void Constructor_NullMessageSerializer_ThrowsArgumentNullException()
    {
        var act = () => new OutboxOrchestrator(_store, _options, _logger, _messageFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("messageSerializer");
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        var act = () => new OutboxOrchestrator(_store, _options, _logger, _messageFactory, _messageSerializer, timeProvider: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var sut = CreateSut();
        sut.Should().NotBeNull();
    }

    #endregion

    #region AddAsync Guards

    [Fact]
    public async Task AddAsync_NullNotification_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.AddAsync<TestNotification>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("notification");
    }

    [Fact]
    public async Task AddAsync_StoreAddFails_ReturnsError()
    {
        var expectedError = EncinaError.New("store failure");
        _messageSerializer.Serialize(Arg.Any<object>()).Returns("{}");
        _messageFactory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Substitute.For<IOutboxMessage>());
        _store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Unit>.Left(expectedError));

        var sut = CreateSut();
        var result = await sut.AddAsync(new TestNotification());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_ValidNotification_ReturnsSuccess()
    {
        _messageSerializer.Serialize(Arg.Any<object>()).Returns("{}");
        var outboxMsg = Substitute.For<IOutboxMessage>();
        outboxMsg.Id.Returns(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(outboxMsg);
        _store.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Unit.Default));

        var sut = CreateSut();
        var result = await sut.AddAsync(new TestNotification());

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region ProcessPendingMessagesAsync Guards

    [Fact]
    public async Task ProcessPendingMessagesAsync_NullCallback_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.ProcessPendingMessagesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("publishCallback");
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_StoreGetFails_ReturnsError()
    {
        var expectedError = EncinaError.New("store failure");
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IOutboxMessage>>.Left(expectedError));

        var sut = CreateSut();
        var result = await sut.ProcessPendingMessagesAsync((_, _, _) => Task.CompletedTask);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_EmptyBatch_ReturnsZero()
    {
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(
                Enumerable.Empty<IOutboxMessage>()));

        var sut = CreateSut();
        var result = await sut.ProcessPendingMessagesAsync((_, _, _) => Task.CompletedTask);

        result.IsRight.Should().BeTrue();
        result.Match(Right: count => count, Left: _ => -1).Should().Be(0);
    }

    #endregion

    #region GetPendingCountAsync Error Propagation

    [Fact]
    public async Task GetPendingCountAsync_StoreGetFails_ReturnsError()
    {
        var expectedError = EncinaError.New("store failure");
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IOutboxMessage>>.Left(expectedError));

        var sut = CreateSut();
        var result = await sut.GetPendingCountAsync();

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingCountAsync_EmptyStore_ReturnsZero()
    {
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(
                Enumerable.Empty<IOutboxMessage>()));

        var sut = CreateSut();
        var result = await sut.GetPendingCountAsync();

        result.IsRight.Should().BeTrue();
        result.Match(Right: count => count, Left: _ => -1).Should().Be(0);
    }

    #endregion

    private sealed class TestNotification
    {
        public string? Message { get; set; }
    }
}
