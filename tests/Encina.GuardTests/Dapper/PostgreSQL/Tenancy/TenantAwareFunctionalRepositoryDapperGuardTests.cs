using System.Data;
using Encina.Dapper.PostgreSQL.Tenancy;
using Encina.Tenancy;

namespace Encina.GuardTests.Dapper.PostgreSQL.Tenancy;

/// <summary>
/// Guard tests for TenantAwareFunctionalRepositoryDapper to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TenantAwareFunctionalRepositoryDapperGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var mapping = CreateTestMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new DapperTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperPostgreSQL, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        ITenantEntityMapping<TenantGuardTestEntityDapperPostgreSQL, Guid> mapping = null!;
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new DapperTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperPostgreSQL, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("mapping");
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var mapping = CreateTestMapping();
        ITenantProvider tenantProvider = null!;
        var options = new DapperTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperPostgreSQL, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("tenantProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var mapping = CreateTestMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        DapperTenancyOptions options = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperPostgreSQL, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("options");
    }

    private static ITenantEntityMapping<TenantGuardTestEntityDapperPostgreSQL, Guid> CreateTestMapping()
    {
        return new TenantEntityMappingBuilder<TenantGuardTestEntityDapperPostgreSQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .HasTenantId(e => e.TenantId)
            .Build()
            .ShouldBeSuccess();
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class TenantGuardTestEntityDapperPostgreSQL : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
