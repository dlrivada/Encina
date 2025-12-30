using Encina.Testing.Time;
using FluentAssertions;
using Xunit;

namespace Encina.Testing.Tests.Time;

/// <summary>
/// Unit tests for <see cref="FakeTimeProvider"/>.
/// </summary>
public sealed class FakeTimeProviderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_ShouldUseCurrentUtcTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var provider = new FakeTimeProvider();

        // Assert
        var after = DateTimeOffset.UtcNow;
        provider.GetUtcNow().Should().BeOnOrAfter(before);
        provider.GetUtcNow().Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_WithStartTime_ShouldUseProvidedTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var provider = new FakeTimeProvider(startTime);

        // Assert
        provider.GetUtcNow().Should().Be(startTime);
    }

    [Fact]
    public void Constructor_WithTimeZone_ShouldUseProvidedTimeZone()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        // Act
        var provider = new FakeTimeProvider(startTime, timeZone);

        // Assert
        provider.LocalTimeZone.Should().Be(timeZone);
    }

    [Fact]
    public void LocalTimeZone_Default_ShouldBeUtc()
    {
        // Arrange & Act
        var provider = new FakeTimeProvider();

        // Assert
        provider.LocalTimeZone.Should().Be(TimeZoneInfo.Utc);
    }

    #endregion

    #region SetUtcNow Tests

    [Fact]
    public void SetUtcNow_ValidFutureTime_ShouldUpdateTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var newTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.SetUtcNow(newTime);

        // Assert
        provider.GetUtcNow().Should().Be(newTime);
    }

    [Fact]
    public void SetUtcNow_SameTime_ShouldSucceed()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.SetUtcNow(startTime);

        // Assert
        provider.GetUtcNow().Should().Be(startTime);
    }

    [Fact]
    public void SetUtcNow_PastTime_ShouldThrowArgumentException()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var pastTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        var act = () => provider.SetUtcNow(pastTime);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("utcNow")
            .WithMessage("Cannot move time backwards*");
    }

    #endregion

    #region Advance Tests

    [Fact]
    public void Advance_PositiveDuration_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);
        var duration = TimeSpan.FromHours(5);

        // Act
        provider.Advance(duration);

        // Assert
        provider.GetUtcNow().Should().Be(startTime.Add(duration));
    }

    [Fact]
    public void Advance_ZeroDuration_ShouldNotChangeTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.Advance(TimeSpan.Zero);

        // Assert
        provider.GetUtcNow().Should().Be(startTime);
    }

    [Fact]
    public void Advance_NegativeDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var negativeDuration = TimeSpan.FromHours(-1);

        // Act
        var act = () => provider.Advance(negativeDuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("duration")
            .WithMessage("Duration must be non-negative*");
    }

    [Fact]
    public void Advance_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.Advance(TimeSpan.FromHours(1));
        provider.Advance(TimeSpan.FromMinutes(30));
        provider.Advance(TimeSpan.FromSeconds(15));

        // Assert
        var expected = startTime
            .AddHours(1)
            .AddMinutes(30)
            .AddSeconds(15);
        provider.GetUtcNow().Should().Be(expected);
    }

    #endregion

    #region Convenience Methods Tests

    [Fact]
    public void AdvanceToNextDay_ShouldAdvance24Hours()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 10, 30, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.AdvanceToNextDay();

        // Assert
        provider.GetUtcNow().Should().Be(startTime.AddDays(1));
    }

    [Fact]
    public void AdvanceToNextHour_ShouldAdvance1Hour()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 10, 30, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.AdvanceToNextHour();

        // Assert
        provider.GetUtcNow().Should().Be(startTime.AddHours(1));
    }

    [Fact]
    public void AdvanceMinutes_ShouldAdvanceBySpecifiedMinutes()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.AdvanceMinutes(45);

        // Assert
        provider.GetUtcNow().Should().Be(startTime.AddMinutes(45));
    }

    [Fact]
    public void AdvanceSeconds_ShouldAdvanceBySpecifiedSeconds()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.AdvanceSeconds(90);

        // Assert
        provider.GetUtcNow().Should().Be(startTime.AddSeconds(90));
    }

    [Fact]
    public void AdvanceMilliseconds_ShouldAdvanceBySpecifiedMilliseconds()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        provider.AdvanceMilliseconds(500);

        // Assert
        provider.GetUtcNow().Should().Be(startTime.AddMilliseconds(500));
    }

    #endregion

    #region Freeze Tests

    [Fact]
    public void Freeze_ShouldRestoreTimeOnDispose()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act
        using (provider.Freeze())
        {
            provider.Advance(TimeSpan.FromHours(5));
            provider.GetUtcNow().Should().Be(startTime.AddHours(5));
        }

        // Assert
        provider.GetUtcNow().Should().Be(startTime);
    }

    [Fact]
    public void Freeze_NestedScopes_ShouldRestoreCorrectly()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);

        // Act & Assert
        using (provider.Freeze())
        {
            provider.Advance(TimeSpan.FromHours(2));
            var afterFirstAdvance = provider.GetUtcNow();
            afterFirstAdvance.Should().Be(startTime.AddHours(2));

            using (provider.Freeze())
            {
                provider.Advance(TimeSpan.FromHours(3));
                provider.GetUtcNow().Should().Be(startTime.AddHours(5));
            }

            // Inner scope restores to when it was created
            provider.GetUtcNow().Should().Be(afterFirstAdvance);
        }

        // Outer scope restores to start
        provider.GetUtcNow().Should().Be(startTime);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_WithTime_ShouldSetNewTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var newTime = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var provider = new FakeTimeProvider(startTime);
        provider.Advance(TimeSpan.FromDays(100));

        // Act
        provider.Reset(newTime);

        // Assert
        provider.GetUtcNow().Should().Be(newTime);
    }

    [Fact]
    public void Reset_Default_ShouldUseCurrentUtcTime()
    {
        // Arrange
        var oldTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var provider = new FakeTimeProvider(oldTime);
        var before = DateTimeOffset.UtcNow;

        // Act
        provider.Reset();

        // Assert
        var after = DateTimeOffset.UtcNow;
        provider.GetUtcNow().Should().BeOnOrAfter(before);
        provider.GetUtcNow().Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Reset_ShouldClearTimers()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
        provider.ActiveTimerCount.Should().Be(1);

        // Act
        provider.Reset(DateTimeOffset.UtcNow);

        // Assert
        provider.ActiveTimerCount.Should().Be(0);
    }

    #endregion

    #region Timer Tests

    [Fact]
    public void CreateTimer_ShouldReturnActiveTimer()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;

        // Act
        var timer = provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(10), TimeSpan.Zero);

        // Assert
        timer.Should().NotBeNull();
        provider.ActiveTimerCount.Should().Be(1);
    }

    [Fact]
    public void CreateTimer_NullCallback_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new FakeTimeProvider();

        // Act
        var act = () => provider.CreateTimer(null!, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("callback");
    }

    [Fact]
    public void Timer_ShouldFireWhenDue()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(5), TimeSpan.Zero);

        // Act - advance past due time
        provider.Advance(TimeSpan.FromSeconds(6));

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void Timer_ShouldNotFireBeforeDue()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(10), TimeSpan.Zero);

        // Act - advance but not past due time
        provider.Advance(TimeSpan.FromSeconds(5));

        // Assert
        callCount.Should().Be(0);
    }

    [Fact]
    public void Timer_Periodic_ShouldFireMultipleTimes()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        // Act - advance to trigger multiple firings
        provider.Advance(TimeSpan.FromSeconds(17));

        // Assert - should fire at 5s, 10s, 15s = 3 times
        callCount.Should().Be(3);
    }

    [Fact]
    public void Timer_OneShot_ShouldFireOnlyOnce()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(5), TimeSpan.Zero);

        // Act - advance way past due time
        provider.Advance(TimeSpan.FromSeconds(100));

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void Timer_Change_ShouldUpdateDueTime()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        var timer = provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(100), TimeSpan.Zero);

        // Act - change to fire sooner
        timer.Change(TimeSpan.FromSeconds(1), TimeSpan.Zero);
        provider.Advance(TimeSpan.FromSeconds(2));

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void Timer_ChangeToInfinite_ShouldDisable()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        var timer = provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);

        // Act - disable the timer
        timer.Change(Timeout.InfiniteTimeSpan, TimeSpan.Zero);
        provider.Advance(TimeSpan.FromSeconds(100));

        // Assert
        callCount.Should().Be(0);
    }

    [Fact]
    public void Timer_Dispose_ShouldPreventFiring()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        var timer = provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(5), TimeSpan.Zero);

        // Act
        timer.Dispose();
        provider.Advance(TimeSpan.FromSeconds(10));

        // Assert
        callCount.Should().Be(0);
        provider.ActiveTimerCount.Should().Be(0);
    }

    [Fact]
    public async Task Timer_DisposeAsync_ShouldPreventFiring()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        var timer = provider.CreateTimer(_ => callCount++, null, TimeSpan.FromSeconds(5), TimeSpan.Zero);

        // Act
        await timer.DisposeAsync();
        provider.Advance(TimeSpan.FromSeconds(10));

        // Assert
        callCount.Should().Be(0);
    }

    [Fact]
    public void Timer_WithState_ShouldPassStateToCallback()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        object? receivedState = null;
        var expectedState = new { Value = 42 };

        provider.CreateTimer(
            state => receivedState = state,
            expectedState,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero);

        // Act
        provider.Advance(TimeSpan.FromSeconds(2));

        // Assert
        receivedState.Should().BeSameAs(expectedState);
    }

    [Fact]
    public void Timer_InfiniteInitialDelay_ShouldNotFireUntilChanged()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var callCount = 0;
        var timer = provider.CreateTimer(_ => callCount++, null, Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1));

        // Act
        provider.Advance(TimeSpan.FromDays(1));

        // Assert
        callCount.Should().Be(0);

        // Now enable the timer - it will fire at 1s (initial), then at 2s and 3s (periodic)
        timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        provider.Advance(TimeSpan.FromSeconds(3));

        callCount.Should().Be(3); // Fires at 1s, 2s, and 3s
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void ConcurrentAdvance_ShouldBeThreadSafe()
    {
        // Arrange
        var provider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tasks = new Task[100];

        // Act
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() => provider.Advance(TimeSpan.FromMilliseconds(1)));
        }

        Task.WaitAll(tasks);

        // Assert - all advances should have been applied
        provider.GetUtcNow().Should().Be(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(100));
    }

    [Fact]
    public void ConcurrentTimerCreation_ShouldBeThreadSafe()
    {
        // Arrange
        var provider = new FakeTimeProvider();
        var tasks = new Task[50];
        var callCount = 0;

        // Act
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                provider.CreateTimer(_ => Interlocked.Increment(ref callCount), null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            });
        }

        Task.WaitAll(tasks);
        provider.Advance(TimeSpan.FromSeconds(2));

        // Assert
        callCount.Should().Be(50);
    }

    #endregion
}
