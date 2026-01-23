using System.Linq.Expressions;
using Encina.Dapper.Oracle.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.Oracle.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantAwareSpecificationSqlBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantAwareSpecificationSqlBuilderTests
{
    private readonly ITenantProvider _tenantProvider;
    private readonly DapperTenancyOptions _options;
    private readonly ITenantEntityMapping<DapperOracleTenantTestOrder, object> _tenantMapping;
    private readonly ITenantEntityMapping<DapperOracleTenantTestOrder, object> _nonTenantMapping;

    public TenantAwareSpecificationSqlBuilderTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _options = new DapperTenancyOptions();

        // Create tenant entity mapping
        var tenantMappingBuilder = new TenantEntityMappingBuilder<DapperOracleTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total)
            .MapProperty(o => o.IsActive);

        var tenantMapping = tenantMappingBuilder.Build();
        _tenantMapping = new OracleGenericTenantMappingAdapter<DapperOracleTenantTestOrder, Guid>(tenantMapping);

        // Create non-tenant entity mapping
        var nonTenantMappingBuilder = new TenantEntityMappingBuilder<DapperOracleTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total);

        var nonTenantMapping = nonTenantMappingBuilder.Build();
        _nonTenantMapping = new OracleGenericTenantMappingAdapter<DapperOracleTenantTestOrder, Guid>(nonTenantMapping);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(null!, _tenantProvider, _options));
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, _tenantProvider, null!));
    }

    #endregion

    #region BuildWhereClause Tests

    [Fact]
    public void BuildWhereClause_WithTenantContext_IncludesTenantFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var builder = new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, _tenantProvider, _options);
        var spec = new OracleTenantActiveOrdersSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert - Oracle uses double quotes and :param syntax
        whereClause.ShouldContain("\"TenantId\" = :tenantId");
        whereClause.ShouldContain("\"IsActive\" = 1");
        parameters.ShouldContainKey("tenantId");
        parameters["tenantId"].ShouldBe("tenant-123");
    }

    [Fact]
    public void BuildWhereClause_WithoutTenantContext_ThrowsException_WhenConfigured()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var options = new DapperTenancyOptions { ThrowOnMissingTenantContext = true };
        var builder = new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new OracleTenantActiveOrdersSpec();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.BuildWhereClause(spec))
            .Message.ShouldContain("without tenant context");
    }

    [Fact]
    public void BuildWhereClause_WithoutTenantContext_NoFilter_WhenNotThrowConfigured()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var options = new DapperTenancyOptions { ThrowOnMissingTenantContext = false };
        var builder = new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new OracleTenantActiveOrdersSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("TenantId");
        parameters.ShouldNotContainKey("tenantId");
    }

    [Fact]
    public void BuildWhereClause_NonTenantEntity_NoFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var builder = new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_nonTenantMapping, _tenantProvider, _options);
        var spec = new OracleTenantActiveOrdersSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("TenantId");
        parameters.ShouldNotContainKey("tenantId");
    }

    [Fact]
    public void BuildWhereClause_AutoFilterDisabled_NoFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var options = new DapperTenancyOptions { AutoFilterTenantQueries = false };
        var builder = new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new OracleTenantActiveOrdersSpec();

        // Act
        var (whereClause, parameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("TenantId");
        parameters.ShouldNotContainKey("tenantId");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithTenantContext_IncludesTenantFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-abc");
        var builder = new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, _tenantProvider, _options);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("Orders");

        // Assert - Oracle uses double quotes for identifiers
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldContain("\"TenantId\" = :tenantId");
        parameters["tenantId"].ShouldBe("tenant-abc");
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_CombinesFilters()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-xyz");
        var builder = new TenantAwareSpecificationSqlBuilder<DapperOracleTenantTestOrder>(_tenantMapping, _tenantProvider, _options);
        var spec = new OracleTenantMinTotalSpec(100m);

        // Act
        var (sql, parameters) = builder.BuildSelectStatement("Orders", spec);

        // Assert - Oracle uses :param syntax
        sql.ShouldContain("\"TenantId\" = :tenantId");
        sql.ShouldContain("\"Total\" >");
        parameters["tenantId"].ShouldBe("tenant-xyz");
    }

    #endregion
}

/// <summary>
/// Internal adapter for tests - allows converting typed mapping to object-based.
/// </summary>
internal sealed class OracleGenericTenantMappingAdapter<TEntity, TId> : ITenantEntityMapping<TEntity, object>
    where TEntity : class
    where TId : notnull
{
    private readonly ITenantEntityMapping<TEntity, TId> _innerMapping;

    public OracleGenericTenantMappingAdapter(ITenantEntityMapping<TEntity, TId> innerMapping)
    {
        _innerMapping = innerMapping;
    }

    public string TableName => _innerMapping.TableName;
    public string IdColumnName => _innerMapping.IdColumnName;
    public IReadOnlyDictionary<string, string> ColumnMappings => _innerMapping.ColumnMappings;
    public IReadOnlySet<string> InsertExcludedProperties => _innerMapping.InsertExcludedProperties;
    public IReadOnlySet<string> UpdateExcludedProperties => _innerMapping.UpdateExcludedProperties;
    public bool IsTenantEntity => _innerMapping.IsTenantEntity;
    public string? TenantColumnName => _innerMapping.TenantColumnName;
    public string? TenantPropertyName => _innerMapping.TenantPropertyName;

    public object GetId(TEntity entity) => _innerMapping.GetId(entity)!;
    public string? GetTenantId(TEntity entity) => _innerMapping.GetTenantId(entity);
    public void SetTenantId(TEntity entity, string tenantId) => _innerMapping.SetTenantId(entity, tenantId);
}

#region Test Specifications

/// <summary>
/// Specification that filters for active tenant orders.
/// </summary>
internal sealed class OracleTenantActiveOrdersSpec : Specification<DapperOracleTenantTestOrder>
{
    public override Expression<Func<DapperOracleTenantTestOrder, bool>> ToExpression()
        => o => o.IsActive;
}

/// <summary>
/// Specification that filters for orders with minimum total.
/// </summary>
internal sealed class OracleTenantMinTotalSpec : Specification<DapperOracleTenantTestOrder>
{
    private readonly decimal _minTotal;

    public OracleTenantMinTotalSpec(decimal minTotal) => _minTotal = minTotal;

    public override Expression<Func<DapperOracleTenantTestOrder, bool>> ToExpression()
        => o => o.Total > _minTotal;
}

#endregion
