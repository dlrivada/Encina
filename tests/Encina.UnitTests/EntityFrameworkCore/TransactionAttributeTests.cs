using Encina.Testing;
using Encina.EntityFrameworkCore;
using System.Data;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Unit tests for <see cref="TransactionAttribute"/>.
/// </summary>
public sealed class TransactionAttributeTests
{
    [Fact]
    public void DefaultConstructor_IsolationLevelIsNull()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute();

        // Assert
        attribute.IsolationLevel.ShouldBeNull();
    }

    [Fact]
    public void IsolationLevel_CanBeSetToReadCommitted()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute
        {
            IsolationLevel = IsolationLevel.ReadCommitted
        };

        // Assert
        attribute.IsolationLevel.ShouldBe(IsolationLevel.ReadCommitted);
    }

    [Fact]
    public void IsolationLevel_CanBeSetToReadUncommitted()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute
        {
            IsolationLevel = IsolationLevel.ReadUncommitted
        };

        // Assert
        attribute.IsolationLevel.ShouldBe(IsolationLevel.ReadUncommitted);
    }

    [Fact]
    public void IsolationLevel_CanBeSetToRepeatableRead()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute
        {
            IsolationLevel = IsolationLevel.RepeatableRead
        };

        // Assert
        attribute.IsolationLevel.ShouldBe(IsolationLevel.RepeatableRead);
    }

    [Fact]
    public void IsolationLevel_CanBeSetToSerializable()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute
        {
            IsolationLevel = IsolationLevel.Serializable
        };

        // Assert
        attribute.IsolationLevel.ShouldBe(IsolationLevel.Serializable);
    }

    [Fact]
    public void IsolationLevel_CanBeSetToSnapshot()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute
        {
            IsolationLevel = IsolationLevel.Snapshot
        };

        // Assert
        attribute.IsolationLevel.ShouldBe(IsolationLevel.Snapshot);
    }

    [Fact]
    public void AttributeUsage_TargetsClass()
    {
        // Arrange
        var attributeUsage = typeof(TransactionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void AttributeUsage_IsInherited()
    {
        // Arrange
        var attributeUsage = typeof(TransactionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void AttributeUsage_AllowsOnlyOnePerClass()
    {
        // Arrange
        var attributeUsage = typeof(TransactionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void CanApplyToCommand()
    {
        // Arrange
        var type = typeof(TestTransactionalCommand);

        // Act
        var attribute = type
            .GetCustomAttributes(typeof(TransactionAttribute), false)
            .Cast<TransactionAttribute>()
            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
    }

    [Transaction]
    private sealed class TestTransactionalCommand;
}
