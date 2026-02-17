using Encina.Sharding.Colocation;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="ColocationGroupBuilder"/> fluency and invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class ColocationGroupBuilderPropertyTests
{
    // ────────────────────────────────────────────────────────────
    //  Fluent chaining: all builder methods return same instance
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_WithRootEntity_ReturnsSameBuilderInstance(byte seed)
    {
        // Arrange
        var builder = new ColocationGroupBuilder();

        // Act
        var result = builder.WithRootEntity<Order>();

        // Assert
        return ReferenceEquals(builder, result);
    }

    [Property(MaxTest = 50)]
    public bool Property_AddColocatedEntity_ReturnsSameBuilderInstance(byte seed)
    {
        // Arrange
        var builder = new ColocationGroupBuilder();

        // Act
        var result = builder.AddColocatedEntity<OrderItem>();

        // Assert
        return ReferenceEquals(builder, result);
    }

    [Property(MaxTest = 50)]
    public bool Property_WithSharedShardKeyProperty_ReturnsSameBuilderInstance(NonEmptyString propName)
    {
        // Arrange
        var builder = new ColocationGroupBuilder();

        // Act
        var result = builder.WithSharedShardKeyProperty(propName.Get);

        // Assert
        return ReferenceEquals(builder, result);
    }

    // ────────────────────────────────────────────────────────────
    //  Build produces consistent group
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_Build_ProducesGroupWithCorrectRootEntity(byte seed)
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .Build();

        // Assert
        return group.RootEntity == typeof(Order);
    }

    [Property(MaxTest = 50)]
    public bool Property_Build_SharedShardKeyPropertyMatchesInput(NonEmptyString propName)
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .WithSharedShardKeyProperty(propName.Get)
            .Build();

        // Assert
        return group.SharedShardKeyProperty == propName.Get;
    }

    [Property(MaxTest = 50)]
    public bool Property_Build_DefaultSharedShardKeyProperty_IsEmptyString(byte seed)
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .Build();

        // Assert
        return group.SharedShardKeyProperty == string.Empty;
    }

    // ────────────────────────────────────────────────────────────
    //  Duplicate co-located entities are deduplicated
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_AddColocatedEntity_DuplicatesAreIgnored(PositiveInt repeatCount)
    {
        // Arrange
        var builder = new ColocationGroupBuilder()
            .WithRootEntity<Order>();

        // Act — add same entity multiple times
        var count = Math.Min(repeatCount.Get, 10); // Cap to avoid extreme cases
        for (var i = 0; i < count; i++)
        {
            builder.AddColocatedEntity<OrderItem>();
        }

        var group = builder.Build();

        // Assert — should have exactly 1 entry
        return group.ColocatedEntities.Count == 1
               && group.ColocatedEntities[0] == typeof(OrderItem);
    }

    // ────────────────────────────────────────────────────────────
    //  Build result implements IColocationGroup
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_Build_ResultImplementsIColocationGroup(byte seed)
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .Build();

        // Assert
        return group is IColocationGroup;
    }

    // ────────────────────────────────────────────────────────────
    //  Multiple co-located entities are preserved in order
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_Build_MultipleColocatedEntities_AllPreserved(byte seed)
    {
        // Arrange & Act
        var group = new ColocationGroupBuilder()
            .WithRootEntity<Order>()
            .AddColocatedEntity<OrderItem>()
            .AddColocatedEntity<OrderPayment>()
            .Build();

        // Assert
        return group.ColocatedEntities.Count == 2
               && group.ColocatedEntities.Contains(typeof(OrderItem))
               && group.ColocatedEntities.Contains(typeof(OrderPayment));
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Order;
    private sealed class OrderItem;
    private sealed class OrderPayment;
}
