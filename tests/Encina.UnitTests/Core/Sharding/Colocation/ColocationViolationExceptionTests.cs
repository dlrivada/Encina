using Encina.Sharding.Colocation;

namespace Encina.UnitTests.Core.Sharding.Colocation;

/// <summary>
/// Unit tests for <see cref="ColocationViolationException"/>.
/// </summary>
public sealed class ColocationViolationExceptionTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AllParameters_StoresCorrectly()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(
            typeof(Order),
            typeof(OrderItem),
            "Key mismatch",
            "String",
            "Int32");

        // Assert
        ex.RootEntityType.ShouldBe(typeof(Order));
        ex.FailedEntityType.ShouldBe(typeof(OrderItem));
        ex.Reason.ShouldBe("Key mismatch");
        ex.ExpectedShardKeyType.ShouldBe("String");
        ex.ActualShardKeyType.ShouldBe("Int32");
    }

    [Fact]
    public void Constructor_NullReason_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => new ColocationViolationException(typeof(Order), typeof(OrderItem), null!));
    }

    [Fact]
    public void Constructor_NullRootEntityType_StoresNull()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(null, typeof(OrderItem), "Test reason");

        // Assert
        ex.RootEntityType.ShouldBeNull();
    }

    [Fact]
    public void Constructor_NullFailedEntityType_StoresNull()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(typeof(Order), null, "Test reason");

        // Assert
        ex.FailedEntityType.ShouldBeNull();
    }

    [Fact]
    public void Constructor_OptionalParameters_DefaultToNull()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(typeof(Order), typeof(OrderItem), "Test reason");

        // Assert
        ex.ExpectedShardKeyType.ShouldBeNull();
        ex.ActualShardKeyType.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  Message
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Message_ContainsReason()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(typeof(Order), typeof(OrderItem), "Key mismatch");

        // Assert
        ex.Message.ShouldContain("Key mismatch");
    }

    [Fact]
    public void Message_ContainsRootEntityName()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(typeof(Order), typeof(OrderItem), "Test");

        // Assert
        ex.Message.ShouldContain("Order");
    }

    [Fact]
    public void Message_ContainsFailedEntityName()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(typeof(Order), typeof(OrderItem), "Test");

        // Assert
        ex.Message.ShouldContain("OrderItem");
    }

    [Fact]
    public void Message_WithShardKeyTypes_ContainsTypeInfo()
    {
        // Arrange & Act
        var ex = new ColocationViolationException(
            typeof(Order), typeof(OrderItem), "Test", "String", "Int32");

        // Assert
        ex.Message.ShouldContain("String");
        ex.Message.ShouldContain("Int32");
    }

    // ────────────────────────────────────────────────────────────
    //  ErrorCode
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ErrorCode_HasExpectedValue()
    {
        ColocationViolationException.ErrorCode.ShouldBe("Encina.ColocationViolation");
    }

    // ────────────────────────────────────────────────────────────
    //  ToEncinaError
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ToEncinaError_ReturnsErrorWithCorrectCode()
    {
        // Arrange
        var ex = new ColocationViolationException(typeof(Order), typeof(OrderItem), "Test reason");

        // Act
        var error = ex.ToEncinaError();

        // Assert
        error.GetCode().IfNone(string.Empty).ShouldBe("Encina.ColocationViolation");
    }

    [Fact]
    public void ToEncinaError_MessageContainsReason()
    {
        // Arrange
        var ex = new ColocationViolationException(typeof(Order), typeof(OrderItem), "Test reason");

        // Act
        var error = ex.ToEncinaError();

        // Assert
        error.Message.ShouldContain("Test reason");
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Order;
    private sealed class OrderItem;
}
