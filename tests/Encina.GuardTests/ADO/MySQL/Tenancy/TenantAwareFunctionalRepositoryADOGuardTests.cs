using System.Data;
using Encina.ADO.MySQL.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using NSubstitute;

namespace Encina.GuardTests.ADO.MySQL.Tenancy;

/// <summary>
/// Guard tests for <see cref="TenantAwareFunctionalRepositoryADO{TEntity, TId}"/> to verify null parameter handling.
/// </summary>
[Trait("Category", "Guard")]
public class TenantAwareFunctionalRepositoryADOGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        var mapping = Substitute.For<ITenantEntityMapping<TenantGuardTestEntityMySQL, Guid>>();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new ADOTenancyOptions();

        // Act & Assert
        var act = () => new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityMySQL, Guid>(
            connection, mapping, tenantProvider, options);
        var ex = Should.Throw<ArgumentNullException>(act);
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
        ITenantEntityMapping<TenantGuardTestEntityMySQL, Guid> mapping = null!;
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new ADOTenancyOptions();

        // Act & Assert
        var act = () => new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityMySQL, Guid>(
            connection, mapping, tenantProvider, options);
        var ex = Should.Throw<ArgumentNullException>(act);
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
        var mapping = CreateValidMapping();
        ITenantProvider tenantProvider = null!;
        var options = new ADOTenancyOptions();

        // Act & Assert
        var act = () => new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityMySQL, Guid>(
            connection, mapping, tenantProvider, options);
        var ex = Should.Throw<ArgumentNullException>(act);
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
        var mapping = CreateValidMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        ADOTenancyOptions options = null!;

        // Act & Assert
        var act = () => new TenantAwareFunctionalRepositoryADO<TenantGuardTestEntityMySQL, Guid>(
            connection, mapping, tenantProvider, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Creates a valid tenant entity mapping for testing.
    /// </summary>
    public static ITenantEntityMapping<TenantGuardTestEntityMySQL, Guid> CreateValidMapping()
    {
        var mapping = Substitute.For<ITenantEntityMapping<TenantGuardTestEntityMySQL, Guid>>();
        mapping.TableName.Returns("TestEntities");
        mapping.IdColumnName.Returns("Id");
        mapping.TenantColumnName.Returns("TenantId");
        mapping.ColumnMappings.Returns(new Dictionary<string, string>
        {
            { "Id", "Id" },
            { "TenantId", "TenantId" },
            { "Name", "Name" }
        });
        mapping.InsertExcludedProperties.Returns(new System.Collections.Generic.HashSet<string>());
        mapping.UpdateExcludedProperties.Returns(new System.Collections.Generic.HashSet<string>());
        mapping.IsTenantEntity.Returns(true);

        return mapping;
    }
}

/// <summary>
/// Test entity for TenantAwareFunctionalRepositoryADO guard tests.
/// </summary>
public class TenantGuardTestEntityMySQL
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = null!;
    public string Name { get; set; } = null!;
}
