using System.Reflection;

namespace Encina.Security.ABAC;

/// <summary>
/// Caches ABAC-related attribute metadata discovered on a request type.
/// </summary>
/// <remarks>
/// <para>
/// This record is used by <see cref="ABACPipelineBehavior{TRequest, TResponse}"/> to avoid
/// repeated reflection on every request. Each generic instantiation caches a single static
/// instance via <c>private static readonly ABACAttributeInfo? CachedInfo = Resolve&lt;TRequest&gt;()</c>.
/// </para>
/// <para>
/// If the request type has no <see cref="RequirePolicyAttribute"/> or
/// <see cref="RequireConditionAttribute"/> decorations, the resolved value is <c>null</c>,
/// signaling the behavior to skip ABAC evaluation entirely.
/// </para>
/// </remarks>
public sealed record ABACAttributeInfo
{
    /// <summary>
    /// Gets the policy attributes declared on the request type.
    /// </summary>
    public required IReadOnlyList<RequirePolicyAttribute> PolicyAttributes { get; init; }

    /// <summary>
    /// Gets the inline condition attributes declared on the request type.
    /// </summary>
    public required IReadOnlyList<RequireConditionAttribute> ConditionAttributes { get; init; }

    /// <summary>
    /// Gets a value indicating whether any ABAC attributes are present.
    /// </summary>
    public bool HasPolicyAttributes => PolicyAttributes.Count > 0;

    /// <summary>
    /// Gets a value indicating whether any inline condition attributes are present.
    /// </summary>
    public bool HasConditionAttributes => ConditionAttributes.Count > 0;

    /// <summary>
    /// Resolves ABAC attribute metadata from the specified request type using reflection.
    /// </summary>
    /// <typeparam name="TRequest">The request type to inspect.</typeparam>
    /// <returns>
    /// An <see cref="ABACAttributeInfo"/> instance if any ABAC attributes are found;
    /// <c>null</c> if the request type has no ABAC decorations.
    /// </returns>
    /// <remarks>
    /// This method is designed to be called once per generic type instantiation
    /// and cached in a static field for zero-cost subsequent access.
    /// </remarks>
    public static ABACAttributeInfo? Resolve<TRequest>()
    {
        var type = typeof(TRequest);

        var policyAttributes = type
            .GetCustomAttributes<RequirePolicyAttribute>(inherit: true)
            .OrderBy(a => a.Order)
            .ToList();

        var conditionAttributes = type
            .GetCustomAttributes<RequireConditionAttribute>(inherit: true)
            .OrderBy(a => a.Order)
            .ToList();

        if (policyAttributes.Count == 0 && conditionAttributes.Count == 0)
        {
            return null;
        }

        return new ABACAttributeInfo
        {
            PolicyAttributes = policyAttributes,
            ConditionAttributes = conditionAttributes
        };
    }
}
