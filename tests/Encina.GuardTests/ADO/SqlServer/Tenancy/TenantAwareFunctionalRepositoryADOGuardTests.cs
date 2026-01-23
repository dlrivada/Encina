using System.Data;
using Encina.ADO.SqlServer.Tenancy;
using Encina.Tenancy;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.SqlServer.Tenancy;

/// <summary>
/// Guard clause tests for <see cref="TenantAwareFunctionalRepositoryADO{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "ADO.SqlServer")]
public sealed class TenantAwareFunctionalRepositoryADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var mapping = CreateMockMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new ADOTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestOrder, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        ITenantEntityMapping<TenantGuardTestOrder, Guid> mapping = null!;
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new ADOTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestOrder, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var mapping = CreateMockMapping();
        ITenantProvider tenantProvider = null!;
        var options = new ADOTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestOrder, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("tenantProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var mapping = CreateMockMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        ADOTenancyOptions options = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestOrder, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("options");
    }

    private static ITenantEntityMapping<TenantGuardTestOrder, Guid> CreateMockMapping()
    {
        var mapping = Substitute.For<ITenantEntityMapping<TenantGuardTestOrder, Guid>>();
        mapping.TableName.Returns("Orders");
        mapping.IdColumnName.Returns("Id");
        mapping.IsTenantEntity.Returns(true);
        mapping.TenantColumnName.Returns("TenantId");
        return mapping;
    }
}
