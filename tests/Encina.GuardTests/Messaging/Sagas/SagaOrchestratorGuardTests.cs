using Encina.Messaging.Sagas;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.Sagas;

/// <summary>
/// Guard clause tests for SagaOrchestrator constructor and method parameters.
/// </summary>
public class SagaOrchestratorGuardTests
{
    private readonly ISagaStore _store = Substitute.For<ISagaStore>();
    private readonly SagaOptions _options = new();
    private readonly ILogger<SagaOrchestrator> _logger = NullLogger<SagaOrchestrator>.Instance;
    private readonly ISagaStateFactory _stateFactory = Substitute.For<ISagaStateFactory>();

    private SagaOrchestrator CreateSut() => new(_store, _options, _logger, _stateFactory);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(null!, _options, _logger, _stateFactory);
        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(_store, null!, _logger, _stateFactory);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(_store, _options, null!, _stateFactory);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullStateFactory_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(_store, _options, _logger, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stateFactory");
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        var act = () => new SagaOrchestrator(_store, _options, _logger, _stateFactory, timeProvider: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var sut = CreateSut();
        sut.Should().NotBeNull();
    }

    #endregion

    #region StartAsync Guards

    [Fact]
    public async Task StartAsync_NullSagaType_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var act = () => sut.StartAsync<TestSagaData>(null!, new TestSagaData());
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sagaType");
    }

