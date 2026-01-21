using Encina.MongoDB.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantEntityMappingBuilder{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantEntityMappingBuilderTests
{
    [Fact]
    public void Build_WithTenantIdProperty_ReturnsTenantEntityMapping()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapField(o => o.CustomerId);

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.TenantFieldName.ShouldBe("TenantId");
        mapping.TenantPropertyName.ShouldBe("TenantId");
        mapping.CollectionName.ShouldBe("orders");
    }

    [Fact]
    public void Build_WithoutTenantIdProperty_ReturnsNonTenantMapping()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId);

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.IsTenantEntity.ShouldBeFalse();
        mapping.TenantFieldName.ShouldBeNull();
        mapping.TenantPropertyName.ShouldBeNull();
    }

    [Fact]
    public void Build_WithCustomTenantFieldName_UsesCustomName()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId, "OrganizationId")
            .MapField(o => o.CustomerId);

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.TenantFieldName.ShouldBe("OrganizationId");
        mapping.TenantPropertyName.ShouldBe("TenantId");
    }

    [Fact]
    public void Build_WithoutCollectionName_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Collection name");
    }

    [Fact]
    public void Build_WithoutIdProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .MapField(o => o.CustomerId);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Primary key");
    }

    [Fact]
    public void ToCollection_WithSchemaPrefix_AcceptsValidName()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("mydb.orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId);

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.CollectionName.ShouldBe("mydb.orders");
    }

    [Fact]
    public void GetTenantId_ReturnsCorrectTenantIdValue()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapField(o => o.CustomerId)
            .Build();

        var order = new MongoTenantTestOrder { TenantId = "tenant-xyz" };

        // Act
        var tenantId = mapping.GetTenantId(order);

        // Assert
        tenantId.ShouldBe("tenant-xyz");
    }

    [Fact]
    public void GetTenantId_OnNonTenantEntity_ReturnsNull()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId)
            .Build();

        var order = new MongoTenantTestOrder { TenantId = "tenant-xyz" };

        // Act
        var tenantId = mapping.GetTenantId(order);

        // Assert
        tenantId.ShouldBeNull();
    }

    [Fact]
    public void SetTenantId_SetsCorrectTenantIdValue()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapField(o => o.CustomerId)
            .Build();

        var order = new MongoTenantTestOrder();

        // Act
        mapping.SetTenantId(order, "tenant-123");

        // Assert
        order.TenantId.ShouldBe("tenant-123");
    }

    [Fact]
    public void SetTenantId_OnNonTenantEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId)
            .Build();

        var order = new MongoTenantTestOrder();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => mapping.SetTenantId(order, "tenant-123"))
            .Message.ShouldContain("not configured as a tenant entity");
    }

    [Fact]
    public void GetId_ReturnsCorrectIdValue()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId)
            .Build();

        var orderId = Guid.NewGuid();
        var order = new MongoTenantTestOrder { Id = orderId };

        // Act
        var id = mapping.GetId(order);

        // Assert
        id.ShouldBe(orderId);
    }

    [Fact]
    public void MapField_WithCustomFieldName_UsesCustomName()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId, "customer_id");

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.FieldMappings["CustomerId"].ShouldBe("customer_id");
    }

    [Fact]
    public void HasId_WithCustomFieldName_UsesCustomName()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id, "order_id")
            .MapField(o => o.CustomerId);

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.IdFieldName.ShouldBe("order_id");
        mapping.FieldMappings["Id"].ShouldBe("order_id");
    }

    [Fact]
    public void HasId_DefaultsToUnderscoreId()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .MapField(o => o.CustomerId);

        // Act
        var mapping = builder.Build();

        // Assert
        mapping.IdFieldName.ShouldBe("_id");
    }
}

/// <summary>
/// Test entity for MongoDB tenancy tests.
/// </summary>
public sealed class MongoTenantTestOrder
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public bool IsActive { get; set; }
}
