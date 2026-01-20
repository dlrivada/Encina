using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationFilterBuilder{TEntity}"/>.
/// These tests verify that filter definitions are created correctly.
/// Deep validation of filter behavior is done in integration tests.
/// </summary>
[Trait("Category", "Unit")]
public class SpecificationFilterBuilderTests
{
    private readonly SpecificationFilterBuilder<TestDocument> _builder = new();

    #region Equality Tests

    [Fact]
    public void BuildFilter_EqualityComparison_ReturnsNonNullFilter()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var spec = new CustomerDocumentsSpec(customerId);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_NotEqualComparison_ReturnsNonNullFilter()
    {
        // Arrange
        var status = "Inactive";
        var spec = new NotStatusSpec(status);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BuildFilter_GreaterThan_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new AmountGreaterThanSpec(100m);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_GreaterThanOrEqual_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new MinAmountSpec(100m);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_LessThan_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new AmountLessThanSpec(500m);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_LessThanOrEqual_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new MaxAmountSpec(1000m);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion

    #region Boolean Tests

    [Fact]
    public void BuildFilter_BooleanProperty_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion

    #region Null Check Tests

    [Fact]
    public void BuildFilter_NullCheck_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new NullDescriptionSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_NotNullCheck_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new HasDescriptionSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion

    #region Logical Operator Tests

    // Note: MongoDB SpecificationFilterBuilder does not support specification composition
    // (And, Or, Not) because these create InvocationExpression that the builder cannot translate.
    // Complex queries should use MongoDB-specific filter builders directly.

    [Fact]
    public void BuildFilter_AndCombination_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec().And(new MinAmountSpec(100m));

        // Act & Assert - Combined specifications create Invoke expressions which are not supported
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_OrCombination_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec().Or(new MinAmountSpec(1000m));

        // Act & Assert - Combined specifications create Invoke expressions which are not supported
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_NotOperator_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec().Not();

        // Act & Assert - Combined specifications create Invoke expressions which are not supported
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    #endregion

    #region String Method Tests

    [Fact]
    public void BuildFilter_StringContains_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new DescriptionContainsSpec("urgent");

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_StringStartsWith_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new DescriptionStartsWithSpec("Priority:");

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_StringEndsWith_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new DescriptionEndsWithSpec("completed");

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion

    #region BuildFilterOrEmpty Tests

    [Fact]
    public void BuildFilterOrEmpty_WithSpecification_ReturnsDifferentFromEmpty()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec();

        // Act
        var filter = _builder.BuildFilterOrEmpty(spec);

        // Assert
        filter.ShouldNotBeNull();
        // The filter should be different from an empty filter
        filter.ShouldNotBe(Builders<TestDocument>.Filter.Empty);
    }

