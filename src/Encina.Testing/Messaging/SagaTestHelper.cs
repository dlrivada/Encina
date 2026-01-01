using System.Text.Json;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Time;

namespace Encina.Testing.Messaging;

/// <summary>
/// Test helper for the Saga pattern using Given/When/Then style.
/// </summary>
/// <remarks>
/// <para>
/// This helper simplifies testing saga orchestration scenarios:
/// </para>
/// <list type="bullet">
/// <item><description><b>Given</b>: Set up existing saga state</description></item>
/// <item><description><b>When</b>: Execute saga steps, transitions, or timeouts</description></item>
/// <item><description><b>Then</b>: Verify saga status, step, and data</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var helper = new SagaTestHelper();
///
/// helper
///     .GivenRunningSaga&lt;OrderSaga&gt;(sagaId, new OrderSagaData { OrderId = orderId })
///     .WhenSagaCompletes(sagaId)
///     .ThenSagaStatus(sagaId, SagaStatus.Completed)
///     .ThenSagaHasCompletedAt(sagaId);
/// </code>
/// </example>
public sealed class SagaTestHelper : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly FakeSagaStore _store;
    private readonly FakeTimeProvider _timeProvider;
    private bool _whenExecuted;
    private Exception? _caughtException;
    private Guid _lastSagaId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTestHelper"/> class.
    /// </summary>
    public SagaTestHelper()
        : this(new FakeSagaStore(), new FakeTimeProvider())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTestHelper"/> class with a specific start time.
    /// </summary>
    /// <param name="startTime">The initial time for the test.</param>
    public SagaTestHelper(DateTimeOffset startTime)
        : this(new FakeSagaStore(), new FakeTimeProvider(startTime))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTestHelper"/> class with existing stores.
    /// </summary>
    /// <param name="store">The fake saga store to use.</param>
    /// <param name="timeProvider">The fake time provider to use.</param>
    public SagaTestHelper(FakeSagaStore store, FakeTimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _store = store;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the underlying fake saga store.
    /// </summary>
    public FakeSagaStore Store => _store;

    /// <summary>
    /// Gets the fake time provider.
    /// </summary>
    public FakeTimeProvider TimeProvider => _timeProvider;

    #region Given

    /// <summary>
    /// Sets up an empty saga store with no sagas.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper GivenNoSagas()
    {
        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastSagaId = Guid.Empty;
        return this;
    }

    /// <summary>
    /// Sets up a new (just started) saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The initial saga data.</param>
    /// <param name="timeout">Optional timeout duration from now.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper GivenNewSaga<TSaga, TData>(
        Guid sagaId,
        TData data,
        TimeSpan? timeout = null)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastSagaId = sagaId;

        var saga = new FakeSagaState
        {
            SagaId = sagaId,
            SagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name,
            Data = JsonSerializer.Serialize(data, JsonOptions),
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            TimeoutAtUtc = timeout.HasValue
                ? _timeProvider.GetUtcNow().UtcDateTime.Add(timeout.Value)
                : null
        };

        _store.AddAsync(saga).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a running saga at a specific step.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The saga data.</param>
    /// <param name="currentStep">The current step number.</param>
    /// <param name="timeout">Optional timeout duration from now.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper GivenRunningSaga<TSaga, TData>(
        Guid sagaId,
        TData data,
        int currentStep = 1,
        TimeSpan? timeout = null)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastSagaId = sagaId;

        var saga = new FakeSagaState
        {
            SagaId = sagaId,
            SagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name,
            Data = JsonSerializer.Serialize(data, JsonOptions),
            Status = SagaStatus.Running,
            CurrentStep = currentStep,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5),
            LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            TimeoutAtUtc = timeout.HasValue
                ? _timeProvider.GetUtcNow().UtcDateTime.Add(timeout.Value)
                : null
        };

        _store.AddAsync(saga).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a completed saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The final saga data.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper GivenCompletedSaga<TSaga, TData>(Guid sagaId, TData data)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastSagaId = sagaId;

        var saga = new FakeSagaState
        {
            SagaId = sagaId,
            SagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name,
            Data = JsonSerializer.Serialize(data, JsonOptions),
            Status = SagaStatus.Completed,
            CurrentStep = FakeSagaState.CompletedStep,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            CompletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1),
            LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1)
        };

        _store.AddAsync(saga).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a saga that is currently compensating.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The saga data.</param>
    /// <param name="failedAtStep">The step where the saga failed.</param>
    /// <param name="errorMessage">The error that triggered compensation.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper GivenCompensatingSaga<TSaga, TData>(
        Guid sagaId,
        TData data,
        int failedAtStep = 2,
        string errorMessage = "Step failed")
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastSagaId = sagaId;

        var saga = new FakeSagaState
        {
            SagaId = sagaId,
            SagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name,
            Data = JsonSerializer.Serialize(data, JsonOptions),
            Status = SagaStatus.Compensating,
            CurrentStep = failedAtStep,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ErrorMessage = errorMessage
        };

        _store.AddAsync(saga).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a failed saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The saga data.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper GivenFailedSaga<TSaga, TData>(
        Guid sagaId,
        TData data,
        string errorMessage = "Saga failed")
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        _store.Clear();
        _whenExecuted = false;
        _caughtException = null;
        _lastSagaId = sagaId;

        var saga = new FakeSagaState
        {
            SagaId = sagaId,
            SagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name,
            Data = JsonSerializer.Serialize(data, JsonOptions),
            Status = SagaStatus.Failed,
            CurrentStep = 0,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            CompletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ErrorMessage = errorMessage
        };

        _store.AddAsync(saga).GetAwaiter().GetResult();
        return this;
    }

    /// <summary>
    /// Sets up a saga that will time out.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The saga data.</param>
    /// <param name="timeoutIn">Duration until timeout (from now).</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper GivenSagaWithTimeout<TSaga, TData>(
        Guid sagaId,
        TData data,
        TimeSpan timeoutIn)
        where TData : class
    {
        return GivenRunningSaga<TSaga, TData>(sagaId, data, currentStep: 1, timeout: timeoutIn);
    }

    #endregion

    #region When

    /// <summary>
    /// Starts a new saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The initial saga data.</param>
    /// <param name="timeout">Optional timeout duration.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenSagaStarts<TSaga, TData>(
        Guid sagaId,
        TData data,
        TimeSpan? timeout = null)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        _lastSagaId = sagaId;

        return WhenAsync(async () =>
        {
            var saga = new FakeSagaState
            {
                SagaId = sagaId,
                SagaType = typeof(TSaga).FullName ?? typeof(TSaga).Name,
                Data = JsonSerializer.Serialize(data, JsonOptions),
                Status = SagaStatus.Running,
                CurrentStep = 0,
                StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                TimeoutAtUtc = timeout.HasValue
                    ? _timeProvider.GetUtcNow().UtcDateTime.Add(timeout.Value)
                    : null
            };

            await _store.AddAsync(saga);
        });
    }

    /// <summary>
    /// Advances a saga to the next step.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenSagaAdvancesToNextStep(Guid sagaId)
    {
        _lastSagaId = sagaId;

        return WhenAsync(async () =>
        {
            var saga = await _store.GetAsync(sagaId);
            if (saga is null)
            {
                throw new InvalidOperationException($"Saga {sagaId} not found");
            }

            var updated = new FakeSagaState
            {
                SagaId = saga.SagaId,
                SagaType = saga.SagaType,
                Data = saga.Data,
                Status = saga.Status,
                CurrentStep = saga.CurrentStep + 1,
                StartedAtUtc = saga.StartedAtUtc,
                CompletedAtUtc = saga.CompletedAtUtc,
                ErrorMessage = saga.ErrorMessage,
                LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                TimeoutAtUtc = saga.TimeoutAtUtc
            };

            await _store.UpdateAsync(updated);
        });
    }

    /// <summary>
    /// Updates the saga data.
    /// </summary>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="data">The new saga data.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenSagaDataUpdated<TData>(Guid sagaId, TData data)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);

        _lastSagaId = sagaId;

        return WhenAsync(async () =>
        {
            var saga = await _store.GetAsync(sagaId);
            if (saga is null)
            {
                throw new InvalidOperationException($"Saga {sagaId} not found");
            }

            var updated = new FakeSagaState
            {
                SagaId = saga.SagaId,
                SagaType = saga.SagaType,
                Data = JsonSerializer.Serialize(data, JsonOptions),
                Status = saga.Status,
                CurrentStep = saga.CurrentStep,
                StartedAtUtc = saga.StartedAtUtc,
                CompletedAtUtc = saga.CompletedAtUtc,
                ErrorMessage = saga.ErrorMessage,
                LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                TimeoutAtUtc = saga.TimeoutAtUtc
            };

            await _store.UpdateAsync(updated);
        });
    }

    /// <summary>
    /// Completes a saga successfully.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenSagaCompletes(Guid sagaId)
    {
        _lastSagaId = sagaId;

        return WhenAsync(async () =>
        {
            var saga = await _store.GetAsync(sagaId);
            if (saga is null)
            {
                throw new InvalidOperationException($"Saga {sagaId} not found");
            }

            var updated = new FakeSagaState
            {
                SagaId = saga.SagaId,
                SagaType = saga.SagaType,
                Data = saga.Data,
                Status = SagaStatus.Completed,
                CurrentStep = saga.CurrentStep,
                StartedAtUtc = saga.StartedAtUtc,
                CompletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                ErrorMessage = null,
                LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                TimeoutAtUtc = saga.TimeoutAtUtc
            };

            await _store.UpdateAsync(updated);
        });
    }

    /// <summary>
    /// Starts compensation for a saga.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="errorMessage">The error that triggered compensation.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenSagaStartsCompensating(Guid sagaId, string errorMessage = "Step failed")
    {
        _lastSagaId = sagaId;

        return WhenAsync(async () =>
        {
            var saga = await _store.GetAsync(sagaId);
            if (saga is null)
            {
                throw new InvalidOperationException($"Saga {sagaId} not found");
            }

            var updated = new FakeSagaState
            {
                SagaId = saga.SagaId,
                SagaType = saga.SagaType,
                Data = saga.Data,
                Status = SagaStatus.Compensating,
                CurrentStep = saga.CurrentStep,
                StartedAtUtc = saga.StartedAtUtc,
                CompletedAtUtc = saga.CompletedAtUtc,
                ErrorMessage = errorMessage,
                LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                TimeoutAtUtc = saga.TimeoutAtUtc
            };

            await _store.UpdateAsync(updated);
        });
    }

    /// <summary>
    /// Fails a saga completely.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenSagaFails(Guid sagaId, string errorMessage = "Saga failed")
    {
        _lastSagaId = sagaId;

        return WhenAsync(async () =>
        {
            var saga = await _store.GetAsync(sagaId);
            if (saga is null)
            {
                throw new InvalidOperationException($"Saga {sagaId} not found");
            }

            var updated = new FakeSagaState
            {
                SagaId = saga.SagaId,
                SagaType = saga.SagaType,
                Data = saga.Data,
                Status = SagaStatus.Failed,
                CurrentStep = saga.CurrentStep,
                StartedAtUtc = saga.StartedAtUtc,
                CompletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                ErrorMessage = errorMessage,
                LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                TimeoutAtUtc = saga.TimeoutAtUtc
            };

            await _store.UpdateAsync(updated);
        });
    }

    /// <summary>
    /// Times out a saga.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenSagaTimesOut(Guid sagaId)
    {
        _lastSagaId = sagaId;

        return WhenAsync(async () =>
        {
            var saga = await _store.GetAsync(sagaId);
            if (saga is null)
            {
                throw new InvalidOperationException($"Saga {sagaId} not found");
            }

            var updated = new FakeSagaState
            {
                SagaId = saga.SagaId,
                SagaType = saga.SagaType,
                Data = saga.Data,
                Status = SagaStatus.TimedOut,
                CurrentStep = saga.CurrentStep,
                StartedAtUtc = saga.StartedAtUtc,
                CompletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                ErrorMessage = "Saga timed out",
                LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                TimeoutAtUtc = saga.TimeoutAtUtc
            };

            await _store.UpdateAsync(updated);
        });
    }

    /// <summary>
    /// Executes a custom action on the store.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper When(Action<FakeSagaStore> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action(_store);
            _caughtException = null;
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }

        _whenExecuted = true;
        return this;
    }

    /// <summary>
    /// Executes a custom async action on the store.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper WhenAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action().GetAwaiter().GetResult();
            _caughtException = null;
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }

        _whenExecuted = true;
        return this;
    }

    #endregion

    #region Then

    /// <summary>
    /// Asserts that no exception was thrown during the When phase.
    /// </summary>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenNoException()
    {
        EnsureWhenExecuted();

        if (_caughtException is not null)
        {
            throw new InvalidOperationException(
                $"Expected no exception but got {_caughtException.GetType().Name}: {_caughtException.Message}",
                _caughtException);
        }

        return this;
    }

    /// <summary>
    /// Asserts that an exception of the specified type was thrown.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>The caught exception for further assertions.</returns>
    public TException ThenThrows<TException>() where TException : Exception
    {
        EnsureWhenExecuted();

        if (_caughtException is null)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but no exception was thrown");
        }

        if (_caughtException is not TException typedException)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but got {_caughtException.GetType().Name}: {_caughtException.Message}");
        }

        return typedException;
    }

    /// <summary>
    /// Asserts that the saga has the expected status.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="expectedStatus">The expected status.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaStatus(Guid sagaId, string expectedStatus)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var saga = _store.GetSaga(sagaId);
        if (saga is null)
        {
            throw new InvalidOperationException($"Saga {sagaId} not found.");
        }

        if (saga.Status != expectedStatus)
        {
            throw new InvalidOperationException(
                $"Expected saga status to be '{expectedStatus}' but was '{saga.Status}'.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the saga is running.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaIsRunning(Guid sagaId)
    {
        return ThenSagaStatus(sagaId, SagaStatus.Running);
    }

    /// <summary>
    /// Asserts that the saga has completed.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaIsCompleted(Guid sagaId)
    {
        return ThenSagaStatus(sagaId, SagaStatus.Completed);
    }

    /// <summary>
    /// Asserts that the saga is compensating.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaIsCompensating(Guid sagaId)
    {
        return ThenSagaStatus(sagaId, SagaStatus.Compensating);
    }

    /// <summary>
    /// Asserts that the saga has failed.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaHasFailed(Guid sagaId)
    {
        return ThenSagaStatus(sagaId, SagaStatus.Failed);
    }

    /// <summary>
    /// Asserts that the saga has timed out.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaHasTimedOut(Guid sagaId)
    {
        return ThenSagaStatus(sagaId, SagaStatus.TimedOut);
    }

    /// <summary>
    /// Asserts that the saga is at the expected step.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="expectedStep">The expected step number.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaIsAtStep(Guid sagaId, int expectedStep)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var saga = _store.GetSaga(sagaId);
        if (saga is null)
        {
            throw new InvalidOperationException($"Saga {sagaId} not found.");
        }

        if (saga.CurrentStep != expectedStep)
        {
            throw new InvalidOperationException(
                $"Expected saga to be at step {expectedStep} but was at step {saga.CurrentStep}.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the saga data matches the predicate.
    /// </summary>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="predicate">The predicate to validate the saga data.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaData<TData>(Guid sagaId, Func<TData, bool> predicate)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureWhenExecuted();
        ThenNoException();

        var saga = _store.GetSaga(sagaId);
        if (saga is null)
        {
            throw new InvalidOperationException($"Saga {sagaId} not found.");
        }

        var data = JsonSerializer.Deserialize<TData>(saga.Data, JsonOptions);
        if (data is null || !predicate(data))
        {
            throw new InvalidOperationException(
                "Saga data did not match the expected predicate.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that a saga of the specified type was started.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaWasStarted<TSaga>()
    {
        EnsureWhenExecuted();
        ThenNoException();

        if (!_store.WasSagaStarted<TSaga>())
        {
            throw new InvalidOperationException(
                $"Expected saga of type '{typeof(TSaga).Name}' to be started but it was not.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the saga has a CompletedAt timestamp.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaHasCompletedAt(Guid sagaId)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var saga = _store.GetSaga(sagaId);
        if (saga is null)
        {
            throw new InvalidOperationException($"Saga {sagaId} not found.");
        }

        if (!saga.CompletedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                $"Expected saga {sagaId} to have a CompletedAtUtc timestamp but it was null.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the saga has an error message.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="expectedMessage">Optional expected error message (partial match).</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper ThenSagaHasError(Guid sagaId, string? expectedMessage = null)
    {
        EnsureWhenExecuted();
        ThenNoException();

        var saga = _store.GetSaga(sagaId);
        if (saga is null)
        {
            throw new InvalidOperationException($"Saga {sagaId} not found.");
        }

        if (string.IsNullOrEmpty(saga.ErrorMessage))
        {
            throw new InvalidOperationException(
                $"Expected saga {sagaId} to have an error message but it was null or empty.");
        }

        if (expectedMessage is not null && !saga.ErrorMessage.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Expected saga error message to contain '{expectedMessage}' but was '{saga.ErrorMessage}'.");
        }

        return this;
    }

    /// <summary>
    /// Gets the saga data for further assertions.
    /// </summary>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>The saga data.</returns>
    public TData GetSagaData<TData>(Guid sagaId) where TData : class
    {
        EnsureWhenExecuted();

        var saga = _store.GetSaga(sagaId);
        if (saga is null)
        {
            throw new InvalidOperationException($"Saga {sagaId} not found.");
        }

        return JsonSerializer.Deserialize<TData>(saga.Data, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize saga data to {typeof(TData).Name}");
    }

    #endregion

    #region Time Control

    /// <summary>
    /// Advances time by the specified duration.
    /// </summary>
    /// <param name="duration">The duration to advance.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper AdvanceTimeBy(TimeSpan duration)
    {
        _timeProvider.Advance(duration);
        return this;
    }

    /// <summary>
    /// Advances time to trigger saga timeout.
    /// </summary>
    /// <param name="sagaId">The saga ID to check timeout for.</param>
    /// <returns>This helper for method chaining.</returns>
    public SagaTestHelper AdvanceTimePastTimeout(Guid sagaId)
    {
        var saga = _store.GetSaga(sagaId);
        if (saga?.TimeoutAtUtc is not null)
        {
            var duration = saga.TimeoutAtUtc.Value - _timeProvider.GetUtcNow().UtcDateTime + TimeSpan.FromSeconds(1);
            if (duration > TimeSpan.Zero)
            {
                _timeProvider.Advance(duration);
            }
        }

        return this;
    }

    #endregion

    private void EnsureWhenExecuted()
    {
        if (!_whenExecuted)
        {
            throw new InvalidOperationException(
                "When() must be called before Then assertions");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _store.Clear();
    }
}
