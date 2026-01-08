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
        // Arrange - no setup required

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
        var attribute = (BroadcastToSignalRAttribute)type
            .GetCustomAttributes(typeof(BroadcastToSignalRAttribute), false)
            .Single();

        // Assert
        attribute.Method.ShouldBe("TestMethod");
    }

    [Fact]
    public void Attribute_UsageSettings_AreCorrect()
    {
        // Arrange
        var attributeUsage = (AttributeUsageAttribute)typeof(BroadcastToSignalRAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Single();

        // Assert
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void GetTargetGroupsList_ParsesCommaSeparatedValues_WithTrimming()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute
        {
            TargetGroups = "Group1, Group2, Group3"
        };

        // Act
        var groups = attribute.GetTargetGroupsList();

        // Assert
        groups.ShouldBe(["Group1", "Group2", "Group3"]);
    }

    [Fact]
    public void GetTargetGroupsList_ReturnsEmptyArray_WhenTargetGroupsIsNull()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute();

        // Act
        var groups = attribute.GetTargetGroupsList();

        // Assert
        groups.ShouldBeEmpty();
    }

    [Fact]
    public void GetTargetUsersList_ParsesCommaSeparatedPlaceholders_WithTrimming()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute
        {
            TargetUsers = "{CustomerId}, {OwnerId}"
        };

        // Act
        var users = attribute.GetTargetUsersList();

        // Assert
        users.ShouldBe(["{CustomerId}", "{OwnerId}"]);
    }

    [Fact]
    public void GetTargetUsersList_ReturnsEmptyArray_WhenTargetUsersIsNull()
    {
        // Arrange
        var attribute = new BroadcastToSignalRAttribute();

        // Act
        var users = attribute.GetTargetUsersList();

        // Assert
        users.ShouldBeEmpty();
    }

    // Test notification type for attribute tests
    [BroadcastToSignalR(Method = "TestMethod")]
    private sealed record TestBroadcastNotification : INotification;
}
