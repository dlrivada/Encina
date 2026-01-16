using Encina.Messaging.Scheduling;
using Shouldly;

namespace Encina.UnitTests.Messaging.Scheduling;

/// <summary>
/// Unit tests for <see cref="SchedulingOptions"/>.
/// </summary>
public sealed class SchedulingOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SchedulingOptions();

        // Assert
        options.ProcessingInterval.ShouldBe(TimeSpan.FromSeconds(30));
        options.BatchSize.ShouldBe(100);
        options.MaxRetries.ShouldBe(3);
        options.BaseRetryDelay.ShouldBe(TimeSpan.FromSeconds(5));
        options.EnableProcessor.ShouldBeTrue();
        options.EnableRecurringMessages.ShouldBeTrue();
    }

    [Fact]
    public void CanSetProcessingInterval()
    {
        // Arrange & Act
        var options = new SchedulingOptions { ProcessingInterval = TimeSpan.FromMinutes(1) };

        // Assert
        options.ProcessingInterval.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CanSetBatchSize()
    {
        // Arrange & Act
        var options = new SchedulingOptions { BatchSize = 50 };

        // Assert
        options.BatchSize.ShouldBe(50);
    }

    [Fact]
    public void CanSetMaxRetries()
    {
        // Arrange & Act
        var options = new SchedulingOptions { MaxRetries = 5 };

        // Assert
        options.MaxRetries.ShouldBe(5);
    }

    [Fact]
    public void CanSetBaseRetryDelay()
    {
        // Arrange & Act
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(10) };

        // Assert
        options.BaseRetryDelay.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CanSetEnableProcessor()
    {
        // Arrange & Act
        var options = new SchedulingOptions { EnableProcessor = false };

        // Assert
        options.EnableProcessor.ShouldBeFalse();
    }

    [Fact]
    public void CanSetEnableRecurringMessages()
    {
        // Arrange & Act
        var options = new SchedulingOptions { EnableRecurringMessages = false };

        // Assert
        options.EnableRecurringMessages.ShouldBeFalse();
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange & Act
        var options = new SchedulingOptions
        {
            ProcessingInterval = TimeSpan.FromSeconds(15),
            BatchSize = 200,
            MaxRetries = 10,
            BaseRetryDelay = TimeSpan.FromSeconds(2),
            EnableProcessor = false,
            EnableRecurringMessages = false
        };

        // Assert
        options.ProcessingInterval.ShouldBe(TimeSpan.FromSeconds(15));
        options.BatchSize.ShouldBe(200);
        options.MaxRetries.ShouldBe(10);
        options.BaseRetryDelay.ShouldBe(TimeSpan.FromSeconds(2));
        options.EnableProcessor.ShouldBeFalse();
        options.EnableRecurringMessages.ShouldBeFalse();
    }
}
