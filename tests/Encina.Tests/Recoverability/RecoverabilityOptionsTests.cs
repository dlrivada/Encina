using Encina.Messaging.Recoverability;
using Shouldly;

namespace Encina.Tests.Recoverability;

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
        options.EnableDelayedRetries.ShouldBeTrue();
        options.UseJitter.ShouldBeTrue();
        options.MaxJitterPercent.ShouldBe(20);
        options.ErrorClassifier.ShouldBeNull();
        options.OnPermanentFailure.ShouldBeNull();
    }

    [Fact]
    public void DelayedRetries_HasDefaultValues()
    {
        // Arrange & Act
        var options = new RecoverabilityOptions();

        // Assert
        options.DelayedRetries.ShouldNotBeEmpty();
        options.DelayedRetries.Length.ShouldBe(4);
        options.DelayedRetries[0].ShouldBe(TimeSpan.FromSeconds(30));
        options.DelayedRetries[1].ShouldBe(TimeSpan.FromMinutes(5));
        options.DelayedRetries[2].ShouldBe(TimeSpan.FromMinutes(30));
        options.DelayedRetries[3].ShouldBe(TimeSpan.FromHours(2));
    }

    [Fact]
    public void TotalRetryAttempts_CalculatesCorrectly_WhenDelayedRetriesEnabled()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 5,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)]
        };

        // Act & Assert
        options.TotalRetryAttempts.ShouldBe(7); // 5 immediate + 2 delayed
    }

    [Fact]
    public void TotalRetryAttempts_CalculatesCorrectly_WhenDelayedRetriesDisabled()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 5,
            EnableDelayedRetries = false,
            DelayedRetries = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)]
        };

        // Act & Assert
        options.TotalRetryAttempts.ShouldBe(5); // Only immediate retries
    }

    [Fact]
    public void CustomErrorClassifier_CanBeSet()
    {
        // Arrange
        var customClassifier = new DefaultErrorClassifier();
        var options = new RecoverabilityOptions
        {
            ErrorClassifier = customClassifier
        };

        // Assert
        options.ErrorClassifier.ShouldBe(customClassifier);
    }

    [Fact]
    public void OnPermanentFailure_CanBeSet()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            OnPermanentFailure = (_, _) => Task.CompletedTask
        };

        // Assert
        options.OnPermanentFailure.ShouldNotBeNull();
    }
}
