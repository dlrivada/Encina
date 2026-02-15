using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="EntityMetadataCache"/>.
/// </summary>
public sealed class EntityMetadataCacheTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class EntityWithKeyAttribute
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class EntityWithIdConvention
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Table("custom_table")]
    private sealed class EntityWithTableAttribute
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class EntityWithColumnAttribute
    {
        public int Id { get; set; }

        [Column("display_name")]
        public string Name { get; set; } = "";
    }

    private sealed class EntityWithoutPrimaryKey
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }

    private sealed class EntityWithMultipleProperties
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public int Age { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    //  Primary Key Discovery
    // ────────────────────────────────────────────────────────────

    #region Primary Key Discovery

    [Fact]
    public void GetOrCreate_KeyAttribute_UsesThatAsKey()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithKeyAttribute>();

        // Assert
        metadata.PrimaryKey.Property.Name.ShouldBe("ProductId");
        metadata.PrimaryKey.IsPrimaryKey.ShouldBeTrue();
    }

    [Fact]
    public void GetOrCreate_IdConvention_UsesIdAsKey()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithIdConvention>();

        // Assert
        metadata.PrimaryKey.Property.Name.ShouldBe("Id");
        metadata.PrimaryKey.IsPrimaryKey.ShouldBeTrue();
    }

    [Fact]
    public void GetOrCreate_NoPrimaryKey_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var act = () => EntityMetadataCache.GetOrCreate<EntityWithoutPrimaryKey>();
        act.ShouldThrow<InvalidOperationException>();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Table Name
    // ────────────────────────────────────────────────────────────

    #region Table Name

    [Fact]
    public void GetOrCreate_TableAttribute_UsesAttributeName()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithTableAttribute>();

        // Assert
        metadata.TableName.ShouldBe("custom_table");
    }

    [Fact]
    public void GetOrCreate_NoTableAttribute_UsesTypeName()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithIdConvention>();

        // Assert
        metadata.TableName.ShouldBe("EntityWithIdConvention");
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Column Name
    // ────────────────────────────────────────────────────────────

    #region Column Name

    [Fact]
    public void GetOrCreate_ColumnAttribute_UsesAttributeName()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithColumnAttribute>();

        // Assert
        var nameProperty = metadata.AllProperties.First(p => p.Property.Name == "Name");
        nameProperty.ColumnName.ShouldBe("display_name");
    }

    [Fact]
    public void GetOrCreate_NoColumnAttribute_UsesPropertyName()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithIdConvention>();

        // Assert
        var nameProperty = metadata.AllProperties.First(p => p.Property.Name == "Name");
        nameProperty.ColumnName.ShouldBe("Name");
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Properties
    // ────────────────────────────────────────────────────────────

    #region Properties

    [Fact]
    public void GetOrCreate_AllProperties_IncludesKeyAndNonKey()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithIdConvention>();

        // Assert
        metadata.AllProperties.Count.ShouldBe(2);
        metadata.NonKeyProperties.Count.ShouldBe(1);
    }

    [Fact]
    public void GetOrCreate_NonKeyProperties_ExcludesPrimaryKey()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithIdConvention>();

        // Assert
        metadata.NonKeyProperties.ShouldAllBe(p => !p.IsPrimaryKey);
    }

    [Fact]
    public void GetOrCreate_MultipleProperties_AllDiscovered()
    {
        // Act
        var metadata = EntityMetadataCache.GetOrCreate<EntityWithMultipleProperties>();

        // Assert
        metadata.AllProperties.Count.ShouldBe(4);
        metadata.NonKeyProperties.Count.ShouldBe(3);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Caching
    // ────────────────────────────────────────────────────────────

    #region Caching

    [Fact]
    public void GetOrCreate_SameType_ReturnsSameInstance()
    {
        // Act
        var meta1 = EntityMetadataCache.GetOrCreate<EntityWithKeyAttribute>();
        var meta2 = EntityMetadataCache.GetOrCreate<EntityWithKeyAttribute>();

        // Assert
        ReferenceEquals(meta1, meta2).ShouldBeTrue();
    }

    [Fact]
    public void GetOrCreate_DifferentTypes_ReturnsDifferentInstances()
    {
        // Act
        var meta1 = EntityMetadataCache.GetOrCreate<EntityWithKeyAttribute>();
        var meta2 = EntityMetadataCache.GetOrCreate<EntityWithIdConvention>();

        // Assert
        ReferenceEquals(meta1, meta2).ShouldBeFalse();
    }

    #endregion
}
