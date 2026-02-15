using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableAttribute"/>.
/// </summary>
public sealed class ReferenceTableAttributeTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    [ReferenceTable]
    private sealed class DecoratedEntity
    {
        public int Id { get; set; }
    }

    private sealed class UndecoratedEntity
    {
        public int Id { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    //  Attribute Presence
    // ────────────────────────────────────────────────────────────

    #region Attribute Presence

    [Fact]
    public void DecoratedEntity_HasReferenceTableAttribute()
    {
        // Arrange
        var attribute = Attribute.GetCustomAttribute(
            typeof(DecoratedEntity),
            typeof(ReferenceTableAttribute));

        // Assert
        attribute.ShouldNotBeNull();
        attribute.ShouldBeOfType<ReferenceTableAttribute>();
    }

    [Fact]
    public void UndecoratedEntity_DoesNotHaveReferenceTableAttribute()
    {
        // Arrange
        var attribute = Attribute.GetCustomAttribute(
            typeof(UndecoratedEntity),
            typeof(ReferenceTableAttribute));

        // Assert
        attribute.ShouldBeNull();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Attribute Usage
    // ────────────────────────────────────────────────────────────

    #region Attribute Usage

    [Fact]
    public void Attribute_TargetsClassOnly()
    {
        // Arrange
        var usage = Attribute.GetCustomAttribute(
            typeof(ReferenceTableAttribute),
            typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;

        // Assert
        usage.ShouldNotBeNull();
        usage!.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_AllowMultipleIsFalse()
    {
        // Arrange
        var usage = Attribute.GetCustomAttribute(
            typeof(ReferenceTableAttribute),
            typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;

        // Assert
        usage.ShouldNotBeNull();
        usage!.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void Attribute_InheritedIsFalse()
    {
        // Arrange
        var usage = Attribute.GetCustomAttribute(
            typeof(ReferenceTableAttribute),
            typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;

        // Assert
        usage.ShouldNotBeNull();
        usage!.Inherited.ShouldBeFalse();
    }

    #endregion
}
