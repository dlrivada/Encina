using Encina.Dapper.MySQL;
using Encina.Dapper.MySQL.Repository;
using Encina.Testing.Shouldly;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.MySQL.Repository;

/// <summary>
/// Unit tests for <see cref="EntityMappingBuilder{TEntity, TId}"/> (MySQL implementation).
/// </summary>
[Trait("Category", "Unit")]
public class EntityMappingBuilderTests
{
    [Fact]
    public void Build_WithValidConfiguration_ReturnsMapping()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "CustomerId")
            .MapProperty(o => o.Total, "Total")
            .MapProperty(o => o.CreatedAtUtc, "CreatedAtUtc")
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.TableName.ShouldBe("Orders");
        mapping.IdColumnName.ShouldBe("Id");
        mapping.ColumnMappings.Count.ShouldBe(4);
        mapping.ColumnMappings["Id"].ShouldBe("Id");
        mapping.ColumnMappings["CustomerId"].ShouldBe("CustomerId");
        mapping.ColumnMappings["Total"].ShouldBe("Total");
        mapping.ColumnMappings["CreatedAtUtc"].ShouldBe("CreatedAtUtc");
    }

    [Fact]
    public void Build_WithCustomColumnNames_UsesCustomNames()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("tbl_orders")
            .HasId(o => o.Id, "order_id")
            .MapProperty(o => o.CustomerId, "customer_id")
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.TableName.ShouldBe("tbl_orders");
        mapping.IdColumnName.ShouldBe("order_id");
        mapping.ColumnMappings["Id"].ShouldBe("order_id");
        mapping.ColumnMappings["CustomerId"].ShouldBe("customer_id");
    }

    [Fact]
    public void Build_WithoutTableName_ReturnsError()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .HasId(o => o.Id);

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldBeErrorWithCode(EntityMappingErrorCodes.MissingTableName);
    }

    [Fact]
    public void Build_WithoutIdProperty_ReturnsError()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("Orders");

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldBeErrorWithCode(EntityMappingErrorCodes.MissingPrimaryKey);
    }

    [Fact]
    public void ExcludeFromInsert_ExcludesPropertyFromInsertExcludedProperties()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "CustomerId")
            .ExcludeFromInsert(o => o.Id)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.InsertExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void ExcludeFromUpdate_ExcludesPropertyFromUpdateExcludedProperties()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "CustomerId")
            .MapProperty(o => o.CreatedAtUtc, "CreatedAtUtc")
            .ExcludeFromUpdate(o => o.CreatedAtUtc)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("CreatedAtUtc");
        mapping.UpdateExcludedProperties.ShouldContain("Id"); // Id is excluded by default
    }

    [Fact]
    public void HasId_AutomaticallyExcludesFromUpdates()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "CustomerId")
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void GetId_ReturnsCorrectIdValue()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "CustomerId")
            .Build()
            .ShouldBeSuccess();

        var order = new TestOrderMySQL
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Total = 100m,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var id = mapping.GetId(order);

        // Assert
        id.ShouldBe(order.Id);
    }

    [Fact]
    public void ToTable_WithSchemaPrefix_AcceptsValidName()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderMySQL, Guid>()
            .ToTable("mydb.Orders")
            .HasId(o => o.Id)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.TableName.ShouldBe("mydb.Orders");
    }
}

/// <summary>
/// Test entity for MySQL Dapper repository tests.
/// </summary>
public class TestOrderMySQL
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
