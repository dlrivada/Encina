using System.Linq.Expressions;
using Encina.Dapper.SqlServer.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.SqlServer.Tenancy;

/// <summary>
/// Guard clause tests for <see cref="TenantAwareSpecificationSqlBuilder{TEntity}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Dapper.SqlServer")]
public sealed class TenantAwareSpecificationSqlBuilderGuardTests
{
    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var mapping = CreateMapping();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = new DapperTenancyOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<TenantBuilderGuardOrder>(null!, tenantProvider, options))
            .ParamName.ShouldBe("mapping");
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<TenantBuilderGuardOrder>(mapping, null!, options))
            .ParamName.ShouldBe("tenantProvider");
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareSpecificationSqlBuilder<TenantBuilderGuardOrder>(mapping, tenantProvider, null!))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void BuildWhereClause_NullSpecificationOrOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((Specification<TenantBuilderGuardOrder>)null!, "List"))
            .ParamName.ShouldBe("specification");
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause((QuerySpecification<TenantBuilderGuardOrder>)null!, "List"))
            .ParamName.ShouldBe("specification");
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause(new GuardActiveSpec(), null!))
            .ParamName.ShouldBe("operation");
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildWhereClause(new GuardQuerySpec(), null!))
            .ParamName.ShouldBe("operation");
    }

    [Fact]
    public void BuildSelectStatement_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.BuildSelectStatement("Orders", null!))
            .ParamName.ShouldBe("operation");
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("Orders", (Specification<TenantBuilderGuardOrder>)null!, "List"))
            .ParamName.ShouldBe("specification");
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("Orders", new GuardActiveSpec(), null!))
            .ParamName.ShouldBe("operation");
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("Orders", (QuerySpecification<TenantBuilderGuardOrder>)null!, "List"))
            .ParamName.ShouldBe("specification");
        Should.Throw<ArgumentNullException>(() =>
            builder.BuildSelectStatement("Orders", new GuardQuerySpec(), null!))
            .ParamName.ShouldBe("operation");
    }

    private static TenantAwareSpecificationSqlBuilder<TenantBuilderGuardOrder> CreateBuilder()
        => new(CreateMapping(), Substitute.For<ITenantProvider>(), new DapperTenancyOptions());

    private static ITenantEntityMapping<TenantBuilderGuardOrder, object> CreateMapping()
    {
        var mapping = Substitute.For<ITenantEntityMapping<TenantBuilderGuardOrder, object>>();
        mapping.TableName.Returns("Orders");
        mapping.IdColumnName.Returns("Id");
        mapping.ColumnMappings.Returns(new Dictionary<string, string> { ["Id"] = "Id", ["TenantId"] = "TenantId" });
        mapping.IsTenantEntity.Returns(true);
        mapping.TenantColumnName.Returns("TenantId");
        return mapping;
    }

    private sealed class GuardActiveSpec : Specification<TenantBuilderGuardOrder>
    {
        public override Expression<Func<TenantBuilderGuardOrder, bool>> ToExpression() => o => o.Id != Guid.Empty;
    }

    private sealed class GuardQuerySpec : QuerySpecification<TenantBuilderGuardOrder>
    {
        public GuardQuerySpec() => AddCriteria(o => o.Id != Guid.Empty);
    }
}

/// <summary>
/// Entity used by <see cref="TenantAwareSpecificationSqlBuilderGuardTests"/>.
/// </summary>
public sealed class TenantBuilderGuardOrder : ITenantEntity
{
    public Guid Id { get; set; }

    public string TenantId { get; set; } = string.Empty;
}
