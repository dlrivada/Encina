using System.Linq.Expressions;
using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying Specification public API contract.
/// </summary>
public sealed class SpecificationContracts
{
    private readonly Type _specType = typeof(Specification<>);
    private readonly Type _querySpecType = typeof(QuerySpecification<>);
    private readonly Type _querySpecWithResultType = typeof(QuerySpecification<,>);

    #region Specification<T> Contracts

    [Fact]
    public void Specification_MustBeAbstract()
    {
        _specType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void Specification_MustHaveToExpressionMethod()
    {
        var method = _specType.GetMethod("ToExpression");
        method.Should().NotBeNull();
        method!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void Specification_MustHaveIsSatisfiedByMethod()
    {
        var method = _specType.GetMethod("IsSatisfiedBy");
        method.Should().NotBeNull();
        // IsSatisfiedBy is a concrete method, not virtual
    }

    [Fact]
    public void Specification_MustHaveAndMethod()
    {
        var method = _specType.GetMethod("And");
        method.Should().NotBeNull();
    }

    [Fact]
    public void Specification_MustHaveOrMethod()
    {
        var method = _specType.GetMethod("Or");
        method.Should().NotBeNull();
    }

    [Fact]
    public void Specification_MustHaveNotMethod()
    {
        var method = _specType.GetMethod("Not");
        method.Should().NotBeNull();
    }

    [Fact]
    public void Specification_MustHaveImplicitConversionToExpression()
    {
        var implicitOp = _specType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Implicit");
        implicitOp.Should().NotBeNull("implicit conversion to Expression should exist");
    }

    [Fact]
    public void Specification_MustHaveToFuncMethod()
    {
        var method = _specType.GetMethod("ToFunc");
        method.Should().NotBeNull();
    }

    #endregion

    #region QuerySpecification<T> Contracts

    [Fact]
    public void QuerySpecification_MustExtendSpecification()
    {
        _querySpecType.BaseType.Should().NotBeNull();
        _querySpecType.BaseType!.GetGenericTypeDefinition().Should().Be(_specType);
    }

    [Fact]
    public void QuerySpecification_MustHaveCriteriaProperty()
    {
        // Criteria is protected, so we need NonPublic binding
        var property = _querySpecType.GetProperty("Criteria", BindingFlags.Instance | BindingFlags.NonPublic);
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveIncludesProperty()
    {
        var property = _querySpecType.GetProperty("Includes");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveIncludeStringsProperty()
    {
        var property = _querySpecType.GetProperty("IncludeStrings");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveOrderByProperty()
    {
        var property = _querySpecType.GetProperty("OrderBy");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveOrderByDescendingProperty()
    {
        var property = _querySpecType.GetProperty("OrderByDescending");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHavePagingProperties()
    {
        var skipProp = _querySpecType.GetProperty("Skip");
        skipProp.Should().NotBeNull();

        var takeProp = _querySpecType.GetProperty("Take");
        takeProp.Should().NotBeNull();

        var isPagingProp = _querySpecType.GetProperty("IsPagingEnabled");
        isPagingProp.Should().NotBeNull();
    }

    [Fact]
    public void QuerySpecification_MustHaveTrackingOptions()
    {
        var noTrackingProp = _querySpecType.GetProperty("AsNoTracking");
        noTrackingProp.Should().NotBeNull();

        var splitQueryProp = _querySpecType.GetProperty("AsSplitQuery");
        splitQueryProp.Should().NotBeNull();
    }

    [Fact]
    public void QuerySpecification_MustHaveProtectedAddIncludeMethod()
    {
        var methods = _querySpecType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.Should().Contain(m => m.Name == "AddInclude" && m.IsFamily);
    }

    [Fact]
    public void QuerySpecification_MustHaveProtectedApplyPagingMethod()
    {
        var methods = _querySpecType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.Should().Contain(m => m.Name == "ApplyPaging" && m.IsFamily);
    }

    [Fact]
    public void QuerySpecification_MustHaveProtectedOrderingMethods()
    {
        var methods = _querySpecType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.Should().Contain(m => m.Name == "ApplyOrderBy" && m.IsFamily);
        methods.Should().Contain(m => m.Name == "ApplyOrderByDescending" && m.IsFamily);
    }

    #endregion

    #region QuerySpecification<T, TResult> Contracts

    [Fact]
    public void QuerySpecificationWithResult_MustExtendQuerySpecification()
    {
        _querySpecWithResultType.BaseType.Should().NotBeNull();
        _querySpecWithResultType.BaseType!.GetGenericTypeDefinition().Should().Be(_querySpecType);
    }

    [Fact]
    public void QuerySpecificationWithResult_MustHaveSelectorProperty()
    {
        // Selector has public getter, protected setter
        var property = _querySpecWithResultType.GetProperty("Selector");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
        // Setter is protected, but property exists with public getter
    }

    #endregion
}
