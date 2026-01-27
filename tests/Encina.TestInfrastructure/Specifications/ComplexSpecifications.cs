using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encina.TestInfrastructure.Specifications;

/// <summary>
/// Complex specification demonstrating deep nesting of AND/OR/NOT operations.
/// Used to test SQL generation edge cases across different providers.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class DeepNestedLogicalSpec<T> : Specification<T>
    where T : class
{
    private readonly Expression<Func<T, bool>> _expression;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeepNestedLogicalSpec{T}"/> class.
    /// </summary>
    /// <param name="expression">The complex expression.</param>
    public DeepNestedLogicalSpec(Expression<Func<T, bool>> expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <inheritdoc />
    public override Expression<Func<T, bool>> ToExpression() => _expression;
}

/// <summary>
/// Specification for testing string operations across providers.
/// Different databases handle string operations differently (case sensitivity, collation).
/// </summary>
public sealed class StringOperationsSpec : Specification<TenantTestEntity>
{
    private readonly string _pattern;
    private readonly StringMatchMode _matchMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringOperationsSpec"/> class.
    /// </summary>
    /// <param name="pattern">The pattern to match.</param>
    /// <param name="matchMode">The string matching mode.</param>
    public StringOperationsSpec(string pattern, StringMatchMode matchMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        _pattern = pattern;
        _matchMode = matchMode;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression() => _matchMode switch
    {
        StringMatchMode.Contains => e => e.Name.Contains(_pattern),
        StringMatchMode.StartsWith => e => e.Name.StartsWith(_pattern),
        StringMatchMode.EndsWith => e => e.Name.EndsWith(_pattern),
        StringMatchMode.Exact => e => e.Name == _pattern,
        // Note: Case-insensitive comparison in LINQ-to-SQL depends on database collation.
        // EF Core will translate this appropriately per provider.
        StringMatchMode.ContainsIgnoreCase => e => EF.Functions.Like(e.Name, $"%{_pattern}%"),
        _ => throw new ArgumentOutOfRangeException(nameof(_matchMode))
    };
}

/// <summary>
/// String matching modes for testing different SQL LIKE patterns.
/// </summary>
public enum StringMatchMode
{
    /// <summary>
    /// Contains the pattern anywhere (LIKE '%pattern%').
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with the pattern (LIKE 'pattern%').
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with the pattern (LIKE '%pattern').
    /// </summary>
    EndsWith,

    /// <summary>
    /// Exact match (= 'pattern').
    /// </summary>
    Exact,

    /// <summary>
    /// Case-insensitive contains (provider-specific implementation).
    /// </summary>
    ContainsIgnoreCase
}

/// <summary>
/// Specification for testing multi-property filter combinations.
/// </summary>
public sealed class MultiPropertyFilterSpec : Specification<TenantTestEntity>
{
    private readonly string? _nameContains;
    private readonly decimal? _minAmount;
    private readonly decimal? _maxAmount;
    private readonly bool? _isActive;
    private readonly DateTime? _createdAfter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiPropertyFilterSpec"/> class.
    /// </summary>
    public MultiPropertyFilterSpec(
        string? nameContains = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        bool? isActive = null,
        DateTime? createdAfter = null)
    {
        _nameContains = nameContains;
        _minAmount = minAmount;
        _maxAmount = maxAmount;
        _isActive = isActive;
        _createdAfter = createdAfter;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
    {
        return entity =>
            (_nameContains == null || entity.Name.Contains(_nameContains))
            && (_minAmount == null || entity.Amount >= _minAmount)
            && (_maxAmount == null || entity.Amount <= _maxAmount)
            && (_isActive == null || entity.IsActive == _isActive)
            && (_createdAfter == null || entity.CreatedAtUtc > _createdAfter);
    }
}

/// <summary>
/// Specification for testing OR-combined conditions across different properties.
/// </summary>
public sealed class OrCombinedConditionsSpec : Specification<TenantTestEntity>
{
    private readonly string _namePattern;
    private readonly decimal _amountThreshold;
    private readonly DateTime _dateThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrCombinedConditionsSpec"/> class.
    /// Matches entities where: name contains pattern OR amount > threshold OR created after date.
    /// </summary>
    public OrCombinedConditionsSpec(string namePattern, decimal amountThreshold, DateTime dateThreshold)
    {
        _namePattern = namePattern ?? throw new ArgumentNullException(nameof(namePattern));
        _amountThreshold = amountThreshold;
        _dateThreshold = dateThreshold;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
    {
        return entity =>
            entity.Name.Contains(_namePattern)
            || entity.Amount > _amountThreshold
            || entity.CreatedAtUtc > _dateThreshold;
    }
}

/// <summary>
/// Specification demonstrating mixed AND/OR with NOT.
/// Pattern: (A AND B) OR (NOT C AND D)
/// </summary>
public sealed class MixedLogicalOperationsSpec : Specification<TenantTestEntity>
{
    private readonly bool _activeCondition;
    private readonly decimal _amountThreshold;
    private readonly string _excludePattern;
    private readonly DateTime _dateThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="MixedLogicalOperationsSpec"/> class.
    /// </summary>
    public MixedLogicalOperationsSpec(
        bool activeCondition,
        decimal amountThreshold,
        string excludePattern,
        DateTime dateThreshold)
    {
        _activeCondition = activeCondition;
        _amountThreshold = amountThreshold;
        _excludePattern = excludePattern ?? throw new ArgumentNullException(nameof(excludePattern));
        _dateThreshold = dateThreshold;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
    {
        // (IsActive == _activeCondition AND Amount >= _amountThreshold)
        // OR
        // (NOT Name.Contains(_excludePattern) AND CreatedAtUtc > _dateThreshold)
        return entity =>
            (entity.IsActive == _activeCondition && entity.Amount >= _amountThreshold)
            ||
            (!entity.Name.Contains(_excludePattern) && entity.CreatedAtUtc > _dateThreshold);
    }
}

/// <summary>
/// Specification for testing nullable property comparisons.
/// </summary>
public sealed class NullablePropertySpec : Specification<TenantTestEntity>
{
    private readonly NullableCheckMode _mode;
    private readonly DateTime? _compareDate;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullablePropertySpec"/> class.
    /// </summary>
    public NullablePropertySpec(NullableCheckMode mode, DateTime? compareDate = null)
    {
        _mode = mode;
        _compareDate = compareDate;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression() => _mode switch
    {
        NullableCheckMode.DescriptionIsNull => e => e.Description == null,
        NullableCheckMode.DescriptionIsNotNull => e => e.Description != null,
        NullableCheckMode.UpdatedAtIsNull => e => e.UpdatedAtUtc == null,
        NullableCheckMode.UpdatedAtIsNotNull => e => e.UpdatedAtUtc != null,
        NullableCheckMode.UpdatedAtBefore => e => e.UpdatedAtUtc != null && e.UpdatedAtUtc < _compareDate,
        NullableCheckMode.UpdatedAtAfter => e => e.UpdatedAtUtc != null && e.UpdatedAtUtc > _compareDate,
        _ => throw new ArgumentOutOfRangeException(nameof(_mode))
    };
}

/// <summary>
/// Modes for nullable property checking.
/// </summary>
public enum NullableCheckMode
{
    /// <summary>
    /// Description property is null.
    /// </summary>
    DescriptionIsNull,

    /// <summary>
    /// Description property is not null.
    /// </summary>
    DescriptionIsNotNull,

    /// <summary>
    /// UpdatedAtUtc property is null.
    /// </summary>
    UpdatedAtIsNull,

    /// <summary>
    /// UpdatedAtUtc property is not null.
    /// </summary>
    UpdatedAtIsNotNull,

    /// <summary>
    /// UpdatedAtUtc is not null and before a given date.
    /// </summary>
    UpdatedAtBefore,

    /// <summary>
    /// UpdatedAtUtc is not null and after a given date.
    /// </summary>
    UpdatedAtAfter
}

/// <summary>
/// Specification for testing IN clause equivalent (multiple values check).
/// </summary>
public sealed class InValuesSpec : Specification<TenantTestEntity>
{
    private readonly IReadOnlyList<Guid> _allowedIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="InValuesSpec"/> class.
    /// </summary>
    /// <param name="allowedIds">The list of allowed IDs.</param>
    public InValuesSpec(IEnumerable<Guid> allowedIds)
    {
        _allowedIds = allowedIds?.ToList() ?? throw new ArgumentNullException(nameof(allowedIds));
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
    {
        return entity => _allowedIds.Contains(entity.Id);
    }
}

/// <summary>
/// Specification for testing decimal precision across providers.
/// </summary>
public sealed class DecimalPrecisionSpec : Specification<TenantTestEntity>
{
    private readonly decimal _exactAmount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalPrecisionSpec"/> class.
    /// </summary>
    /// <param name="exactAmount">The exact amount to match (tests decimal precision).</param>
    public DecimalPrecisionSpec(decimal exactAmount)
    {
        _exactAmount = exactAmount;
    }

    /// <inheritdoc />
    public override Expression<Func<TenantTestEntity, bool>> ToExpression()
    {
        return entity => entity.Amount == _exactAmount;
    }
}