    [Fact]
    public void BuildFilterOrEmpty_WithNull_ReturnsEmptyFilter()
    {
        // Act
        var filter = _builder.BuildFilterOrEmpty(null);

        // Assert
        filter.ShouldNotBeNull();
        // When spec is null, should return empty filter
        filter.ShouldBe(Builders<TestDocument>.Filter.Empty);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void BuildFilter_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _builder.BuildFilter(null!));
    }

    [Fact]
    public void BuildFilter_SpecialCharactersInStringSearch_DoesNotThrow()
    {
        // Arrange - using regex special characters
        var spec = new DescriptionContainsSpec("test.*value");

        // Act & Assert - Should not throw, special characters should be escaped
        var filter = _builder.BuildFilter(spec);
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_ComplexNestedExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new ActiveDocumentsSpec()
            .And(new MinAmountSpec(100m).Or(new MaxAmountSpec(50m)))
            .Or(new HasDescriptionSpec());

        // Act & Assert - Complex combinations create Invoke expressions which are not supported
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_BooleanConstantTrue_ReturnsFilter()
    {
        // Arrange - specification that uses a boolean constant expression
        var spec = new TrueConstantSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_BooleanConstantFalse_ReturnsFilter()
    {
        // Arrange
        var spec = new FalseConstantSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_NotBooleanProperty_ReturnsNonNullFilter()
    {
        // Arrange - specification with NOT operator on boolean property directly
        var spec = new InactiveDocumentsSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_ConvertExpression_ReturnsNonNullFilter()
    {
        // Arrange - specification that uses a nullable comparison (generates Convert expression)
        var spec = new NullableAmountSpec(100m);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_AndAlsoWithTwoComparisons_ReturnsNonNullFilter()
    {
        // Arrange - specification using && in expression body directly
        var spec = new AmountRangeSpec(50m, 200m);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_OrElseWithTwoComparisons_ReturnsNonNullFilter()
    {
        // Arrange - specification using || in expression body directly
        var spec = new AmountOutsideRangeSpec(50m, 200m);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_ListContains_ThrowsNotSupportedException()
    {
        // Arrange - specification using List<T>.Contains (not supported, different from Enumerable.Contains)
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var spec = new CustomerIdsInListSpec(ids);

        // Act & Assert - List<T>.Contains is not the same as System.Linq.Enumerable.Contains
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_UnsupportedStringMethod_ThrowsNotSupportedException()
    {
        // Arrange - specification with unsupported string method
        var spec = new UnsupportedStringMethodSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_UnsupportedMethodCall_ThrowsNotSupportedException()
    {
        // Arrange - specification with unsupported method call (Object.Equals)
        var spec = new UnsupportedMethodCallSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_ValueOnLeftSideOfComparison_ThrowsException()
    {
        // Arrange - specification with value on left side of comparison
        // This is not supported by the current implementation
        var spec = new ReversedComparisonSpec(100m);

        // Act & Assert - The builder cannot handle member access when value is on left
        Should.Throw<InvalidOperationException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_NestedPropertyAccess_ReturnsNonNullFilter()
    {
        // Arrange
        var builder = new SpecificationFilterBuilder<TestDocumentWithNestedProperty>();
        var spec = new NestedPropertySpec("test");

        // Act
        var filter = builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        // Arrange - specification using XOR which is not supported
        var spec = new UnsupportedBinaryOperatorSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_UnsupportedUnaryOperator_ThrowsNotSupportedException()
    {
        // Arrange - specification using unsupported unary operator
        var spec = new UnsupportedUnaryOperatorSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_UnsupportedExpressionType_ThrowsNotSupportedException()
    {
        // Arrange - specification with unsupported expression type
        var spec = new UnsupportedExpressionTypeSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    [Fact]
    public void BuildFilter_NewExpressionValue_ReturnsNonNullFilter()
    {
        // Arrange - specification with new expression as value
        var spec = new NewExpressionValueSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_MethodCallValue_ReturnsNonNullFilter()
    {
        // Arrange - specification with method call as value
        var spec = new MethodCallValueSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_PropertyMemberValue_ReturnsNonNullFilter()
    {
        // Arrange - specification where value comes from property member
        var spec = new PropertyMemberValueSpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_UnsupportedMemberType_ThrowsNotSupportedException()
    {
        // Arrange - specification accessing an unsupported member type
        var spec = new UnsupportedMemberTypeSpec();

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _builder.BuildFilter(spec));
    }

    #endregion

    #region BuildSortDefinition Tests

    [Fact]
    public void BuildSortDefinition_WithOrderBy_ReturnsSortDefinition()
    {
        // Arrange
        var spec = new OrderByAmountQuerySpec();

        // Act
        var sort = _builder.BuildSortDefinition(spec);

        // Assert
        sort.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSortDefinition_WithOrderByDescending_ReturnsSortDefinition()
    {
        // Arrange
        var spec = new OrderByAmountDescQuerySpec();

        // Act
        var sort = _builder.BuildSortDefinition(spec);

        // Assert
        sort.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSortDefinition_WithThenBy_ReturnsCombinedSortDefinition()
    {
        // Arrange
        var spec = new OrderByStatusThenByAmountQuerySpec();

        // Act
        var sort = _builder.BuildSortDefinition(spec);

        // Assert
        sort.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSortDefinition_WithThenByDescending_ReturnsCombinedSortDefinition()
    {
        // Arrange
        var spec = new OrderByStatusThenByAmountDescQuerySpec();

        // Act
        var sort = _builder.BuildSortDefinition(spec);

        // Assert
        sort.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSortDefinition_WithComplexOrdering_ReturnsCombinedSortDefinition()
    {
        // Arrange
        var spec = new ComplexOrderingMongoQuerySpec();

        // Act
        var sort = _builder.BuildSortDefinition(spec);

        // Assert
        sort.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSortDefinition_WithNoOrdering_ReturnsNull()
    {
        // Arrange
        var spec = new NoOrderingMongoQuerySpec();

        // Act
        var sort = _builder.BuildSortDefinition(spec);

        // Assert
        sort.ShouldBeNull();
    }

    [Fact]
    public void BuildSortDefinition_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _builder.BuildSortDefinition(null!));
    }

    #endregion

    #region BuildKeysetFilter Tests

    [Fact]
    public void BuildKeysetFilter_ReturnsFilterDefinition()
    {
        // Arrange
        var lastId = Guid.NewGuid();

        // Act
        var filter = _builder.BuildKeysetFilter(d => d.Id, lastId);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildKeysetFilter_WithDecimalKey_ReturnsFilterDefinition()
    {
        // Arrange
        var lastAmount = 100m;

        // Act
        var filter = _builder.BuildKeysetFilter(d => d.Amount, lastAmount);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildKeysetFilter_WithDateTimeKey_ReturnsFilterDefinition()
    {
        // Arrange
        var lastDate = DateTime.UtcNow;

        // Act
        var filter = _builder.BuildKeysetFilter(d => d.CreatedAtUtc, lastDate);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion

    #region QuerySpecification with BuildFilter Tests

    [Fact]
    public void BuildFilter_WithQuerySpecification_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new ActiveMongoQuerySpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_WithKeysetPaginationQuerySpecification_ReturnsNonNullFilter()
    {
        // Arrange
        var lastId = Guid.NewGuid();
        var spec = new KeysetPaginatedMongoQuerySpec(lastId);

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    [Fact]
    public void BuildFilter_WithKeysetPaginationNoLastKey_ReturnsNonNullFilter()
    {
        // Arrange
        var spec = new KeysetPaginatedNoLastKeyMongoQuerySpec();

        // Act
        var filter = _builder.BuildFilter(spec);

        // Assert
        filter.ShouldNotBeNull();
    }

    #endregion
}

#region Test QuerySpecifications for MongoDB

/// <summary>
/// QuerySpecification with OrderBy ascending.
/// </summary>
public class OrderByAmountQuerySpec : QuerySpecification<TestDocument>
{
    public OrderByAmountQuerySpec()
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderBy(d => d.Amount);
    }
}

/// <summary>
/// QuerySpecification with OrderByDescending.
/// </summary>
public class OrderByAmountDescQuerySpec : QuerySpecification<TestDocument>
{
    public OrderByAmountDescQuerySpec()
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderByDescending(d => d.Amount);
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenBy.
/// </summary>
public class OrderByStatusThenByAmountQuerySpec : QuerySpecification<TestDocument>
{
    public OrderByStatusThenByAmountQuerySpec()
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderBy(d => d.Status);
        ApplyThenBy(d => d.Amount);
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenByDescending.
/// </summary>
public class OrderByStatusThenByAmountDescQuerySpec : QuerySpecification<TestDocument>
{
    public OrderByStatusThenByAmountDescQuerySpec()
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderBy(d => d.Status);
        ApplyThenByDescending(d => d.Amount);
    }
}

/// <summary>
/// QuerySpecification with complex multi-column ordering.
/// </summary>
public class ComplexOrderingMongoQuerySpec : QuerySpecification<TestDocument>
{
    public ComplexOrderingMongoQuerySpec()
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderByDescending(d => d.Status);
        ApplyThenBy(d => d.Amount);
        ApplyThenByDescending(d => d.CreatedAtUtc);
    }
}

/// <summary>
/// QuerySpecification without any ordering.
/// </summary>
public class NoOrderingMongoQuerySpec : QuerySpecification<TestDocument>
{
    public NoOrderingMongoQuerySpec()
    {
        AddCriteria(d => d.IsActive);
    }
}

/// <summary>
/// QuerySpecification for active documents.
/// </summary>
public class ActiveMongoQuerySpec : QuerySpecification<TestDocument>
{
    public ActiveMongoQuerySpec()
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderBy(d => d.Id);
        ApplyPaging(0, 10);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination.
/// </summary>
public class KeysetPaginatedMongoQuerySpec : QuerySpecification<TestDocument>
{
    public KeysetPaginatedMongoQuerySpec(Guid lastId)
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderBy(d => d.Id);
        ApplyKeysetPagination(d => d.Id, lastId, 10);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination but no last key value.
/// </summary>
public class KeysetPaginatedNoLastKeyMongoQuerySpec : QuerySpecification<TestDocument>
{
    public KeysetPaginatedNoLastKeyMongoQuerySpec()
    {
        AddCriteria(d => d.IsActive);
        ApplyOrderBy(d => d.Id);
        ApplyKeysetPagination(d => d.Id, null, 10);
    }
}

#endregion

#region Test Entity

/// <summary>
/// Test document for MongoDB repository tests.
/// </summary>
public class TestDocument
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

#endregion

#region Test Specifications

public class CustomerDocumentsSpec : Specification<TestDocument>
{
    private readonly Guid _customerId;
    public CustomerDocumentsSpec(Guid customerId) => _customerId = customerId;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.CustomerId == _customerId;
}

public class NotStatusSpec : Specification<TestDocument>
{
    private readonly string _status;
    public NotStatusSpec(string status) => _status = status;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Status != _status;
}

public class MinAmountSpec : Specification<TestDocument>
{
    private readonly decimal _minAmount;
    public MinAmountSpec(decimal minAmount) => _minAmount = minAmount;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount >= _minAmount;
}

public class MaxAmountSpec : Specification<TestDocument>
{
    private readonly decimal _maxAmount;
    public MaxAmountSpec(decimal maxAmount) => _maxAmount = maxAmount;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount <= _maxAmount;
}

public class AmountGreaterThanSpec : Specification<TestDocument>
{
    private readonly decimal _amount;
    public AmountGreaterThanSpec(decimal amount) => _amount = amount;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount > _amount;
}

public class AmountLessThanSpec : Specification<TestDocument>
{
    private readonly decimal _amount;
    public AmountLessThanSpec(decimal amount) => _amount = amount;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount < _amount;
}

public class ActiveDocumentsSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.IsActive;
}

public class NullDescriptionSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Description == null;
}

public class HasDescriptionSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Description != null;
}

public class DescriptionContainsSpec : Specification<TestDocument>
{
    private readonly string _searchTerm;
    public DescriptionContainsSpec(string searchTerm) => _searchTerm = searchTerm;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Description != null && d.Description.Contains(_searchTerm);
}

public class DescriptionStartsWithSpec : Specification<TestDocument>
{
    private readonly string _prefix;
    public DescriptionStartsWithSpec(string prefix) => _prefix = prefix;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Description != null && d.Description.StartsWith(_prefix);
}

public class DescriptionEndsWithSpec : Specification<TestDocument>
{
    private readonly string _suffix;
    public DescriptionEndsWithSpec(string suffix) => _suffix = suffix;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Description != null && d.Description.EndsWith(_suffix);
}

/// <summary>
/// Specification that causes NotSupportedException when translated to MongoDB filter.
/// Uses a method call that SpecificationFilterBuilder doesn't support.
/// </summary>
public class UnsupportedMongoSpec : Specification<TestDocument>
{
    private readonly List<Guid> _validIds = [Guid.NewGuid()];

    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => _validIds.Contains(d.Id);
}

/// <summary>
/// Specification that always returns true (boolean constant expression).
/// </summary>
public class TrueConstantSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => true;
}

/// <summary>
/// Specification that always returns false (boolean constant expression).
/// </summary>
public class FalseConstantSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => false;
}

/// <summary>
/// Specification for inactive documents (uses NOT operator).
/// </summary>
public class InactiveDocumentsSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => !d.IsActive;
}

/// <summary>
/// Specification with nullable decimal comparison (generates Convert expression).
/// </summary>
public class NullableAmountSpec : Specification<TestDocument>
{
    private readonly decimal? _amount;
    public NullableAmountSpec(decimal? amount) => _amount = amount;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount == _amount;
}

/// <summary>
/// Specification for amount within range using && (AndAlso).
/// </summary>
public class AmountRangeSpec : Specification<TestDocument>
{
    private readonly decimal _min;
    private readonly decimal _max;
    public AmountRangeSpec(decimal min, decimal max) { _min = min; _max = max; }
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount >= _min && d.Amount <= _max;
}

/// <summary>
/// Specification for amount outside range using || (OrElse).
/// </summary>
public class AmountOutsideRangeSpec : Specification<TestDocument>
{
    private readonly decimal _min;
    private readonly decimal _max;
    public AmountOutsideRangeSpec(decimal min, decimal max) { _min = min; _max = max; }
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount < _min || d.Amount > _max;
}

/// <summary>
/// Specification with customer IDs in a list (Enumerable.Contains).
/// </summary>
public class CustomerIdsInListSpec : Specification<TestDocument>
{
    private readonly List<Guid> _customerIds;
    public CustomerIdsInListSpec(List<Guid> customerIds) => _customerIds = customerIds;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => _customerIds.Contains(d.CustomerId);
}

/// <summary>
/// Specification using unsupported string method (Trim).
/// </summary>
public class UnsupportedStringMethodSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Status.Trim() == "Active";
}

/// <summary>
/// Specification using unsupported method call (custom method).
/// </summary>
public class UnsupportedMethodCallSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount.Equals(100m);
}

/// <summary>
/// Specification with value on left side of comparison.
/// </summary>
public class ReversedComparisonSpec : Specification<TestDocument>
{
    private readonly decimal _amount;
    public ReversedComparisonSpec(decimal amount) => _amount = amount;
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => _amount == d.Amount;
}

/// <summary>
/// Test document with nested property for testing nested property access.
/// </summary>
public class TestDocumentWithNestedProperty
{
    public Guid Id { get; set; }
    public NestedInfo Info { get; set; } = new();
}

/// <summary>
/// Nested property class for testing.
/// </summary>
public class NestedInfo
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Specification for nested property access.
/// </summary>
public class NestedPropertySpec : Specification<TestDocumentWithNestedProperty>
{
    private readonly string _name;
    public NestedPropertySpec(string name) => _name = name;
    public override Expression<Func<TestDocumentWithNestedProperty, bool>> ToExpression()
        => d => d.Info.Name == _name;
}

/// <summary>
/// Specification using unsupported binary operator (XOR).
/// </summary>
public class UnsupportedBinaryOperatorSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
    {
        var param = Expression.Parameter(typeof(TestDocument), "d");
        var isActive = Expression.Property(param, nameof(TestDocument.IsActive));
        var xor = Expression.ExclusiveOr(isActive, Expression.Constant(true));
        return Expression.Lambda<Func<TestDocument, bool>>(xor, param);
    }
}

/// <summary>
/// Specification using unsupported unary operator.
/// </summary>
public class UnsupportedUnaryOperatorSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
    {
        var param = Expression.Parameter(typeof(TestDocument), "d");
        var amount = Expression.Property(param, nameof(TestDocument.Amount));
        // UnaryPlus is not supported
        var unaryPlus = Expression.UnaryPlus(amount);
        var comparison = Expression.GreaterThan(unaryPlus, Expression.Constant(0m));
        return Expression.Lambda<Func<TestDocument, bool>>(comparison, param);
    }
}

/// <summary>
/// Specification with unsupported expression type at root level.
/// </summary>
public class UnsupportedExpressionTypeSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
    {
        var param = Expression.Parameter(typeof(TestDocument), "d");
        // TypeIs expression is not supported at root level
        var typeIs = Expression.TypeIs(param, typeof(TestDocument));
        return Expression.Lambda<Func<TestDocument, bool>>(typeIs, param);
    }
}

/// <summary>
/// Specification with new expression as value.
/// </summary>
public class NewExpressionValueSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.CreatedAtUtc > new DateTime(2020, 1, 1);
}

/// <summary>
/// Specification with method call as value.
/// </summary>
public class MethodCallValueSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.CreatedAtUtc > DateTime.UtcNow.AddDays(-30);
}

/// <summary>
/// Helper class for property value tests.
/// </summary>
public static class TestValues
{
    public static decimal MinAmount { get; } = 50m;
}

/// <summary>
/// Specification where value comes from property member.
/// </summary>
public class PropertyMemberValueSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
        => d => d.Amount >= TestValues.MinAmount;
}

/// <summary>
/// Helper class for unsupported member type test.
/// </summary>
public class UnsupportedMemberHelper
{
#pragma warning disable CA1822 // Mark members as static - intentionally instance member for test
    public bool CheckDocument(TestDocument d) => d.IsActive;
#pragma warning restore CA1822
}

/// <summary>
/// Specification accessing an unsupported member type.
/// </summary>
public class UnsupportedMemberTypeSpec : Specification<TestDocument>
{
    public override Expression<Func<TestDocument, bool>> ToExpression()
    {
        var param = Expression.Parameter(typeof(TestDocument), "d");
        var helper = new UnsupportedMemberHelper();

        // Create a method call expression that will fail to extract member value
        var method = typeof(UnsupportedMemberHelper).GetMethod(nameof(UnsupportedMemberHelper.CheckDocument))!;
        var helperConst = Expression.Constant(helper);
        var call = Expression.Call(helperConst, method, param);

        return Expression.Lambda<Func<TestDocument, bool>>(call, param);
    }
}

#endregion
