using Encina.Sharding.Colocation;
using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ColocationGroupRegistry"/>.
/// </summary>
public sealed class ColocationGroupRegistryGuardTests
{
    // ────────────────────────────────────────────────────────────
    //  RegisterGroup
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterGroup_NullGroup_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => registry.RegisterGroup(null!));
        ex.ParamName.ShouldBe("group");
    }

    // ────────────────────────────────────────────────────────────
    //  RegisterColocatedEntity
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterColocatedEntity_NullRootEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => registry.RegisterColocatedEntity(null!, typeof(string)));
        ex.ParamName.ShouldBe("rootEntity");
    }

    [Fact]
    public void RegisterColocatedEntity_NullColocatedEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => registry.RegisterColocatedEntity(typeof(string), null!));
        ex.ParamName.ShouldBe("colocatedEntity");
    }

    // ────────────────────────────────────────────────────────────
    //  TryGetGroup
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void TryGetGroup_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ColocationGroupRegistry();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => registry.TryGetGroup(null!, out _));
        ex.ParamName.ShouldBe("entityType");
    }
}
