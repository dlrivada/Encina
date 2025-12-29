using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Represents a business rule that can be validated.
/// Business rules are domain invariants that must be satisfied for an operation to succeed.
/// </summary>
/// <remarks>
/// Business rules differ from input validation:
/// - Input validation: Checks data format (email format, string length)
/// - Business rules: Enforces domain invariants (order must have items, customer credit limit)
///
/// Use business rules within aggregates and domain services to protect invariants.
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderMustHaveItemsRule : BusinessRule
/// {
///     private readonly Order _order;
///
///     public OrderMustHaveItemsRule(Order order) =&gt; _order = order;
///
///     public override string ErrorCode =&gt; "ORDER_NO_ITEMS";
///     public override string ErrorMessage =&gt; "Order must have at least one item";
///     public override bool IsSatisfied() =&gt; _order.Items.Any();
/// }
/// </code>
/// </example>
public interface IBusinessRule
{
    /// <summary>
    /// Gets the error code for this rule violation.
    /// </summary>
    string ErrorCode { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    string ErrorMessage { get; }

    /// <summary>
    /// Checks whether the business rule is satisfied.
    /// </summary>
    /// <returns>True if the rule is satisfied; otherwise, false.</returns>
    bool IsSatisfied();
}

/// <summary>
/// Abstract base class for business rules.
/// </summary>
public abstract class BusinessRule : IBusinessRule
{
    /// <inheritdoc />
    public abstract string ErrorCode { get; }

    /// <inheritdoc />
    public abstract string ErrorMessage { get; }

    /// <inheritdoc />
    public abstract bool IsSatisfied();
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
/// <remarks>
/// Use this for throw-based flow control in aggregates.
/// For ROP-based flow, use <see cref="BusinessRuleExtensions.Check"/>.
/// </remarks>
public sealed class BusinessRuleViolationException : Exception
{
    /// <summary>
    /// Gets the business rule that was violated.
    /// </summary>
    public IBusinessRule BrokenRule { get; }

    /// <summary>
    /// Gets the error code from the broken rule.
    /// </summary>
    public string ErrorCode => BrokenRule.ErrorCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
    /// </summary>
    /// <param name="brokenRule">The rule that was violated.</param>
    public BusinessRuleViolationException(IBusinessRule brokenRule)
        : base(brokenRule?.ErrorMessage ?? "A business rule was violated")
    {
        ArgumentNullException.ThrowIfNull(brokenRule);
        BrokenRule = brokenRule;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
    /// </summary>
    /// <param name="brokenRule">The rule that was violated.</param>
    /// <param name="innerException">The inner exception.</param>
    public BusinessRuleViolationException(IBusinessRule brokenRule, Exception innerException)
        : base(brokenRule?.ErrorMessage ?? "A business rule was violated", innerException)
    {
        ArgumentNullException.ThrowIfNull(brokenRule);
        BrokenRule = brokenRule;
    }
}

/// <summary>
/// Represents an error resulting from a business rule violation.
/// </summary>
/// <param name="ErrorCode">The error code identifying the rule.</param>
/// <param name="ErrorMessage">The human-readable error message.</param>
public sealed record BusinessRuleError(string ErrorCode, string ErrorMessage)
{
    /// <summary>
    /// Creates a business rule error from a business rule.
    /// </summary>
    /// <param name="rule">The violated business rule.</param>
    /// <returns>A new business rule error.</returns>
    public static BusinessRuleError From(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new BusinessRuleError(rule.ErrorCode, rule.ErrorMessage);
    }
}

/// <summary>
/// Represents multiple accumulated business rule errors.
/// </summary>
/// <param name="Errors">The list of business rule errors.</param>
public sealed record AggregateBusinessRuleError(IReadOnlyList<BusinessRuleError> Errors)
{
    /// <summary>
    /// Creates an aggregate error from multiple business rule errors.
    /// </summary>
    /// <param name="errors">The errors to aggregate.</param>
    /// <returns>A new aggregate business rule error.</returns>
    public static AggregateBusinessRuleError From(params BusinessRuleError[] errors)
        => new(errors);

    /// <summary>
    /// Creates an aggregate error from multiple business rules.
    /// </summary>
    /// <param name="rules">The violated business rules.</param>
    /// <returns>A new aggregate business rule error.</returns>
    public static AggregateBusinessRuleError FromRules(IEnumerable<IBusinessRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return new AggregateBusinessRuleError(
            rules.Select(BusinessRuleError.From).ToList());
    }
}

/// <summary>
/// Extension methods for business rule validation using Railway Oriented Programming.
/// </summary>
public static class BusinessRuleExtensions
{
    /// <summary>
    /// Checks a single business rule and returns an Either result.
    /// </summary>
    /// <param name="rule">The business rule to check.</param>
    /// <returns>Right(Unit) if satisfied; Left(BusinessRuleError) if violated.</returns>
    public static Either<BusinessRuleError, Unit> Check(this IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule.IsSatisfied()
            ? Unit.Default
            : BusinessRuleError.From(rule);
    }

    /// <summary>
    /// Checks multiple business rules and returns an Either result.
    /// Uses fail-fast semantics (stops at first failure).
    /// </summary>
    /// <param name="rules">The business rules to check.</param>
    /// <returns>Right(Unit) if all satisfied; Left(BusinessRuleError) of first violation.</returns>
    public static Either<BusinessRuleError, Unit> CheckFirst(this IEnumerable<IBusinessRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
        {
            if (!rule.IsSatisfied())
            {
                return BusinessRuleError.From(rule);
            }
        }

        return Unit.Default;
    }

    /// <summary>
    /// Checks multiple business rules and accumulates all failures.
    /// </summary>
    /// <param name="rules">The business rules to check.</param>
    /// <returns>Right(Unit) if all satisfied; Left(AggregateBusinessRuleError) with all violations.</returns>
    public static Either<AggregateBusinessRuleError, Unit> CheckAll(this IEnumerable<IBusinessRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var violations = rules
            .Where(r => !r.IsSatisfied())
            .Select(BusinessRuleError.From)
            .ToList();

        return violations.Count == 0
            ? Unit.Default
            : new AggregateBusinessRuleError(violations);
    }

    /// <summary>
    /// Throws a <see cref="BusinessRuleViolationException"/> if the rule is not satisfied.
    /// </summary>
    /// <param name="rule">The business rule to check.</param>
    /// <exception cref="BusinessRuleViolationException">Thrown when the rule is violated.</exception>
    public static void ThrowIfNotSatisfied(this IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (!rule.IsSatisfied())
        {
            throw new BusinessRuleViolationException(rule);
        }
    }

    /// <summary>
    /// Throws if any rule in the collection is not satisfied.
    /// Uses fail-fast semantics.
    /// </summary>
    /// <param name="rules">The business rules to check.</param>
    /// <exception cref="BusinessRuleViolationException">Thrown when any rule is violated.</exception>
    public static void ThrowIfAnyNotSatisfied(this IEnumerable<IBusinessRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
        {
            rule.ThrowIfNotSatisfied();
        }
    }
}
