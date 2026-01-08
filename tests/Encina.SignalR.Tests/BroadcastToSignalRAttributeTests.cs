using Encina.SignalR;
using Xunit;

namespace Encina.SignalR.Tests;

/// <summary>
/// Tests for the <see cref="BroadcastToSignalRAttribute"/> class.
/// </summary>
public sealed class BroadcastToSignalRAttributeTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var attribute = new BroadcastToSignalRAttribute();

        // Assert
        attribute.Method.ShouldBeNull();
        attribute.TargetUsers.ShouldBeNull();
        attribute.TargetGroups.ShouldBeNull();
        attribute.ExcludeCaller.ShouldBeFalse();
        attribute.ConditionalProperty.ShouldBeNull();
    }

    [Fact]
    public void Method_CanBeSet()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute();

        // Act
        attribute.Method = "OrderCreated";

        // Assert
        attribute.Method.ShouldBe("OrderCreated");
    }

    [Fact]
    public void TargetUsers_CanBeSet()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute();

        // Act
        attribute.TargetUsers = "{UserId}";

        // Assert
        attribute.TargetUsers.ShouldBe("{UserId}");
    }

    [Fact]
    public void TargetGroups_CanBeSet()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute();

        // Act
        attribute.TargetGroups = "Administrators,Managers";

        // Assert
        attribute.TargetGroups.ShouldBe("Administrators,Managers");
    }

    [Fact]
    public void ExcludeCaller_CanBeSet()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute();

        // Act
        attribute.ExcludeCaller = true;

        // Assert
        attribute.ExcludeCaller.ShouldBeTrue();
    }

    [Fact]
    public void ConditionalProperty_CanBeSet()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute();

        // Act
        attribute.ConditionalProperty = "ShouldBroadcast";

        // Assert
        attribute.ConditionalProperty.ShouldBe("ShouldBroadcast");
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var type = typeof(TestBroadcastNotification);
        var attribute = type.GetCustomAttributes(typeof(BroadcastToSignalRAttribute), false)
            .FirstOrDefault() as BroadcastToSignalRAttribute;

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Method.ShouldBe("TestMethod");
    }

    [Fact]
    public void Attribute_UsageSettings_AreCorrect()
    {
        // Arrange
        var attributeUsage = typeof(BroadcastToSignalRAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void Attribute_CanHaveMultipleTargetGroups()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute
        {
            TargetGroups = "Group1, Group2, Group3"
        };

        // Assert
        attribute.TargetGroups.ShouldContain("Group1");
        attribute.TargetGroups.ShouldContain("Group2");
        attribute.TargetGroups.ShouldContain("Group3");
    }

    [Fact]
    public void Attribute_CanHavePlaceholderInTargetUsers()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute
        {
            TargetUsers = "{CustomerId},{OwnerId}"
        };

        // Assert
        attribute.TargetUsers.ShouldContain("{CustomerId}");
        attribute.TargetUsers.ShouldContain("{OwnerId}");
    }

    // Test notification type for attribute tests
    [BroadcastToSignalR(Method = "TestMethod")]
    private sealed record TestBroadcastNotification : INotification;
}
