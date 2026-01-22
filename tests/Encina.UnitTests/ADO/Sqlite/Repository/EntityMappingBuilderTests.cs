using Encina.ADO.Sqlite.Repository;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.Sqlite.Repository;

/// <summary>
/// Unit tests for <see cref="EntityMappingBuilder{TEntity, TId}"/> in ADO.NET SQLite.
/// </summary>
[Trait("Category", "Unit")]
public class EntityMappingBuilderTests
{
    [Fact]
    public void Build_WithValidConfiguration_ReturnsMapping()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .ToTable("Products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Price, "Price")
            .MapProperty(p => p.IsAvailable, "IsAvailable")
            .Build();

        // Assert
        mapping.TableName.ShouldBe("Products");
        mapping.IdColumnName.ShouldBe("Id");
        mapping.ColumnMappings.Count.ShouldBe(4);
    }

    [Fact]
    public void Build_WithCustomColumnNames_UsesCustomNames()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .ToTable("tbl_products")
            .HasId(p => p.Id, "product_id")
            .MapProperty(p => p.Name, "product_name")
            .Build();

        // Assert
        mapping.TableName.ShouldBe("tbl_products");
        mapping.IdColumnName.ShouldBe("product_id");
        mapping.ColumnMappings["Name"].ShouldBe("product_name");
    }

    [Fact]
    public void Build_WithoutTableName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .HasId(p => p.Id);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Table name must be specified");
    }

    [Fact]
    public void Build_WithoutIdProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .ToTable("Products");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Primary key must be specified");
    }

    [Fact]
    public void ExcludeFromInsert_ExcludesProperty()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .ToTable("Products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .ExcludeFromInsert(p => p.Id)
            .Build();

        // Assert
        mapping.InsertExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void ExcludeFromUpdate_ExcludesProperty()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .ToTable("Products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.CreatedAtUtc, "CreatedAtUtc")
            .ExcludeFromUpdate(p => p.CreatedAtUtc)
            .Build();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("CreatedAtUtc");
    }

    [Fact]
    public void GetId_ReturnsCorrectIdValue()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .ToTable("Products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .Build();

        var product = new TestProductSqlite
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m,
            IsAvailable = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var id = mapping.GetId(product);

        // Assert
        id.ShouldBe(product.Id);
    }

    [Fact]
    public void HasId_AutomaticallyExcludesFromUpdates()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestProductSqlite, Guid>()
            .ToTable("Products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .Build();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("Id");
    }
}

/// <summary>
/// Test entity for ADO.NET SQLite repository tests.
/// </summary>
public class TestProductSqlite
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? Description { get; set; }
}
