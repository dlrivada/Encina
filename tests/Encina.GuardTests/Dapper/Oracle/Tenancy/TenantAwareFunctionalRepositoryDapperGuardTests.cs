using System.Data;
using Encina.Dapper.Oracle.Tenancy;
using Encina.Tenancy;
using NSubstitute;

namespace Encina.GuardTests.Dapper.Oracle.Tenancy;

/// <summary>
/// Guard tests for <see cref="TenantAwareFunctionalRepositoryDapper{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TenantAwareFunctionalRepositoryDapperGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
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
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperOracle, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("connection");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when mapping is null.
    /// </summary>
    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        ITenantEntityMapping<TenantGuardTestEntityDapperOracle, Guid> mapping = null!;
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new DapperTenancyOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperOracle, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("mapping");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when tenantProvider is null.
    /// </summary>
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
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperOracle, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("tenantProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
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
            new TenantAwareFunctionalRepositoryDapper<TenantGuardTestEntityDapperOracle, Guid>(
                connection, mapping, tenantProvider, options));
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Creates a test mapping for the guard test entity.
    /// </summary>
    private static ITenantEntityMapping<TenantGuardTestEntityDapperOracle, Guid> CreateTestMapping()
    {
        return new TenantEntityMappingBuilder<TenantGuardTestEntityDapperOracle, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .HasTenantId(e => e.TenantId)
            .Build();
    }
}

/// <summary>
/// Test entity for guard tests.
/// </summary>
public sealed class TenantGuardTestEntityDapperOracle : ITenantEntity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
}
