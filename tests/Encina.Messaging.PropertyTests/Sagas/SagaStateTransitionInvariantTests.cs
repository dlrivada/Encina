using Encina.Messaging.Sagas;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.Messaging.PropertyTests.Sagas;

/// <summary>
/// Property-based tests for Saga State Transition invariants.
/// </summary>
public sealed class SagaStateTransitionInvariantTests
{
    private readonly DateTime _fixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// All SagaErrorCodes string constants, discovered once via reflection at class load.
    /// </summary>
    private static readonly string[] AllSagaErrorCodes = typeof(SagaErrorCodes)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
        .Select(f => (string)f.GetValue(null)!)
        .ToArray();

    #region ISagaState Properties

    /// <summary>
    /// SagaId should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaId_IsPreserved(Guid sagaId)
    {
        var state = new TestSagaState
        {
            SagaId = sagaId
        };

        return state.SagaId == sagaId;
    }

    /// <summary>
    /// SagaType should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaType_IsPreserved(NonEmptyString sagaType)
    {
        var state = new TestSagaState
        {
            SagaType = sagaType.Get
        };

        return state.SagaType == sagaType.Get;
    }

    /// <summary>
    /// Data should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Data_IsPreserved(NonEmptyString data)
    {
        var state = new TestSagaState
        {
            Data = data.Get
        };

        return state.Data == data.Get;
    }

    /// <summary>
    /// Status should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Status_IsPreserved(NonEmptyString status)
    {
        var state = new TestSagaState
        {
            Status = status.Get
        };

        return state.Status == status.Get;
    }

    #endregion

    #region Saga Status Transition Invariants

    /// <summary>
    /// Each valid saga status should be a non-empty, non-whitespace string.
    /// Uses direct constant access for performance (&lt;1ms per test).
    /// </summary>
    [Theory]
    [InlineData(SagaStatus.Running)]
    [InlineData(SagaStatus.Completed)]
    [InlineData(SagaStatus.Compensating)]
    [InlineData(SagaStatus.Compensated)]
    [InlineData(SagaStatus.Failed)]
    [InlineData(SagaStatus.TimedOut)]
    public void SagaStatus_IsNonEmpty(string status)
    {
        Assert.False(string.IsNullOrWhiteSpace(status), $"SagaStatus value '{status}' should not be null or whitespace");
    }

    /// <summary>
    /// All saga statuses should be unique.
    /// Uses reflection to automatically detect any new status constants.
    /// </summary>
    [Fact]
    public void SagaStatus_AllStatusesAreUnique()
    {
        var statuses = typeof(SagaStatus)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToArray();

        Assert.True(statuses.Length > 0, "SagaStatus should have at least one status constant");
        Assert.Equal(statuses.Length, statuses.Distinct().Count());
    }

