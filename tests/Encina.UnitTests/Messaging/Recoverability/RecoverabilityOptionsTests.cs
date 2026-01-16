using Encina.Messaging.Recoverability;
using Shouldly;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Unit tests for <see cref="RecoverabilityOptions"/>.
/// </summary>
public sealed class RecoverabilityOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new RecoverabilityOptions();

        // Assert
        options.ImmediateRetries.ShouldBe(3);
        options.ImmediateRetryDelay.ShouldBe(TimeSpan.FromMilliseconds(100));
        options.UseExponentialBackoffForImmediateRetries.ShouldBeTrue();
        options.DelayedRetries.Length.ShouldBe(4);
        options.DelayedRetries[0].ShouldBe(TimeSpan.FromSeconds(30));
        options.DelayedRetries[1].ShouldBe(TimeSpan.FromMinutes(5));
        options.DelayedRetries[2].ShouldBe(TimeSpan.FromMinutes(30));
        options.DelayedRetries[3].ShouldBe(TimeSpan.FromHours(2));
        options.OnPermanentFailure.ShouldBeNull();
        options.ErrorClassifier.ShouldBeNull();
        options.EnableDelayedRetries.ShouldBeTrue();
        options.UseJitter.ShouldBeTrue();
        options.MaxJitterPercent.ShouldBe(20);
    }

    [Fact]
    public void TotalRetryAttempts_WithDelayedRetries_ReturnsCorrectCount()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            DelayedRetries = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)],
            EnableDelayedRetries = true
        };

        // Act & Assert
        options.TotalRetryAttempts.ShouldBe(5); // 3 immediate + 2 delayed
    }

    [Fact]
    public void TotalRetryAttempts_WithoutDelayedRetries_ReturnsOnlyImmediate()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 5,
            DelayedRetries = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)],
            EnableDelayedRetries = false
        };

        // Act & Assert
        options.TotalRetryAttempts.ShouldBe(5); // Only immediate
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange
        var callback = new Func<FailedMessage, CancellationToken, Task>((_, _) => Task.CompletedTask);
        var classifier = new DefaultErrorClassifier();

        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 5,
            ImmediateRetryDelay = TimeSpan.FromSeconds(1),
            UseExponentialBackoffForImmediateRetries = false,
            DelayedRetries = [TimeSpan.FromMinutes(1)],
            OnPermanentFailure = callback,
            ErrorClassifier = classifier,
            EnableDelayedRetries = false,
            UseJitter = false,
            MaxJitterPercent = 50
        };

        // Assert
        options.ImmediateRetries.ShouldBe(5);
        options.ImmediateRetryDelay.ShouldBe(TimeSpan.FromSeconds(1));
        options.UseExponentialBackoffForImmediateRetries.ShouldBeFalse();
        options.DelayedRetries.Length.ShouldBe(1);
        options.OnPermanentFailure.ShouldBe(callback);
        options.ErrorClassifier.ShouldBe(classifier);
        options.EnableDelayedRetries.ShouldBeFalse();
        options.UseJitter.ShouldBeFalse();
        options.MaxJitterPercent.ShouldBe(50);
    }
}
