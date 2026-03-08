namespace Encina.Security.ABAC;

/// <summary>
/// Policy Information Point (PIP) — resolves attribute values on demand during
/// XACML policy evaluation.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.3 — When the PDP encounters an <see cref="AttributeDesignator"/>
/// during evaluation and the attribute is not already present in the
/// <see cref="PolicyEvaluationContext"/>, it delegates to the PIP for on-demand resolution.
/// </para>
/// <para>
/// The PIP returns an <see cref="AttributeBag"/> which may be empty if the attribute
/// cannot be resolved. If <see cref="AttributeDesignator.MustBePresent"/> is <c>true</c>
/// and the bag is empty, the PDP produces an Indeterminate result.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var designator = new AttributeDesignator
/// {
///     Category = AttributeCategory.Subject,
///     AttributeId = "department",
///     DataType = XACMLDataTypes.String,
///     MustBePresent = true
/// };
///
/// AttributeBag bag = await pip.ResolveAttributeAsync(designator, ct);
/// </code>
/// </example>
public interface IPolicyInformationPoint
{
    /// <summary>
    /// Resolves an attribute value based on the given designator.
    /// </summary>
    /// <param name="designator">
    /// The attribute designator specifying the category, attribute ID, and data type to resolve.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// An <see cref="AttributeBag"/> containing the resolved values, or
    /// <see cref="AttributeBag.Empty"/> if the attribute could not be resolved.
    /// </returns>
    ValueTask<AttributeBag> ResolveAttributeAsync(
        AttributeDesignator designator,
        CancellationToken cancellationToken = default);
}