    /// <summary>
    /// Running saga can transition to Completed.
    /// </summary>
    [Fact]
    public void SagaStatusTransition_Running_CanTransitionToCompleted()
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Running
        };

        state.Status = SagaStatus.Completed;

        Assert.Equal(SagaStatus.Completed, state.Status);
    }

    /// <summary>
    /// Running saga can transition to Compensating.
    /// </summary>
    [Fact]
    public void SagaStatusTransition_Running_CanTransitionToCompensating()
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Running
        };

        state.Status = SagaStatus.Compensating;

        Assert.Equal(SagaStatus.Compensating, state.Status);
    }

    /// <summary>
    /// Running saga can transition to TimedOut.
    /// </summary>
    [Fact]
    public void SagaStatusTransition_Running_CanTransitionToTimedOut()
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Running
        };

        state.Status = SagaStatus.TimedOut;

        Assert.Equal(SagaStatus.TimedOut, state.Status);
    }

    /// <summary>
    /// Compensating saga can transition to Compensated.
    /// </summary>
    [Fact]
    public void SagaStatusTransition_Compensating_CanTransitionToCompensated()
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Compensating
        };

        state.Status = SagaStatus.Compensated;

        Assert.Equal(SagaStatus.Compensated, state.Status);
    }

    /// <summary>
    /// Compensating saga can transition to Failed.
    /// </summary>
    [Fact]
    public void SagaStatusTransition_Compensating_CanTransitionToFailed()
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Compensating
        };

        state.Status = SagaStatus.Failed;

        Assert.Equal(SagaStatus.Failed, state.Status);
    }

    #endregion

    #region Saga Step Invariants

    /// <summary>
    /// CurrentStep should reject negative values with ArgumentOutOfRangeException.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CurrentStep_RejectsNegativeValues(NegativeInt negativeStep)
    {
        var state = new TestSagaState();

        try
        {
            state.CurrentStep = negativeStep.Get;
            return false; // Should have thrown
        }
        catch (ArgumentOutOfRangeException)
        {
            return true; // Expected behavior
        }
        catch (Exception ex)
        {
            if (ex is OutOfMemoryException or StackOverflowException)
                throw;
            return false; // Wrong exception type
        }
    }

    /// <summary>
    /// CurrentStep should accept non-negative values.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CurrentStep_AcceptsNonNegativeValues(NonNegativeInt step)
    {
        var state = new TestSagaState
        {
            CurrentStep = step.Get
        };

        return state.CurrentStep == step.Get;
    }

    #endregion

    #region Saga Timestamp Invariants

    /// <summary>
    /// StartedAtUtc should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool StartedAtUtc_IsPreserved(DateTime startedAt)
    {
        var utcDate = DateTime.SpecifyKind(startedAt, DateTimeKind.Utc);
        var state = new TestSagaState
        {
            StartedAtUtc = utcDate
        };

        return state.StartedAtUtc == utcDate;
    }

    /// <summary>
    /// CompletedAtUtc should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CompletedAtUtc_IsPreserved(DateTime? completedAt)
    {
        var utcDate = completedAt.HasValue
            ? DateTime.SpecifyKind(completedAt.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var state = new TestSagaState
        {
            CompletedAtUtc = utcDate
        };

        return state.CompletedAtUtc == utcDate;
    }

    /// <summary>
    /// LastUpdatedAtUtc should always be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LastUpdatedAtUtc_IsPreserved(DateTime lastUpdated)
    {
        var utcDate = DateTime.SpecifyKind(lastUpdated, DateTimeKind.Utc);
        var state = new TestSagaState
        {
            LastUpdatedAtUtc = utcDate
        };

        return state.LastUpdatedAtUtc == utcDate;
    }

    /// <summary>
    /// TimeoutAtUtc should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TimeoutAtUtc_IsPreserved(DateTime? timeoutAt)
    {
        var utcDate = timeoutAt.HasValue
            ? DateTime.SpecifyKind(timeoutAt.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var state = new TestSagaState
        {
            TimeoutAtUtc = utcDate
        };

        return state.TimeoutAtUtc == utcDate;
    }

    /// <summary>
    /// CompletedAtUtc before StartedAtUtc should fail validation.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CompletedAtUtc_BeforeStartedAtUtc_FailsValidation(PositiveInt minutesBefore)
    {
        var completedAt = _fixedUtcNow.AddMinutes(-minutesBefore.Get);

        var state = new TestSagaState
        {
            StartedAtUtc = _fixedUtcNow,
            CompletedAtUtc = completedAt
        };

        return !state.IsValid();
    }

    /// <summary>
    /// CompletedAtUtc after StartedAtUtc should pass validation.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CompletedAtUtc_AfterStartedAtUtc_PassesValidation(PositiveInt minutesAfter)
    {
        var completedAt = _fixedUtcNow.AddMinutes(minutesAfter.Get);

        var state = new TestSagaState
        {
            StartedAtUtc = _fixedUtcNow,
            CompletedAtUtc = completedAt
        };

        return state.IsValid();
    }

    /// <summary>
    /// Null CompletedAtUtc should pass validation (saga not yet completed).
    /// </summary>
    [Fact]
    public void CompletedAtUtc_WhenNull_PassesValidation()
    {
        var state = new TestSagaState
        {
            StartedAtUtc = _fixedUtcNow,
            CompletedAtUtc = null
        };

        Assert.True(state.IsValid());
    }

    /// <summary>
    /// LastUpdatedAtUtc >= StartedAtUtc should pass validation.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LastUpdatedAtUtc_AtOrAfterStartedAtUtc_PassesValidation(PositiveInt minutesAfter)
    {
        var startedAt = _fixedUtcNow;
        var lastUpdated = startedAt.AddMinutes(minutesAfter.Get);

        var state = new TestSagaState
        {
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = lastUpdated
        };

        return state.IsValid();
    }

    /// <summary>
    /// LastUpdatedAtUtc < StartedAtUtc should fail validation.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LastUpdatedAtUtc_BeforeStartedAtUtc_FailsValidation(PositiveInt minutesBefore)
    {
        var startedAt = _fixedUtcNow;
        var lastUpdated = startedAt.AddMinutes(-minutesBefore.Get);

        var state = new TestSagaState
        {
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = lastUpdated
        };

        return !state.IsValid();
    }

    #endregion

    #region Saga ErrorMessage Invariants

    /// <summary>
    /// ErrorMessage should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ErrorMessage_IsPreserved(string? errorMessage)
    {
        var state = new TestSagaState
        {
            ErrorMessage = errorMessage
        };

        return state.ErrorMessage == errorMessage;
    }

    /// <summary>
    /// Failed sagas without an error message should fail validation.
    /// </summary>
    [Fact]
    public void FailedSaga_WithoutErrorMessage_FailsValidation()
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Failed,
            ErrorMessage = null
        };

        Assert.False(state.IsValid());
    }

    /// <summary>
    /// Failed sagas with empty error message should fail validation.
    /// </summary>
    [Fact]
    public void FailedSaga_WithEmptyErrorMessage_FailsValidation()
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Failed,
            ErrorMessage = string.Empty
        };

        Assert.False(state.IsValid());
    }

    /// <summary>
    /// Failed sagas with whitespace-only error message should fail validation.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool FailedSaga_WithWhitespaceOnlyErrorMessage_FailsValidation(PositiveInt spaceCount)
    {
        // Generate whitespace-only string with varying lengths
        var whitespaceOnly = new string(' ', Math.Min(spaceCount.Get, 100));

        var state = new TestSagaState
        {
            Status = SagaStatus.Failed,
            ErrorMessage = whitespaceOnly
        };

        return !state.IsValid();
    }

    /// <summary>
    /// Failed sagas with an error message should pass validation.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool FailedSaga_WithErrorMessage_PassesValidation(NonEmptyString errorMessage)
    {
        var state = new TestSagaState
        {
            Status = SagaStatus.Failed,
            ErrorMessage = errorMessage.Get
        };

        return state.IsValid();
    }

    #endregion

    #region SagaErrorCodes Invariants

    /// <summary>
    /// All error codes should start with "saga." prefix.
    /// Uses reflection (cached at class load) to automatically detect any new error codes.
    /// </summary>
    [Fact]
    public void SagaErrorCodes_AllCodesHaveSagaPrefix()
    {
        Assert.True(AllSagaErrorCodes.Length > 0, "SagaErrorCodes should have at least one error code constant");

        foreach (var code in AllSagaErrorCodes)
        {
            Assert.StartsWith("saga.", code);
        }
    }

    /// <summary>
    /// All error codes should be unique.
    /// Uses reflection (cached at class load) to automatically detect any new error codes.
    /// </summary>
    [Fact]
    public void SagaErrorCodes_AllCodesAreUnique()
    {
        Assert.True(AllSagaErrorCodes.Length > 0, "SagaErrorCodes should have at least one error code constant");
        Assert.Equal(AllSagaErrorCodes.Length, AllSagaErrorCodes.Distinct().Count());
    }

    #endregion

    #region SagaOptions Invariants

    /// <summary>
    /// SagaOptions.DefaultSagaTimeout should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaOptions_DefaultSagaTimeout_PreservesValue(PositiveInt minutes)
    {
        var timeout = TimeSpan.FromMinutes(Math.Min(minutes.Get, 1440)); // Max 24 hours
        var options = new SagaOptions
        {
            DefaultSagaTimeout = timeout
        };

        return options.DefaultSagaTimeout == timeout;
    }

    /// <summary>
    /// SagaOptions.StuckSagaThreshold should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaOptions_StuckSagaThreshold_PreservesValue(PositiveInt minutes)
    {
        var threshold = TimeSpan.FromMinutes(Math.Min(minutes.Get, 1440));
        var options = new SagaOptions
        {
            StuckSagaThreshold = threshold
        };

        return options.StuckSagaThreshold == threshold;
    }

    /// <summary>
    /// SagaOptions.StuckSagaCheckInterval should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaOptions_StuckSagaCheckInterval_PreservesValue(PositiveInt minutes)
    {
        var interval = TimeSpan.FromMinutes(Math.Min(minutes.Get, 1440));
        var options = new SagaOptions
        {
            StuckSagaCheckInterval = interval
        };

        return options.StuckSagaCheckInterval == interval;
    }

    /// <summary>
    /// SagaOptions.StuckSagaBatchSize should preserve value when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaOptions_StuckSagaBatchSize_PreservesValue(PositiveInt batchSize)
    {
        var options = new SagaOptions
        {
            StuckSagaBatchSize = batchSize.Get
        };

        return options.StuckSagaBatchSize == batchSize.Get;
    }

    #endregion

    #region Test Implementation

    private sealed class TestSagaState : ISagaState
    {
        private int _currentStep;

        public Guid SagaId { get; set; }
        public string SagaType { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Status { get; set; } = SagaStatus.Running;

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                _currentStep = value;
            }
        }

        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; }
        public DateTime? TimeoutAtUtc { get; set; }

        /// <summary>
        /// Validates saga state invariants.
        /// </summary>
        /// <returns>True if the state is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            // Failed sagas must have an error message
            if (Status == SagaStatus.Failed && string.IsNullOrWhiteSpace(ErrorMessage))
            {
                return false;
            }

            // CompletedAtUtc must be after StartedAtUtc when set
            if (CompletedAtUtc.HasValue && CompletedAtUtc < StartedAtUtc)
            {
                return false;
            }

            // LastUpdatedAtUtc must be at or after StartedAtUtc
            if (LastUpdatedAtUtc < StartedAtUtc)
            {
                return false;
            }

            return true;
        }
    }

    #endregion
}
