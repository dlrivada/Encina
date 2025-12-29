namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for domain services.
/// </summary>
/// <remarks>
/// Domain services contain domain logic that doesn't naturally fit within an aggregate.
/// They operate on domain concepts and enforce business rules.
///
/// Use domain services when:
/// <list type="bullet">
/// <item><description>The operation spans multiple aggregates</description></item>
/// <item><description>The operation requires data from external sources</description></item>
/// <item><description>The logic doesn't belong to any single entity</description></item>
/// <item><description>The operation represents a significant business process</description></item>
/// </list>
///
/// Domain services should:
/// <list type="bullet">
/// <item><description>Be stateless</description></item>
/// <item><description>Use domain language in naming</description></item>
/// <item><description>Accept and return domain objects</description></item>
/// <item><description>Not depend on infrastructure concerns</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class OrderPricingService : IDomainService
/// {
///     public Money CalculateTotal(Order order, IEnumerable&lt;Discount&gt; discounts)
///     {
///         var subtotal = order.Items.Sum(i =&gt; i.Quantity * i.UnitPrice);
///         var discountAmount = discounts.Aggregate(Money.Zero,
///             (acc, d) =&gt; acc + d.Calculate(subtotal));
///         return subtotal - discountAmount;
///     }
/// }
/// </code>
/// </example>
public interface IDomainService
{
}
