using Encina.ADO.PostgreSQL.Repository;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.PostgreSQL.Repository;

/// <summary>
/// Unit tests for <see cref="EntityMappingBuilder{TEntity, TId}"/> in ADO.NET PostgreSQL.
/// </summary>
[Trait("Category", "Unit")]
public class EntityMappingBuilderTests
{
    [Fact]
    public void Build_WithValidConfiguration_ReturnsMapping()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
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
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
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
        var builder = new EntityMappingBuilder<TestProductPg, Guid>()
            .HasId(p => p.Id);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Table name must be specified");
    }

    [Fact]
    public void Build_WithoutIdProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestProductPg, Guid>()
            .ToTable("Products");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Primary key must be specified");
    }

    [Fact]
    public void ExcludeFromInsert_ExcludesProperty()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
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
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
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
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
            .ToTable("Products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .Build();

        var product = new TestProductPg
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
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
            .ToTable("Products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .Build();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void Build_WithSnakeCaseColumnNames_MapsCorrectly()
    {
        // Arrange & Act - PostgreSQL typically uses snake_case
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
            .ToTable("products")
            .HasId(p => p.Id, "id")
            .MapProperty(p => p.Name, "product_name")
            .MapProperty(p => p.Price, "unit_price")
            .MapProperty(p => p.IsAvailable, "is_available")
            .MapProperty(p => p.CreatedAtUtc, "created_at_utc")
            .Build();

        // Assert
        mapping.TableName.ShouldBe("products");
        mapping.IdColumnName.ShouldBe("id");
        mapping.ColumnMappings["Name"].ShouldBe("product_name");
        mapping.ColumnMappings["Price"].ShouldBe("unit_price");
        mapping.ColumnMappings["IsAvailable"].ShouldBe("is_available");
        mapping.ColumnMappings["CreatedAtUtc"].ShouldBe("created_at_utc");
    }

    [Fact]
    public void Build_WithSchemaPrefix_AcceptsTableName()
    {
        // Arrange & Act - PostgreSQL supports schema-qualified table names
        var mapping = new EntityMappingBuilder<TestProductPg, Guid>()
            .ToTable("public.products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .Build();

        // Assert
        mapping.TableName.ShouldBe("public.products");
    }
}

/// <summary>
/// Test entity for ADO.NET PostgreSQL repository tests.
/// </summary>
public class TestProductPg
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? Description { get; set; }
}
