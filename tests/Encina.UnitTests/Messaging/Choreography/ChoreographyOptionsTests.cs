using Encina.Messaging.Choreography;
using Shouldly;

namespace Encina.UnitTests.Messaging.Choreography;

/// <summary>
/// Unit tests for <see cref="ChoreographyOptions"/>.
/// </summary>
public sealed class ChoreographyOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ChoreographyOptions();

        // Assert
        options.AutoCompensateOnFailure.ShouldBeTrue();
        options.SagaTimeout.ShouldBe(TimeSpan.FromMinutes(30));
        options.PersistState.ShouldBeTrue();
        options.MaxCompensationRetries.ShouldBe(3);
        options.CompensationRetryDelay.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CanSetAutoCompensateOnFailure()
    {
        // Arrange & Act
        var options = new ChoreographyOptions { AutoCompensateOnFailure = false };

        // Assert
        options.AutoCompensateOnFailure.ShouldBeFalse();
    }

    [Fact]
    public void CanSetSagaTimeout()
    {
        // Arrange & Act
        var options = new ChoreographyOptions { SagaTimeout = TimeSpan.FromHours(1) };

        // Assert
        options.SagaTimeout.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void CanSetPersistState()
    {
        // Arrange & Act
        var options = new ChoreographyOptions { PersistState = false };

        // Assert
        options.PersistState.ShouldBeFalse();
    }

    [Fact]
    public void CanSetMaxCompensationRetries()
    {
        // Arrange & Act
        var options = new ChoreographyOptions { MaxCompensationRetries = 5 };

        // Assert
        options.MaxCompensationRetries.ShouldBe(5);
    }

    [Fact]
    public void CanSetCompensationRetryDelay()
    {
        // Arrange & Act
        var options = new ChoreographyOptions { CompensationRetryDelay = TimeSpan.FromSeconds(10) };

        // Assert
        options.CompensationRetryDelay.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange & Act
        var options = new ChoreographyOptions
        {
            AutoCompensateOnFailure = false,
            SagaTimeout = TimeSpan.FromMinutes(60),
            PersistState = false,
            MaxCompensationRetries = 10,
            CompensationRetryDelay = TimeSpan.FromSeconds(1)
        };

        // Assert
        options.AutoCompensateOnFailure.ShouldBeFalse();
        options.SagaTimeout.ShouldBe(TimeSpan.FromMinutes(60));
        options.PersistState.ShouldBeFalse();
        options.MaxCompensationRetries.ShouldBe(10);
        options.CompensationRetryDelay.ShouldBe(TimeSpan.FromSeconds(1));
    }
}
