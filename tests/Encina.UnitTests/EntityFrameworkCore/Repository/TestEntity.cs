using System.Linq.Expressions;
using Encina.DomainModeling;

namespace Encina.UnitTests.EntityFrameworkCore.Repository;

/// <summary>
/// Test entity for repository tests.
/// </summary>
public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active test entities.
/// </summary>
public class ActiveEntitySpec : Specification<TestEntity>
{
    public override Expression<Func<TestEntity, bool>> ToExpression()
        => e => e.IsActive;
}

/// <summary>
/// Specification for entities with minimum amount.
/// </summary>
public class MinAmountSpec : Specification<TestEntity>
{
    private readonly decimal _minAmount;

    public MinAmountSpec(decimal minAmount)
    {
        _minAmount = minAmount;
    }

    public override Expression<Func<TestEntity, bool>> ToExpression()
        => e => e.Amount >= _minAmount;
}

/// <summary>
/// Specification for entities by name.
/// </summary>
public class NameContainsSpec : Specification<TestEntity>
{
    private readonly string _searchTerm;

    public NameContainsSpec(string searchTerm)
    {
        _searchTerm = searchTerm;
    }

    public override Expression<Func<TestEntity, bool>> ToExpression()
        => e => e.Name.Contains(_searchTerm);
}

/// <summary>
/// Test entity that implements IHasId for testing GetEntityIdDescription helper.
/// </summary>
public class TestEntityWithHasId : IHasId<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
