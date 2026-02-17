using Encina.Sharding.Colocation;

namespace Encina.UnitTests.Core.Sharding.Colocation;

/// <summary>
/// Unit tests for <see cref="ColocationGroupBuilder"/>.
/// </summary>
public sealed class ColocationGroupBuilderTests
{
    // ────────────────────────────────────────────────────────────
    //  WithRootEntity
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void WithRootEntity_SetsRootEntityType()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .Build();

        // Assert
        group.RootEntity.ShouldBe(typeof(Order));
    }

    [Fact]
    public void WithRootEntity_ReturnsSameBuilder_ForChaining()
    {
        // Arrange
        var builder = new ColocationGroupBuilder();

        // Act
        var result = builder.WithRootEntity<Order>();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    // ────────────────────────────────────────────────────────────
    //  AddColocatedEntity
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddColocatedEntity_SingleEntity_AddsToGroup()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .AddColocatedEntity<OrderItem>()
            .Build();

        // Assert
        group.ColocatedEntities.ShouldContain(typeof(OrderItem));
        group.ColocatedEntities.Count.ShouldBe(1);
    }

    [Fact]
    public void AddColocatedEntity_MultipleEntities_AllAdded()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .AddColocatedEntity<OrderItem>()
            .AddColocatedEntity<OrderPayment>()
            .Build();

        // Assert
        group.ColocatedEntities.Count.ShouldBe(2);
        group.ColocatedEntities.ShouldContain(typeof(OrderItem));
        group.ColocatedEntities.ShouldContain(typeof(OrderPayment));
    }

    [Fact]
    public void AddColocatedEntity_DuplicateEntity_NotAddedTwice()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .AddColocatedEntity<OrderItem>()
            .AddColocatedEntity<OrderItem>()
            .Build();

        // Assert
        group.ColocatedEntities.Count.ShouldBe(1);
    }

    [Fact]
    public void AddColocatedEntity_ReturnsSameBuilder_ForChaining()
    {
        // Arrange
        var builder = new ColocationGroupBuilder()
            .WithRootEntity<Order>();

        // Act
        var result = builder.AddColocatedEntity<OrderItem>();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    // ────────────────────────────────────────────────────────────
    //  WithSharedShardKeyProperty
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void WithSharedShardKeyProperty_SetsPropertyName()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .WithSharedShardKeyProperty("CustomerId")
            .Build();

        // Assert
        group.SharedShardKeyProperty.ShouldBe("CustomerId");
    }

    [Fact]
    public void WithSharedShardKeyProperty_ReturnsSameBuilder_ForChaining()
    {
        // Arrange
        var builder = new ColocationGroupBuilder()
            .WithRootEntity<Order>();

        // Act
        var result = builder.WithSharedShardKeyProperty("CustomerId");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    // ────────────────────────────────────────────────────────────
    //  Build
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithoutRootEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new ColocationGroupBuilder();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_NoColocatedEntities_ReturnsGroupWithEmptyList()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .Build();

        // Assert
        group.ColocatedEntities.ShouldBeEmpty();
    }

    [Fact]
    public void Build_DefaultSharedShardKeyProperty_IsEmptyString()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .Build();

        // Assert
        group.SharedShardKeyProperty.ShouldBe(string.Empty);
    }

    [Fact]
    public void Build_FullConfiguration_ReturnsCorrectGroup()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .AddColocatedEntity<OrderItem>()
            .AddColocatedEntity<OrderPayment>()
            .WithSharedShardKeyProperty("CustomerId")
            .Build();

        // Assert
        group.RootEntity.ShouldBe(typeof(Order));
        group.ColocatedEntities.Count.ShouldBe(2);
        group.SharedShardKeyProperty.ShouldBe("CustomerId");
    }

    [Fact]
    public void Build_ReturnsIColocationGroup()
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .Build();

        // Assert
        group.ShouldBeAssignableTo<IColocationGroup>();
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Order;
    private sealed class OrderItem;
    private sealed class OrderPayment;
}
