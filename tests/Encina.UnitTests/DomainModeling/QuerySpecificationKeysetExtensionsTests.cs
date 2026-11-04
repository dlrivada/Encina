using System.Linq.Expressions;
using Encina.DomainModeling;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Unit tests for <see cref="QuerySpecificationKeysetExtensions"/>: the keyset filter must follow the
/// sort direction of the keyset property, which must be the primary ordering (#3154).
/// </summary>
[Trait("Category", "Unit")]
public sealed class QuerySpecificationKeysetExtensionsTests
{
    [Fact]
    public void IsKeysetDescending_WithoutKeysetPagination_IsFalse()
    {
        // Arrange
        var spec = new PageSpec().OrderedDescending(o => o.Total);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeFalse();
    }

    [Fact]
    public void IsKeysetDescending_KeysetSortedAscending_IsFalse()
    {
        // Arrange
        var spec = new PageSpec().Ordered(o => o.Total).KeysetOn(o => o.Total);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeFalse();
    }

    [Fact]
    public void IsKeysetDescending_KeysetSortedDescending_IsTrue()
    {
        // Arrange
        var spec = new PageSpec().OrderedDescending(o => o.Total).KeysetOn(o => o.Total);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeTrue();
    }

    [Fact]
    public void IsKeysetDescending_KeysetIsPrimaryWithSecondaryOrderings_FollowsThePrimaryOrdering()
    {
        // Arrange
        var spec = new PageSpec().OrderedDescending(o => o.Total).ThenOrdered(o => o.CreatedAtUtc).KeysetOn(o => o.Total);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeTrue();
    }

    [Fact]
    public void IsKeysetDescending_WithoutAnyOrdering_IsFalse()
    {
        // Arrange - no ordering keeps the ascending default
        var spec = new PageSpec().KeysetOn(o => o.Id);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeFalse();
    }

    [Fact]
    public void IsKeysetDescending_KeysetNotInTheOrdering_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new PageSpec().OrderedDescending(o => o.CreatedAtUtc).KeysetOn(o => o.Id);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => spec.IsKeysetDescending)
            .Message.ShouldContain("Keyset pagination on Id needs Id as the primary ordering");
    }

    [Fact]
    public void IsKeysetDescending_KeysetIsThenBy_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new PageSpec().OrderedDescending(o => o.CreatedAtUtc).ThenOrdered(o => o.Id).KeysetOn(o => o.Id);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => spec.IsKeysetDescending);
    }

    [Fact]
    public void IsKeysetDescending_KeysetIsThenByDescending_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new PageSpec().Ordered(o => o.CreatedAtUtc).ThenOrderedDescending(o => o.Id).KeysetOn(o => o.Id);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => spec.IsKeysetDescending);
    }

    [Fact]
    public void IsKeysetDescending_KeysetInBothThenByAndThenByDescending_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new PageSpec().Ordered(o => o.CreatedAtUtc).ThenOrdered(o => o.Id).ThenOrderedDescending(o => o.Id).KeysetOn(o => o.Id);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => spec.IsKeysetDescending);
    }

    [Fact]
    public void IsKeysetDescending_KeysetOnlyInThenByWithoutPrimaryOrdering_ThrowsNotSupportedException()
    {
        // Arrange
        var spec = new PageSpec().ThenOrderedDescending(o => o.Id).KeysetOn(o => o.Id);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => spec.IsKeysetDescending);
    }

    [Fact]
    public void IsKeysetDescending_SameMemberThroughDifferentLambdas_Matches()
    {
        // Arrange - another parameter name, and a boxed value-type member
        var spec = new PageSpec().OrderedDescending(x => x.Total).KeysetOn(order => order.Total);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeTrue();
    }

    [Fact]
    public void IsKeysetDescending_NestedMemberPath_MatchesTheWholePath()
    {
        // Arrange
        var sameNested = new PageSpec().OrderedDescending(o => o.Customer.Name).KeysetOn(o => o.Customer.Name);
        var otherName = new PageSpec().OrderedDescending(o => o.Customer.Name).KeysetOn(o => o.Name);

        // Act & Assert
        sameNested.IsKeysetDescending.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => otherName.IsKeysetDescending);
    }

    [Fact]
    public void IsKeysetDescending_KeysetIsNotAMemberAccess_IsFalse()
    {
        // Arrange
        var spec = new PageSpec().OrderedDescending(o => o.Total).KeysetOn(o => o.Total * 2);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeFalse();
    }

    [Fact]
    public void IsKeysetDescending_ThroughTheInterface_MatchesTheSpecification()
    {
        // Arrange
        IQuerySpecification<Order> spec = new PageSpec().OrderedDescending(o => o.Total).KeysetOn(o => o.Total);

        // Act & Assert
        spec.IsKeysetDescending.ShouldBeTrue();
    }
    public sealed class Order
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public Customer Customer { get; init; } = new();
    }

    public sealed class Customer
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class PageSpec : QuerySpecification<Order>
    {
        public PageSpec Ordered(Expression<Func<Order, object>> orderBy)
        {
            ApplyOrderBy(orderBy);
            return this;
        }

        public PageSpec OrderedDescending(Expression<Func<Order, object>> orderBy)
        {
            ApplyOrderByDescending(orderBy);
            return this;
        }

        public PageSpec ThenOrdered(Expression<Func<Order, object>> thenBy)
        {
            ApplyThenBy(thenBy);
            return this;
        }

        public PageSpec ThenOrderedDescending(Expression<Func<Order, object>> thenBy)
        {
            ApplyThenByDescending(thenBy);
            return this;
        }

        public PageSpec KeysetOn(Expression<Func<Order, object>> keyset)
        {
            ApplyKeysetPagination(keyset, lastKeyValue: 1, take: 10);
            return this;
        }
    }
}