    [Fact]
    public async Task StartAsync_EmptySagaType_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var act = () => sut.StartAsync<TestSagaData>(string.Empty, new TestSagaData());
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sagaType");
    }

    [Fact]
    public async Task StartAsync_WhitespaceSagaType_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var act = () => sut.StartAsync<TestSagaData>("   ", new TestSagaData());
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sagaType");
    }

    [Fact]
    public async Task StartAsync_NullData_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.StartAsync<TestSagaData>("TestSaga", (TestSagaData)null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("data");
    }

    [Fact]
    public async Task StartAsync_WithTimeout_NullSagaType_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var act = () => sut.StartAsync<TestSagaData>(null!, new TestSagaData(), TimeSpan.FromMinutes(5));
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sagaType");
    }

    [Fact]
    public async Task StartAsync_WithTimeout_NullData_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.StartAsync<TestSagaData>("TestSaga", null!, TimeSpan.FromMinutes(5));
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("data");
    }

    #endregion

    #region StartCompensationAsync Guards

    [Fact]
    public async Task StartCompensationAsync_NullErrorMessage_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var act = () => sut.StartCompensationAsync(Guid.NewGuid(), null!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public async Task StartCompensationAsync_EmptyErrorMessage_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var act = () => sut.StartCompensationAsync(Guid.NewGuid(), string.Empty);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public async Task StartCompensationAsync_WhitespaceErrorMessage_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var act = () => sut.StartCompensationAsync(Guid.NewGuid(), "   ");
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }

    #endregion

    #region StartAsync Store Error Propagation

    [Fact]
    public async Task StartAsync_StoreAddFails_ReturnsError()
    {
        var expectedError = EncinaError.New("store failure");
        _store.AddAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Unit>.Left(expectedError));
        _stateFactory.Create(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime?>())
            .Returns(Substitute.For<ISagaState>());

        var sut = CreateSut();
        var result = await sut.StartAsync("TestSaga", new TestSagaData());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_ValidInput_ReturnsSagaId()
    {
        _store.AddAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Unit.Default));
        _stateFactory.Create(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime?>())
            .Returns(Substitute.For<ISagaState>());

        var sut = CreateSut();
        var result = await sut.StartAsync("TestSaga", new TestSagaData());

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region AdvanceAsync Store Error Propagation

    [Fact]
    public async Task AdvanceAsync_SagaNotFound_ReturnsError()
    {
        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(LanguageExt.Option<ISagaState>.None));

        var sut = CreateSut();
        var result = await sut.AdvanceAsync<TestSagaData>(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AdvanceAsync_StoreGetFails_ReturnsError()
    {
        var expectedError = EncinaError.New("get failure");
        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Left(expectedError));

        var sut = CreateSut();
        var result = await sut.AdvanceAsync<TestSagaData>(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AdvanceAsync_SagaNotRunning_ReturnsError()
    {
        var sagaState = Substitute.For<ISagaState>();
        sagaState.Status.Returns(SagaStatus.Completed);
        sagaState.Data.Returns("{}");

        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(
                LanguageExt.Option<ISagaState>.Some(sagaState)));

        var sut = CreateSut();
        var result = await sut.AdvanceAsync<TestSagaData>(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region CompleteAsync Error Paths

    [Fact]
    public async Task CompleteAsync_SagaNotFound_ReturnsError()
    {
        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(LanguageExt.Option<ISagaState>.None));

        var sut = CreateSut();
        var result = await sut.CompleteAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_SagaNotRunning_ReturnsError()
    {
        var sagaState = Substitute.For<ISagaState>();
        sagaState.Status.Returns(SagaStatus.Compensated);

        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(
                LanguageExt.Option<ISagaState>.Some(sagaState)));

        var sut = CreateSut();
        var result = await sut.CompleteAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region CompensateStepAsync Error Paths

    [Fact]
    public async Task CompensateStepAsync_SagaNotFound_ReturnsError()
    {
        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(LanguageExt.Option<ISagaState>.None));

        var sut = CreateSut();
        var result = await sut.CompensateStepAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task CompensateStepAsync_SagaNotCompensating_ReturnsError()
    {
        var sagaState = Substitute.For<ISagaState>();
        sagaState.Status.Returns(SagaStatus.Running);

        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(
                LanguageExt.Option<ISagaState>.Some(sagaState)));

        var sut = CreateSut();
        var result = await sut.CompensateStepAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region TimeoutAsync Error Paths

    [Fact]
    public async Task TimeoutAsync_SagaNotFound_ReturnsError()
    {
        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(LanguageExt.Option<ISagaState>.None));

        var sut = CreateSut();
        var result = await sut.TimeoutAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task TimeoutAsync_SagaAlreadyCompleted_ReturnsError()
    {
        var sagaState = Substitute.For<ISagaState>();
        sagaState.Status.Returns(SagaStatus.Completed);

        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(
                LanguageExt.Option<ISagaState>.Some(sagaState)));

        var sut = CreateSut();
        var result = await sut.TimeoutAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task TimeoutAsync_SagaAlreadyFailed_ReturnsError()
    {
        var sagaState = Substitute.For<ISagaState>();
        sagaState.Status.Returns(SagaStatus.Failed);

        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(
                LanguageExt.Option<ISagaState>.Some(sagaState)));

        var sut = CreateSut();
        var result = await sut.TimeoutAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region StartCompensationAsync Status Validation

    [Fact]
    public async Task StartCompensationAsync_SagaAlreadyCompleted_ReturnsError()
    {
        var sagaState = Substitute.For<ISagaState>();
        sagaState.Status.Returns(SagaStatus.Completed);

        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(
                LanguageExt.Option<ISagaState>.Some(sagaState)));

        var sut = CreateSut();
        var result = await sut.StartCompensationAsync(Guid.NewGuid(), "test error");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task StartCompensationAsync_SagaRunning_Succeeds()
    {
        var sagaState = Substitute.For<ISagaState>();
        sagaState.Status.Returns(SagaStatus.Running);
        sagaState.CurrentStep.Returns(3);

        _store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Option<ISagaState>>.Right(
                LanguageExt.Option<ISagaState>.Some(sagaState)));
        _store.UpdateAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Unit.Default));

        var sut = CreateSut();
        var result = await sut.StartCompensationAsync(Guid.NewGuid(), "test error");

        result.IsRight.Should().BeTrue();
    }

    #endregion

    private sealed class TestSagaData
    {
        public string? Value { get; set; }
    }
}
