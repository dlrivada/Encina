using System.Text.Json;
using Encina.Messaging.Sagas;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Messaging.Sagas;

/// <summary>
/// Extension methods for Either assertions in tests.
/// </summary>
internal static class EitherTestExtensions
{
    /// <summary>
    /// Asserts the Either is Right and returns the inner value.
    /// </summary>
    public static R ShouldBeRight<L, R>(this Either<L, R> either)
    {
        either.IsRight.ShouldBeTrue("Expected Right but got Left");
        return either.Match(Right: r => r, Left: _ => throw new InvalidOperationException("Unreachable"));
    }

    /// <summary>
    /// Asserts the Either is Left and returns the error.
    /// </summary>
    public static L ShouldBeLeft<L, R>(this Either<L, R> either)
    {
        either.IsLeft.ShouldBeTrue("Expected Left but got Right");
        return either.Match(Right: _ => throw new InvalidOperationException("Unreachable"), Left: l => l);
    }

    /// <summary>
    /// Asserts the Option is Some and returns the inner value.
    /// </summary>
    public static T ShouldBeSome<T>(this Option<T> option)
    {
        option.IsSome.ShouldBeTrue("Expected Some but got None");
        return option.Match(Some: v => v, None: () => throw new InvalidOperationException("Unreachable"));
    }
}

