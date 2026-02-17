using Encina.Sharding.Colocation;
using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ColocationGroupBuilder"/>.
/// </summary>
public sealed class ColocationGroupBuilderGuardTests
{
    // ────────────────────────────────────────────────────────────
    //  WithSharedShardKeyProperty
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void WithSharedShardKeyProperty_NullPropertyName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ColocationGroupBuilder();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => builder.WithSharedShardKeyProperty(null!));
        ex.ParamName.ShouldBe("propertyName");
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
        var ex = Should.Throw<InvalidOperationException>(() => builder.Build());
        ex.Message.ShouldContain("Root entity type must be set");
    }
}
