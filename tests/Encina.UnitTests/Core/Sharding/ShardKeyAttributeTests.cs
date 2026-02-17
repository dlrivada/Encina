using System.Reflection;
using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardKeyAttribute"/>.
/// </summary>
public sealed class ShardKeyAttributeTests
{
    [Fact]
    public void Attribute_CanBeAppliedToProperty()
    {
        // Act
        var property = typeof(TestEntity).GetProperty(nameof(TestEntity.TenantId));
        var attribute = property?.GetCustomAttribute<ShardKeyAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void Attribute_NotAppliedToUnmarkedProperty()
    {
        // Act
        var property = typeof(TestEntity).GetProperty(nameof(TestEntity.Name));
        var attribute = property?.GetCustomAttribute<ShardKeyAttribute>();

        // Assert
        attribute.ShouldBeNull();
    }

    [Fact]
    public void Attribute_IsInherited()
    {
        // Act
        var property = typeof(DerivedEntity).GetProperty(nameof(DerivedEntity.TenantId));
        var attribute = property?.GetCustomAttribute<ShardKeyAttribute>(inherit: true);

        // Assert
        attribute.ShouldNotBeNull();
    }

    [Fact]
    public void Attribute_TargetsPropertyOnly()
    {
        // Act
        var usage = typeof(ShardKeyAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }

    private class TestEntity
    {
        [ShardKey]
        public string TenantId { get; set; } = default!;

        public string Name { get; set; } = default!;
    }

    private sealed class DerivedEntity : TestEntity;
}
