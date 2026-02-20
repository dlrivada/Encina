using Encina.ADO.SqlServer;
using Encina.ADO.SqlServer.Tenancy;
using Encina.Tenancy;
using Encina.Testing.Shouldly;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.SqlServer.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantEntityMappingBuilder{TEntity, TId}"/> in ADO.NET.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantEntityMappingBuilderTests
{
    [Fact]
    public void Build_WithTenantIdProperty_ReturnsTenantEntityMapping()
    {
        // Arrange & Act
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.TenantColumnName.ShouldBe("TenantId");
        mapping.TenantPropertyName.ShouldBe("TenantId");
        mapping.TableName.ShouldBe("Orders");
        mapping.IdColumnName.ShouldBe("Id");
        mapping.ColumnMappings.Count.ShouldBe(4);
    }

    [Fact]
    public void Build_WithCustomTenantColumnName_UsesCustomName()
    {
        // Arrange & Act
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId, "organization_id")
            .MapProperty(o => o.CustomerId)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.TenantColumnName.ShouldBe("organization_id");
        mapping.TenantPropertyName.ShouldBe("TenantId");
        mapping.ColumnMappings["TenantId"].ShouldBe("organization_id");
    }

    [Fact]
    public void Build_WithoutTenantIdProperty_ReturnsNonTenantMapping()
    {
        // Arrange & Act
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.IsTenantEntity.ShouldBeFalse();
        mapping.TenantColumnName.ShouldBeNull();
        mapping.TenantPropertyName.ShouldBeNull();
    }

    [Fact]
    public void Build_WithoutTableName_ReturnsError()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId);

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldBeErrorWithCode(EntityMappingErrorCodes.MissingTableName);
    }

    [Fact]
    public void Build_WithoutIdProperty_ReturnsError()
    {
        // Arrange
        var builder = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasTenantId(o => o.TenantId);

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldBeErrorWithCode(EntityMappingErrorCodes.MissingPrimaryKey);
    }

    [Fact]
    public void HasTenantId_AutomaticallyExcludesFromUpdates()
    {
        // Arrange & Act
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void GetTenantId_ReturnsCorrectTenantIdValue()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build()
            .ShouldBeSuccess();

        var order = new ADOTenantTestOrder
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-123",
            CustomerId = Guid.NewGuid(),
            Total = 100m
        };

        // Act
        var tenantId = mapping.GetTenantId(order);

        // Assert
        tenantId.ShouldBe("tenant-123");
    }

    [Fact]
    public void SetTenantId_SetsCorrectTenantIdValue()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build()
            .ShouldBeSuccess();

        var order = new ADOTenantTestOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Total = 100m
        };

        // Act
        mapping.SetTenantId(order, "tenant-456");

        // Assert
        order.TenantId.ShouldBe("tenant-456");
    }

    [Fact]
    public void SetTenantId_OnNonTenantEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId)
            .Build()
            .ShouldBeSuccess();

        var order = new ADOTenantTestOrder { Id = Guid.NewGuid() };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => mapping.SetTenantId(order, "tenant-123"))
            .Message.ShouldContain("not configured as a tenant entity");
    }

    [Fact]
    public void GetTenantId_OnNonTenantEntity_ReturnsNull()
    {
        // Arrange
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId)
            .Build()
            .ShouldBeSuccess();

        var order = new ADOTenantTestOrder
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-123"
        };

        // Act
        var tenantId = mapping.GetTenantId(order);

        // Assert - Returns null because mapping is not configured for tenant
        tenantId.ShouldBeNull();
    }

    [Fact]
    public void ExcludeFromInsert_ExcludesPropertyFromInsertExcludedProperties()
    {
        // Arrange & Act
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
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
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.CreatedAtUtc)
            .ExcludeFromUpdate(o => o.CreatedAtUtc)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.UpdateExcludedProperties.ShouldContain("CreatedAtUtc");
    }

    [Fact]
    public void ToTable_WithSchemaPrefix_AcceptsValidName()
    {
        // Arrange & Act
        var mapping = new TenantEntityMappingBuilder<ADOTenantTestOrder, Guid>()
            .ToTable("dbo.Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeSuccess();

        // Assert
        mapping.TableName.ShouldBe("dbo.Orders");
    }
}

/// <summary>
/// Test entity implementing <see cref="ITenantEntity"/> for ADO.NET tenancy tests.
/// </summary>
public sealed class ADOTenantTestOrder : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
