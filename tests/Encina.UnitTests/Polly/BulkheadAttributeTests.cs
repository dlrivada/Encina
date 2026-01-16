using Encina.Testing;
using Encina.Polly;
namespace Encina.UnitTests.Polly;

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
        attribute.MaxConcurrency.ShouldBe(10);
        attribute.MaxQueuedActions.ShouldBe(20);
        attribute.QueueTimeoutMs.ShouldBe(30000);
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
        attribute.MaxConcurrency.ShouldBe(5);
        attribute.MaxQueuedActions.ShouldBe(10);
        attribute.QueueTimeoutMs.ShouldBe(5000);
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
        attribute.MaxQueuedActions.ShouldBe(0);
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
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeTrue();
    }
}
