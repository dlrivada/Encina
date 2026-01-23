using System.Data;
using System.Linq.Expressions;
using Encina.ADO.Oracle.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.Oracle.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantAwareSpecificationSqlBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantAwareSpecificationSqlBuilderTests
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ADOTenancyOptions _options;
    private readonly ITenantEntityMapping<OracleTenantTestOrder, object> _tenantMapping;
    private readonly ITenantEntityMapping<OracleTenantTestOrder, object> _nonTenantMapping;

    public TenantAwareSpecificationSqlBuilderTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _options = new ADOTenancyOptions();

        // Create tenant entity mapping
        var tenantMappingBuilder = new TenantEntityMappingBuilder<OracleTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total)
            .MapProperty(o => o.IsActive);

        var tenantMapping = tenantMappingBuilder.Build();
        _tenantMapping = new OracleGenericTenantMappingAdapter<OracleTenantTestOrder, Guid>(tenantMapping);

        // Create non-tenant entity mapping
        var nonTenantMappingBuilder = new TenantEntityMappingBuilder<OracleTenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total);

        var nonTenantMapping = nonTenantMappingBuilder.Build();
        _nonTenantMapping = new OracleGenericTenantMappingAdapter<OracleTenantTestOrder, Guid>(nonTenantMapping);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullMapping_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(null!, _tenantProvider, _options));
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, _tenantProvider, null!));
    }

    #endregion

    #region BuildWhereClause Tests

    [Fact]
    public void BuildWhereClause_WithTenantContext_IncludesTenantFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var builder = new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, _tenantProvider, _options);
        var spec = new OracleActiveOrdersSpec();

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);

        // Assert - Oracle uses double quotes for identifiers and colon for parameters
        whereClause.ShouldContain("\"TenantId\" = :TenantId");
        whereClause.ShouldContain("\"IsActive\" = 1");

        // Verify parameters
        var mockCommand = CreateMockCommand();
        addParameters(mockCommand);
        mockCommand.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    public void BuildWhereClause_WithoutTenantContext_ThrowsException_WhenConfigured()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var options = new ADOTenancyOptions { ThrowOnMissingTenantContext = true };
        var builder = new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new OracleActiveOrdersSpec();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.BuildWhereClause(spec))
            .Message.ShouldContain("without tenant context");
    }

    [Fact]
    public void BuildWhereClause_WithoutTenantContext_NoFilter_WhenNotThrowConfigured()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var options = new ADOTenancyOptions { ThrowOnMissingTenantContext = false };
        var builder = new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new OracleActiveOrdersSpec();

        // Act
        var (whereClause, addParameters) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("TenantId");

        // Verify parameters - no tenant parameter
        var mockCommand = CreateMockCommand();
        addParameters(mockCommand);
        mockCommand.Parameters.Count.ShouldBe(0);
    }

    [Fact]
    public void BuildWhereClause_NonTenantEntity_NoFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var builder = new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_nonTenantMapping, _tenantProvider, _options);
        var spec = new OracleActiveOrdersSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("TenantId");
    }

    [Fact]
    public void BuildWhereClause_AutoFilterDisabled_NoFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var options = new ADOTenancyOptions { AutoFilterTenantQueries = false };
        var builder = new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, _tenantProvider, options);
        var spec = new OracleActiveOrdersSpec();

        // Act
        var (whereClause, _) = builder.BuildWhereClause(spec);

        // Assert
        whereClause.ShouldNotContain("TenantId");
    }

    #endregion

    #region BuildSelectStatement Tests

    [Fact]
    public void BuildSelectStatement_WithTenantContext_IncludesTenantFilter()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-abc");
        var builder = new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, _tenantProvider, _options);

        // Act
        var (sql, addParameters) = builder.BuildSelectStatement("Orders");

        // Assert - Oracle uses double quotes for identifiers
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM \"Orders\"");
        sql.ShouldContain("\"TenantId\" = :TenantId");

        // Verify parameters
        var mockCommand = CreateMockCommand();
        addParameters(mockCommand);
        mockCommand.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    public void BuildSelectStatement_WithSpecification_CombinesFilters()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-xyz");
        var builder = new TenantAwareSpecificationSqlBuilder<OracleTenantTestOrder>(_tenantMapping, _tenantProvider, _options);
        var spec = new OracleMinTotalSpec(100m);

        // Act
        var (sql, addParameters) = builder.BuildSelectStatement("Orders", spec);

        // Assert - Oracle uses colon for parameters (:p0 instead of @p0)
        sql.ShouldContain("\"TenantId\" = :TenantId");
        sql.ShouldContain("\"Total\" >");

        // Verify parameters
        var mockCommand = CreateMockCommand();
        addParameters(mockCommand);
        mockCommand.Parameters.Count.ShouldBe(2); // TenantId + p0
    }

    #endregion

    private static IDbCommand CreateMockCommand()
    {
        var command = Substitute.For<IDbCommand>();
        var parameters = new OracleTestParameterCollection();
        command.Parameters.Returns(parameters);
        command.CreateParameter().Returns(_ =>
        {
            var param = Substitute.For<IDbDataParameter>();
            param.ParameterName.Returns(string.Empty);
            param.Value.Returns(DBNull.Value);
            return param;
        });
        return command;
    }
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

/// <summary>
/// Simple test parameter collection for Oracle tests.
/// </summary>
internal sealed class OracleTestParameterCollection : List<object>, IDataParameterCollection
{
    public bool Contains(string parameterName) => false;
    public int IndexOf(string parameterName) => -1;
    public void RemoveAt(string parameterName) { }
    public object this[string parameterName]
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
    public object SyncRoot => this;
    public bool IsSynchronized => false;
}

#region Test Specifications

/// <summary>
/// Specification that filters for active Oracle tenant orders.
/// </summary>
internal sealed class OracleActiveOrdersSpec : Specification<OracleTenantTestOrder>
{
    public override Expression<Func<OracleTenantTestOrder, bool>> ToExpression()
        => o => o.IsActive;
}

/// <summary>
/// Specification that filters for orders with minimum total.
/// </summary>
internal sealed class OracleMinTotalSpec : Specification<OracleTenantTestOrder>
{
    private readonly decimal _minTotal;

    public OracleMinTotalSpec(decimal minTotal) => _minTotal = minTotal;

    public override Expression<Func<OracleTenantTestOrder, bool>> ToExpression()
        => o => o.Total > _minTotal;
}

#endregion
