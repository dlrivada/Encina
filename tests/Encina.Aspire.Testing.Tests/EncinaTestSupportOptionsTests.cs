using Encina.Aspire.Testing;
using Shouldly;

namespace Encina.Aspire.Testing.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaTestSupportOptions"/>.
/// </summary>
public sealed class EncinaTestSupportOptionsTests
{
    private static EncinaTestSupportOptions CreateOptions() => new();

    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var options = CreateOptions();

        // Assert
        options.ClearOutboxBeforeTest.ShouldBeTrue();
        options.ClearInboxBeforeTest.ShouldBeTrue();
        options.ResetSagasBeforeTest.ShouldBeTrue();
        options.ClearScheduledMessagesBeforeTest.ShouldBeTrue();
        options.ClearDeadLetterBeforeTest.ShouldBeTrue();
        options.DefaultWaitTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.PollingInterval.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Theory]
    [InlineData(nameof(EncinaTestSupportOptions.ClearOutboxBeforeTest))]
    [InlineData(nameof(EncinaTestSupportOptions.ClearInboxBeforeTest))]
    [InlineData(nameof(EncinaTestSupportOptions.ResetSagasBeforeTest))]
    [InlineData(nameof(EncinaTestSupportOptions.ClearScheduledMessagesBeforeTest))]
    [InlineData(nameof(EncinaTestSupportOptions.ClearDeadLetterBeforeTest))]
    public void BooleanProperty_CanBeSetToFalse(string propertyName)
    {
        // Arrange
        var options = CreateOptions();
        var property = typeof(EncinaTestSupportOptions).GetProperty(propertyName)!;

        // Act
        property.SetValue(options, false);

        // Assert
        property.GetValue(options).ShouldBe(false);
    }

    [Fact]
    public void DefaultWaitTimeout_CanBeSet()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        options.DefaultWaitTimeout = TimeSpan.FromMinutes(5);

        // Assert
        options.DefaultWaitTimeout.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void PollingInterval_CanBeSet()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        options.PollingInterval = TimeSpan.FromMilliseconds(500);

        // Assert
        options.PollingInterval.ShouldBe(TimeSpan.FromMilliseconds(500));
    }
}
