using Encina.Sharding.ReferenceTables;

namespace Encina.GuardTests.Sharding.ReferenceTables;

/// <summary>
/// Guard clause tests for <see cref="EntityMetadataCache"/>.
/// </summary>
public sealed class EntityMetadataCacheGuardTests
{
    /// <summary>
    /// An entity type with no primary key property and no [Key] attribute,
    /// used to verify <see cref="EntityMetadataCache"/> throws when no key is found.
    /// </summary>
    private sealed class EntityWithoutKey
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }

    [Fact]
    public void GetOrCreate_EntityWithoutPrimaryKey_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => EntityMetadataCache.GetOrCreate<EntityWithoutKey>();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void GetOrCreate_NullType_ThrowsArgumentNullException()
    {
        // Act
        var act = () => EntityMetadataCache.GetOrCreate(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }
}
