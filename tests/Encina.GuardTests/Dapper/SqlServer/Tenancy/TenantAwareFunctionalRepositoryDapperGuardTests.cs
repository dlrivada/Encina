using System.Data;
using Encina.Dapper.SqlServer.Tenancy;
using Encina.Tenancy;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.SqlServer.Tenancy;

/// <summary>
/// Guard clause tests for <see cref="TenantAwareFunctionalRepositoryDapper{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Dapper.SqlServer")]
public sealed class TenantAwareFunctionalRepositoryDapperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var mapping = CreateMockMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new DapperTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestOrder, Guid>(
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
        var options = new DapperTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestOrder, Guid>(
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
        var options = new DapperTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestOrder, Guid>(
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
        DapperTenancyOptions options = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestOrder, Guid>(
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