/// <summary>
/// Unit tests for SagaOrchestrator.
/// </summary>
public sealed class SagaOrchestratorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISagaStore _store;
    private readonly SagaOptions _options;
    private readonly ILogger<SagaOrchestrator> _logger;
    private readonly ISagaStateFactory _stateFactory;
    private readonly SagaOrchestrator _orchestrator;

    public SagaOrchestratorTests()
    {
        _store = Substitute.For<ISagaStore>();
        _options = new SagaOptions
        {
            StuckSagaThreshold = TimeSpan.FromMinutes(5),
            StuckSagaBatchSize = 100,
            ExpiredSagaBatchSize = 100,
            DefaultSagaTimeout = null
        };
        _logger = Substitute.For<ILogger<SagaOrchestrator>>();
        _stateFactory = Substitute.For<ISagaStateFactory>();

        _orchestrator = new SagaOrchestrator(_store, _options, _logger, _stateFactory);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(null!, _options, _logger, _stateFactory);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(_store, null!, _logger, _stateFactory);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(_store, _options, null!, _stateFactory);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullStateFactory_ThrowsArgumentNullException()
    {
        var act = () => new SagaOrchestrator(_store, _options, _logger, null!);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("stateFactory");
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ValidData_CreatesSagaAndReturnsId()
    {
        // Arrange
        var sagaType = "OrderSaga";
        var data = new TestSagaData { OrderId = Guid.NewGuid(), Amount = 100m };
        var sagaState = CreateTestSagaState(Guid.NewGuid(), sagaType);

        _stateFactory.Create(
            Arg.Any<Guid>(),
            sagaType,
            Arg.Any<string>(),
            SagaStatus.Running,
            0,
            Arg.Any<DateTime>(),
            Arg.Any<DateTime?>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.StartAsync(sagaType, data);

        // Assert
        var sagaId = result.ShouldBeRight();
        sagaId.ShouldNotBe(Guid.Empty);
        await _store.Received(1).AddAsync(sagaState, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithTimeout_CreatesSagaWithTimeoutSet()
    {
        // Arrange
        var sagaType = "OrderSaga";
        var data = new TestSagaData { OrderId = Guid.NewGuid(), Amount = 100m };
        var timeout = TimeSpan.FromMinutes(10);
        var sagaState = CreateTestSagaState(Guid.NewGuid(), sagaType);

        _stateFactory.Create(
            Arg.Any<Guid>(),
            sagaType,
            Arg.Any<string>(),
            SagaStatus.Running,
            0,
            Arg.Any<DateTime>(),
            Arg.Any<DateTime?>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.StartAsync(sagaType, data, timeout);

        // Assert
        result.IsRight.ShouldBeTrue();
        _stateFactory.Received(1).Create(
            Arg.Any<Guid>(),
            sagaType,
            Arg.Any<string>(),
            SagaStatus.Running,
            0,
            Arg.Any<DateTime>(),
            Arg.Is<DateTime?>(t => t.HasValue));
    }

    [Fact]
    public async Task StartAsync_NullSagaType_ThrowsArgumentException()
    {
        var data = new TestSagaData();

        var act = async () => await _orchestrator.StartAsync(null!, data);

        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartAsync_NullData_ThrowsArgumentNullException()
    {
        var act = async () => await _orchestrator.StartAsync<TestSagaData>("OrderSaga", null!);

        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region AdvanceAsync Tests

    [Fact]
    public async Task AdvanceAsync_RunningSaga_AdvancesStep()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Running, 1);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.AdvanceAsync<TestSagaData>(sagaId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var advance = result.ShouldBeRight();
        advance.SagaId.ShouldBe(sagaId);
        advance.CurrentStep.ShouldBe(2);

        await _store.Received(1).UpdateAsync(
            Arg.Is<ISagaState>(s => s.CurrentStep == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdvanceAsync_WithDataUpdate_UpdatesSagaData()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var initialData = new TestSagaData { OrderId = Guid.NewGuid(), Amount = 100m };
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Running, 1, initialData);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.AdvanceAsync<TestSagaData>(
            sagaId,
            data => data with { Amount = 150m });

        // Assert
        result.IsRight.ShouldBeTrue();
        result.ShouldBeRight().Data.Amount.ShouldBe(150m);
    }

    [Fact]
    public async Task AdvanceAsync_SagaNotFound_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns((ISagaState?)null);

        // Act
        var result = await _orchestrator.AdvanceAsync<TestSagaData>(sagaId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.ShouldBeLeft().Message.ShouldContain("not found");
    }

    [Fact]
    public async Task AdvanceAsync_SagaNotRunning_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Completed, 3);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.AdvanceAsync<TestSagaData>(sagaId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.ShouldBeLeft().Message.ShouldContain("status");
    }

    #endregion

    #region CompleteAsync Tests

    [Fact]
    public async Task CompleteAsync_RunningSaga_MarksSagaAsCompleted()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Running, 3);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.CompleteAsync(sagaId);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _store.Received(1).UpdateAsync(
            Arg.Is<ISagaState>(s => s.Status == SagaStatus.Completed && s.CompletedAtUtc.HasValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteAsync_SagaNotFound_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns((ISagaState?)null);

        // Act
        var result = await _orchestrator.CompleteAsync(sagaId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.ShouldBeLeft().Message.ShouldContain("not found");
    }

    [Fact]
    public async Task CompleteAsync_SagaNotRunning_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Compensating, 2);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.CompleteAsync(sagaId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.ShouldBeLeft().Message.ShouldContain("status");
    }

    #endregion

    #region StartCompensationAsync Tests

    [Fact]
    public async Task StartCompensationAsync_RunningSaga_StartCompensation()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Running, 3);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.StartCompensationAsync(sagaId, "Order failed");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.ShouldBeRight().ShouldBe(3);

        await _store.Received(1).UpdateAsync(
            Arg.Is<ISagaState>(s => s.Status == SagaStatus.Compensating && s.ErrorMessage == "Order failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartCompensationAsync_NullErrorMessage_ThrowsArgumentException()
    {
        var sagaId = Guid.NewGuid();

        var act = async () => await _orchestrator.StartCompensationAsync(sagaId, null!);

        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartCompensationAsync_CompletedSaga_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Completed, 3);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.StartCompensationAsync(sagaId, "Too late");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.ShouldBeLeft().Message.ShouldContain("status");
    }

    #endregion

    #region CompensateStepAsync Tests

    [Fact]
    public async Task CompensateStepAsync_CompensatingSaga_DecrementsStep()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Compensating, 3);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.CompensateStepAsync(sagaId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.ShouldBeRight().ShouldBe(2);
    }

    [Fact]
    public async Task CompensateStepAsync_LastStep_MarksSagaAsCompensated()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Compensating, 1);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.CompensateStepAsync(sagaId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.ShouldBeRight().ShouldBe(0);

        await _store.Received(1).UpdateAsync(
            Arg.Is<ISagaState>(s => s.Status == SagaStatus.Compensated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateStepAsync_NotCompensatingSaga_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Running, 2);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.CompensateStepAsync(sagaId);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region FailAsync Tests

    [Fact]
    public async Task FailAsync_AnySaga_MarksSagaAsFailed()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Compensating, 2);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.FailAsync(sagaId, "Compensation failed");

        // Assert
        result.IsRight.ShouldBeTrue();
        await _store.Received(1).UpdateAsync(
            Arg.Is<ISagaState>(s => s.Status == SagaStatus.Failed && s.ErrorMessage == "Compensation failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailAsync_SagaNotFound_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns((ISagaState?)null);

        // Act
        var result = await _orchestrator.FailAsync(sagaId, "Error");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.ShouldBeLeft().Message.ShouldContain("not found");
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ExistingSaga_ReturnsSagaSnapshot()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var data = new TestSagaData { OrderId = Guid.NewGuid(), Amount = 100m };
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Running, 2, data);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.GetAsync<TestSagaData>(sagaId);

        // Assert
        result.IsSome.ShouldBeTrue();
        var snapshot = result.ShouldBeSome();
        snapshot.SagaId.ShouldBe(sagaId);
        snapshot.SagaType.ShouldBe("OrderSaga");
        snapshot.CurrentStep.ShouldBe(2);
        snapshot.Data.Amount.ShouldBe(100m);
    }

    [Fact]
    public async Task GetAsync_NonExistentSaga_ReturnsNone()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns((ISagaState?)null);

        // Act
        var result = await _orchestrator.GetAsync<TestSagaData>(sagaId);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    #endregion

    #region TimeoutAsync Tests

    [Fact]
    public async Task TimeoutAsync_RunningSaga_MarksSagaAsTimedOut()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Running, 2);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.TimeoutAsync(sagaId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.ShouldBeRight().ShouldBe(2);

        await _store.Received(1).UpdateAsync(
            Arg.Is<ISagaState>(s => s.Status == SagaStatus.TimedOut),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TimeoutAsync_CompletedSaga_ReturnsError()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaState = CreateTestSagaState(sagaId, "OrderSaga", SagaStatus.Completed, 3);

        _store.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(sagaState);

        // Act
        var result = await _orchestrator.TimeoutAsync(sagaId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.ShouldBeLeft().Message.ShouldContain("status");
    }

    #endregion

    #region GetStuckSagasAsync Tests

    [Fact]
    public async Task GetStuckSagasAsync_DelegatesToStore()
    {
        // Arrange
        var stuckSagas = new[]
        {
            CreateTestSagaState(Guid.NewGuid(), "OrderSaga", SagaStatus.Running, 1),
            CreateTestSagaState(Guid.NewGuid(), "PaymentSaga", SagaStatus.Running, 2)
        };

        _store.GetStuckSagasAsync(
            _options.StuckSagaThreshold,
            _options.StuckSagaBatchSize,
            Arg.Any<CancellationToken>())
            .Returns(stuckSagas);

        // Act
        var result = await _orchestrator.GetStuckSagasAsync();

        // Assert
        var stuckResult = result.ShouldBeRight();
        stuckResult.Count().ShouldBe(2);
    }

    #endregion

    #region GetExpiredSagasAsync Tests

    [Fact]
    public async Task GetExpiredSagasAsync_DelegatesToStore()
    {
        // Arrange
        var expiredSagas = new[]
        {
            CreateTestSagaState(Guid.NewGuid(), "OrderSaga", SagaStatus.Running, 1),
        };

        _store.GetExpiredSagasAsync(
            _options.ExpiredSagaBatchSize,
            Arg.Any<CancellationToken>())
            .Returns(expiredSagas);

        // Act
        var result = await _orchestrator.GetExpiredSagasAsync();

        // Assert
        var expiredResult = result.ShouldBeRight();
        expiredResult.Count().ShouldBe(1);
    }

    #endregion

    #region Helpers

    private static TestSagaState CreateTestSagaState(
        Guid sagaId,
        string sagaType,
        string status = SagaStatus.Running,
        int currentStep = 0,
        TestSagaData? data = null)
    {
        var sagaData = data ?? new TestSagaData { OrderId = Guid.NewGuid(), Amount = 100m };
        return new TestSagaState
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Data = JsonSerializer.Serialize(sagaData, JsonOptions),
            Status = status,
            CurrentStep = currentStep,
            StartedAtUtc = FixedUtcNow.AddMinutes(-5),
            LastUpdatedAtUtc = FixedUtcNow
        };
    }

    #endregion
}

/// <summary>
/// Test saga data for unit tests.
/// </summary>
internal sealed record TestSagaData
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
}

/// <summary>
/// Test implementation of ISagaState for unit tests.
/// </summary>
internal sealed class TestSagaState : ISagaState
{
    public Guid SagaId { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string Status { get; set; } = SagaStatus.Running;
    public int CurrentStep { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? TimeoutAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
