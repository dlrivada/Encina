using Encina.Messaging.DeadLetter;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class DeadLetterOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new DeadLetterOptions();

        // Assert
        options.RetentionPeriod.ShouldBe(TimeSpan.FromDays(7));
        options.CleanupInterval.ShouldBe(TimeSpan.FromHours(1));
        options.EnableAutomaticCleanup.ShouldBeTrue();
        options.IntegrateWithRecoverability.ShouldBeTrue();
        options.IntegrateWithOutbox.ShouldBeTrue();
        options.IntegrateWithInbox.ShouldBeTrue();
        options.IntegrateWithScheduling.ShouldBeTrue();
        options.IntegrateWithSagas.ShouldBeTrue();
        options.OnDeadLetter.ShouldBeNull();
    }

    [Fact]
    public void RetentionPeriod_CanBeSetToNull()
    {
        // Arrange
        var options = new DeadLetterOptions();

        // Act
        options.RetentionPeriod = null;

        // Assert
        options.RetentionPeriod.ShouldBeNull();
    }

    [Fact]
    public void OnDeadLetter_CanBeConfigured()
    {
        // Arrange
        var options = new DeadLetterOptions();

        // Act
        options.OnDeadLetter = (_, _) => Task.CompletedTask;

        // Assert
        options.OnDeadLetter.ShouldNotBeNull();
    }

    [Fact]
    public void IntegrationFlags_CanBeDisabled()
    {
        // Arrange & Act
        var options = new DeadLetterOptions
        {
            IntegrateWithRecoverability = false,
            IntegrateWithOutbox = false,
            IntegrateWithInbox = false,
            IntegrateWithScheduling = false,
            IntegrateWithSagas = false
        };

        // Assert
        options.IntegrateWithRecoverability.ShouldBeFalse();
        options.IntegrateWithOutbox.ShouldBeFalse();
        options.IntegrateWithInbox.ShouldBeFalse();
        options.IntegrateWithScheduling.ShouldBeFalse();
        options.IntegrateWithSagas.ShouldBeFalse();
    }
}
