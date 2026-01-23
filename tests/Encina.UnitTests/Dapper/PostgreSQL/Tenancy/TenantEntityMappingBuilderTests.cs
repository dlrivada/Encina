using Encina.Dapper.PostgreSQL.Tenancy;
using Encina.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.PostgreSQL.Tenancy;

[Trait("Category", "Unit")]
public sealed class TenantEntityMappingBuilderTests
{
    [Fact]
    public void Build_WithTenantIdProperty_ReturnsTenantEntityMapping()
    {
        var mapping = new TenantEntityMappingBuilder<DapperPostgreSQLTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total)
            .Build();

        mapping.IsTenantEntity.ShouldBeTrue();
        mapping.TenantColumnName.ShouldBe("TenantId");
        mapping.TableName.ShouldBe("Orders");
        mapping.ColumnMappings.Count.ShouldBe(4);
    }

    [Fact]
    public void Build_WithCustomTenantColumnName_UsesCustomName()
    {
        var mapping = new TenantEntityMappingBuilder<DapperPostgreSQLTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId, "organization_id")
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.TenantColumnName.ShouldBe("organization_id");
    }

    [Fact]
    public void Build_WithoutTenantIdProperty_ReturnsNonTenantMapping()
    {
        var mapping = new TenantEntityMappingBuilder<DapperPostgreSQLTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.IsTenantEntity.ShouldBeFalse();
        mapping.TenantColumnName.ShouldBeNull();
    }

    [Fact]
    public void Build_WithoutTableName_ThrowsInvalidOperationException()
    {
        var builder = new TenantEntityMappingBuilder<DapperPostgreSQLTenantTestOrder, Guid>()
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId);

        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Table name must be specified");
    }

    [Fact]
    public void HasTenantId_AutomaticallyExcludesFromUpdates()
    {
        var mapping = new TenantEntityMappingBuilder<DapperPostgreSQLTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        mapping.UpdateExcludedProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void GetTenantId_ReturnsCorrectTenantIdValue()
    {
        var mapping = new TenantEntityMappingBuilder<DapperPostgreSQLTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        var order = new DapperPostgreSQLTenantTestOrder { Id = Guid.NewGuid(), TenantId = "tenant-123" };
        mapping.GetTenantId(order).ShouldBe("tenant-123");
    }

    [Fact]
    public void SetTenantId_SetsCorrectTenantIdValue()
    {
        var mapping = new TenantEntityMappingBuilder<DapperPostgreSQLTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .Build();

        var order = new DapperPostgreSQLTenantTestOrder { Id = Guid.NewGuid() };
        mapping.SetTenantId(order, "tenant-456");
        order.TenantId.ShouldBe("tenant-456");
    }
}

public sealed class DapperPostgreSQLTenantTestOrder : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; }
}
