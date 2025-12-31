using System.Linq.Expressions;
using System.Reflection;
using Encina.DomainModeling;
using Shouldly;

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
        _specType.IsAbstract.ShouldBeTrue();
    }

    [Fact]
    public void Specification_MustHaveToExpressionMethod()
    {
        var method = _specType.GetMethod("ToExpression");
        method.ShouldNotBeNull();
        method!.IsAbstract.ShouldBeTrue();
    }

    [Fact]
    public void Specification_MustHaveIsSatisfiedByMethod()
    {
        var method = _specType.GetMethod("IsSatisfiedBy");
        method.ShouldNotBeNull();
        // IsSatisfiedBy is a concrete method, not virtual
    }

    [Fact]
    public void Specification_MustHaveAndMethod()
    {
        var method = _specType.GetMethod("And");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void Specification_MustHaveOrMethod()
    {
        var method = _specType.GetMethod("Or");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void Specification_MustHaveNotMethod()
    {
        var method = _specType.GetMethod("Not");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void Specification_MustHaveImplicitConversionToExpression()
    {
        var implicitOp = _specType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Implicit");
        implicitOp.ShouldNotBeNull("implicit conversion to Expression should exist");
    }

    [Fact]
    public void Specification_MustHaveToFuncMethod()
    {
        var method = _specType.GetMethod("ToFunc");
        method.ShouldNotBeNull();
    }

    #endregion

    #region QuerySpecification<T> Contracts

    [Fact]
    public void QuerySpecification_MustExtendSpecification()
    {
        _querySpecType.BaseType.ShouldNotBeNull();
        _querySpecType.BaseType!.GetGenericTypeDefinition().ShouldBe(_specType);
    }

    [Fact]
    public void QuerySpecification_MustHaveCriteriaProperty()
    {
        // Criteria is protected, so we need NonPublic binding
        var property = _querySpecType.GetProperty("Criteria", BindingFlags.Instance | BindingFlags.NonPublic);
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveIncludesProperty()
    {
        var property = _querySpecType.GetProperty("Includes");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveIncludeStringsProperty()
    {
        var property = _querySpecType.GetProperty("IncludeStrings");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveOrderByProperty()
    {
        var property = _querySpecType.GetProperty("OrderBy");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHaveOrderByDescendingProperty()
    {
        var property = _querySpecType.GetProperty("OrderByDescending");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void QuerySpecification_MustHavePagingProperties()
    {
        var skipProp = _querySpecType.GetProperty("Skip");
        skipProp.ShouldNotBeNull();

        var takeProp = _querySpecType.GetProperty("Take");
        takeProp.ShouldNotBeNull();

        var isPagingProp = _querySpecType.GetProperty("IsPagingEnabled");
        isPagingProp.ShouldNotBeNull();
    }

    [Fact]
    public void QuerySpecification_MustHaveTrackingOptions()
    {
        var noTrackingProp = _querySpecType.GetProperty("AsNoTracking");
        noTrackingProp.ShouldNotBeNull();

        var splitQueryProp = _querySpecType.GetProperty("AsSplitQuery");
        splitQueryProp.ShouldNotBeNull();
    }

    [Fact]
    public void QuerySpecification_MustHaveProtectedAddIncludeMethod()
    {
        var methods = _querySpecType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.ShouldContain(m => m.Name == "AddInclude" && m.IsFamily);
    }

    [Fact]
    public void QuerySpecification_MustHaveProtectedApplyPagingMethod()
    {
        var methods = _querySpecType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.ShouldContain(m => m.Name == "ApplyPaging" && m.IsFamily);
    }

    [Fact]
    public void QuerySpecification_MustHaveProtectedOrderingMethods()
    {
        var methods = _querySpecType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.ShouldContain(m => m.Name == "ApplyOrderBy" && m.IsFamily);
        methods.ShouldContain(m => m.Name == "ApplyOrderByDescending" && m.IsFamily);
    }

    #endregion

    #region QuerySpecification<T, TResult> Contracts

    [Fact]
    public void QuerySpecificationWithResult_MustExtendQuerySpecification()
    {
        _querySpecWithResultType.BaseType.ShouldNotBeNull();
        _querySpecWithResultType.BaseType!.GetGenericTypeDefinition().ShouldBe(_querySpecType);
    }

    [Fact]
    public void QuerySpecificationWithResult_MustHaveSelectorProperty()
    {
        // Selector has public getter, protected setter
        var property = _querySpecWithResultType.GetProperty("Selector");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
        // Setter is protected, but property exists with public getter
    }

    #endregion
}
