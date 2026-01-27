using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Entities;

namespace Encina.TestInfrastructure.Specifications;

/// <summary>
/// Specification for filtering active tenant entities.
/// </summary>
public sealed class ActiveTenantEntitiesSpec : Specification<TenantTestEntity>
{
    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
        => entity => entity.IsActive;
}

/// <summary>
/// Specification for filtering tenant entities by minimum amount.
/// </summary>
public sealed class MinAmountTenantEntitiesSpec : Specification<TenantTestEntity>
{
    private readonly decimal _minAmount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinAmountTenantEntitiesSpec"/> class.
    /// </summary>
    /// <param name="minAmount">The minimum amount threshold.</param>
    public MinAmountTenantEntitiesSpec(decimal minAmount)
    {
        _minAmount = minAmount;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
        => entity => entity.Amount >= _minAmount;
}

/// <summary>
/// Specification for filtering tenant entities by name pattern.
/// Tests string Contains operation across different SQL providers.
/// </summary>
public sealed class NameContainsTenantEntitiesSpec : Specification<TenantTestEntity>
{
    private readonly string _pattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameContainsTenantEntitiesSpec"/> class.
    /// </summary>
    /// <param name="pattern">The pattern to search for in the name.</param>
    public NameContainsTenantEntitiesSpec(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        _pattern = pattern;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
        => entity => entity.Name.Contains(_pattern);
}

/// <summary>
/// Specification for filtering tenant entities created within a date range.
/// Tests DateTime comparison across different SQL providers.
/// </summary>
public sealed class DateRangeTenantEntitiesSpec : Specification<TenantTestEntity>
{
    private readonly DateTime _startUtc;
    private readonly DateTime _endUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateRangeTenantEntitiesSpec"/> class.
    /// </summary>
    /// <param name="startUtc">The start of the date range (inclusive).</param>
    /// <param name="endUtc">The end of the date range (exclusive).</param>
    public DateRangeTenantEntitiesSpec(DateTime startUtc, DateTime endUtc)
    {
        _startUtc = startUtc;
        _endUtc = endUtc;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
        => entity => entity.CreatedAtUtc >= _startUtc && entity.CreatedAtUtc < _endUtc;
}

/// <summary>
/// Complex specification combining multiple conditions with AND/OR/NOT.
/// Used to test SQL generation for nested logical operations.
/// </summary>
public sealed class ComplexTenantEntitiesSpec : Specification<TenantTestEntity>
{
    private readonly decimal _minAmount;
    private readonly decimal _maxAmount;
    private readonly string _excludeNamePattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplexTenantEntitiesSpec"/> class.
    /// </summary>
    /// <param name="minAmount">Minimum amount (inclusive).</param>
    /// <param name="maxAmount">Maximum amount (inclusive).</param>
    /// <param name="excludeNamePattern">Pattern to exclude from results.</param>
    public ComplexTenantEntitiesSpec(decimal minAmount, decimal maxAmount, string excludeNamePattern)
    {
        _minAmount = minAmount;
        _maxAmount = maxAmount;
        _excludeNamePattern = excludeNamePattern;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
        => entity =>
            (entity.IsActive && entity.Amount >= _minAmount && entity.Amount <= _maxAmount)
            ||
            (entity.UpdatedAtUtc != null && !entity.Name.Contains(_excludeNamePattern));
}

/// <summary>
/// Specification testing null handling across providers.
/// </summary>
public sealed class NullDescriptionTenantEntitiesSpec : Specification<TenantTestEntity>
{
    private readonly bool _isNull;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullDescriptionTenantEntitiesSpec"/> class.
    /// </summary>
    /// <param name="isNull">If true, matches entities with null description; otherwise, non-null.</param>
    public NullDescriptionTenantEntitiesSpec(bool isNull)
    {
        _isNull = isNull;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
        => _isNull
            ? entity => entity.Description == null
            : entity => entity.Description != null;
}

/// <summary>
/// Query specification with pagination and ordering for tenant entities.
/// </summary>
public sealed class PagedTenantEntitiesSpec : QuerySpecification<TenantTestEntity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedTenantEntitiesSpec"/> class.
    /// </summary>
    /// <param name="isActive">Filter by active status, or null for all.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="orderByDescending">If true, orders by CreatedAtUtc descending.</param>
    public PagedTenantEntitiesSpec(bool? isActive, int skip, int take, bool orderByDescending = false)
    {
        if (isActive.HasValue)
        {
            AddCriteria(e => e.IsActive == isActive.Value);
        }

        if (orderByDescending)
        {
            ApplyOrderByDescending(e => e.CreatedAtUtc);
        }
        else
        {
            ApplyOrderBy(e => e.CreatedAtUtc);
        }

        // Add secondary ordering for deterministic results
        ApplyThenBy(e => e.Id);

        ApplyPaging(skip, take);
    }
}

/// <summary>
/// Query specification using keyset pagination for efficient large dataset handling.
/// </summary>
public sealed class KeysetPagedTenantEntitiesSpec : QuerySpecification<TenantTestEntity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeysetPagedTenantEntitiesSpec"/> class.
    /// </summary>
    /// <param name="lastCreatedAtUtc">The last CreatedAtUtc value from the previous page, or null for first page.</param>
    /// <param name="take">Number of items to take.</param>
    public KeysetPagedTenantEntitiesSpec(DateTime? lastCreatedAtUtc, int take)
    {
        AddCriteria(e => e.IsActive);

        if (lastCreatedAtUtc.HasValue)
        {
            AddCriteria(e => e.CreatedAtUtc > lastCreatedAtUtc.Value);
        }

        ApplyOrderBy(e => e.CreatedAtUtc);
        ApplyThenBy(e => e.Id);
        ApplyKeysetPagination(e => e.CreatedAtUtc, lastCreatedAtUtc, take);
    }
}
