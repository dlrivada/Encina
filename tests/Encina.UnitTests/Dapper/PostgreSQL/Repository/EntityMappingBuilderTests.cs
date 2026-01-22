using Encina.Dapper.PostgreSQL.Repository;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.PostgreSQL.Repository;

/// <summary>
/// Unit tests for <see cref="EntityMappingBuilder{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Unit")]
public class EntityMappingBuilderTests
{
    [Fact]
    public void Build_WithValidConfiguration_ReturnsMapping()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "customer_id")
            .MapProperty(o => o.Total, "total")
            .MapProperty(o => o.CreatedAtUtc, "created_at_utc")
            .Build();

        // Assert
        mapping.TableName.ShouldBe("orders");
        mapping.IdColumnName.ShouldBe("Id");
        mapping.ColumnMappings.Count.ShouldBe(4);
        mapping.ColumnMappings["Id"].ShouldBe("Id");
        mapping.ColumnMappings["CustomerId"].ShouldBe("customer_id");
        mapping.ColumnMappings["Total"].ShouldBe("total");
        mapping.ColumnMappings["CreatedAtUtc"].ShouldBe("created_at_utc");
    }

    [Fact]
    public void Build_WithCustomColumnNames_UsesCustomNames()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("tbl_orders")
            .HasId(o => o.Id, "order_id")
            .MapProperty(o => o.CustomerId, "customer_id")
            .Build();

        // Assert
        mapping.TableName.ShouldBe("tbl_orders");
        mapping.IdColumnName.ShouldBe("order_id");
        mapping.ColumnMappings["Id"].ShouldBe("order_id");
        mapping.ColumnMappings["CustomerId"].ShouldBe("customer_id");
    }

    [Fact]
    public void Build_WithoutTableName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderPg, Guid>()
            .HasId(o => o.Id);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Table name must be specified");
    }

    [Fact]
    public void Build_WithoutIdProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("orders");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Primary key must be specified");
    }

    [Fact]
    public void ExcludeFromInsert_ExcludesPropertyFromInsertExcludedProperties()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "customer_id")
            .ExcludeFromInsert(o => o.Id)
            .Build();

        // Assert
        mapping.InsertExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void ExcludeFromUpdate_ExcludesPropertyFromUpdateExcludedProperties()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "customer_id")
            .MapProperty(o => o.CreatedAtUtc, "created_at_utc")
            .ExcludeFromUpdate(o => o.CreatedAtUtc)
            .Build();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("CreatedAtUtc");
        mapping.UpdateExcludedProperties.ShouldContain("Id"); // Id is excluded by default
    }

    [Fact]
    public void HasId_AutomaticallyExcludesFromUpdates()
    {
        // Arrange & Act
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "customer_id")
            .Build();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("Id");
    }

    [Fact]
    public void GetId_ReturnsCorrectIdValue()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "customer_id")
            .Build();

        var order = new TestOrderPg
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
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("public.orders")
            .HasId(o => o.Id)
            .Build();

        // Assert
        mapping.TableName.ShouldBe("public.orders");
    }

    [Fact]
    public void HasId_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderPg, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.HasId<Guid>(null!));
    }

    [Fact]
    public void MapProperty_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderPg, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.MapProperty<string>(null!));
    }

    [Fact]
    public void ExcludeFromInsert_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderPg, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromInsert<string>(null!));
    }

    [Fact]
    public void ExcludeFromUpdate_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new EntityMappingBuilder<TestOrderPg, Guid>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ExcludeFromUpdate<string>(null!));
    }

    [Fact]
    public void GetId_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mapping = new EntityMappingBuilder<TestOrderPg, Guid>()
            .ToTable("orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId, "customer_id")
            .Build();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => mapping.GetId(null!));
    }
}

/// <summary>
/// Test entity for PostgreSQL Dapper repository tests.
/// </summary>
public class TestOrderPg
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
