namespace Encina.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="BulkheadAttribute"/>.
/// Tests default values and property initialization.
/// </summary>
public class BulkheadAttributeTests
{
    [Fact]
    public void BulkheadAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new BulkheadAttribute();

        // Assert
        attribute.MaxConcurrency.Should().Be(10);
        attribute.MaxQueuedActions.Should().Be(20);
        attribute.QueueTimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void BulkheadAttribute_CustomValues_ShouldBeSet()
    {
        // Arrange & Act
        var attribute = new BulkheadAttribute
        {
            MaxConcurrency = 5,
            MaxQueuedActions = 10,
            QueueTimeoutMs = 5000
        };

        // Assert
        attribute.MaxConcurrency.Should().Be(5);
        attribute.MaxQueuedActions.Should().Be(10);
        attribute.QueueTimeoutMs.Should().Be(5000);
    }

    [Fact]
    public void BulkheadAttribute_NoQueue_ShouldBeConfigurable()
    {
        // Arrange & Act
        var attribute = new BulkheadAttribute
        {
            MaxConcurrency = 10,
            MaxQueuedActions = 0 // Disable queueing
        };

        // Assert
        attribute.MaxQueuedActions.Should().Be(0);
    }

    [Fact]
    public void BulkheadAttribute_ShouldHaveCorrectUsage()
    {
        // Arrange
        var attributeUsage = typeof(BulkheadAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Class);
        attributeUsage.AllowMultiple.Should().BeFalse();
        attributeUsage.Inherited.Should().BeTrue();
    }
}
