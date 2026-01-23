using System.Data;
using Encina.ADO.PostgreSQL.Tenancy;
using Encina.Tenancy;

namespace Encina.GuardTests.ADO.PostgreSQL.Tenancy;

/// <summary>
/// Guard tests for TenantAwareFunctionalRepositoryADO to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TenantAwareFunctionalRepositoryADOGuardTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var mapping = CreateTestMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new ADOTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityPostgreSQL, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        ITenantEntityMapping<TenantGuardTestEntityPostgreSQL, Guid> mapping = null!;
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new ADOTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityPostgreSQL, Guid>(
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
        var options = new ADOTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityPostgreSQL, Guid>(
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
        ADOTenancyOptions options = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityPostgreSQL, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("options");
    }

    private static ITenantEntityMapping<TenantGuardTestEntityPostgreSQL, Guid> CreateTestMapping()
    {
        return new TenantEntityMappingBuilder<TenantGuardTestEntityPostgreSQL, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .HasTenantId(e => e.TenantId)
            .Build();
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class TenantGuardTestEntityPostgreSQL : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
