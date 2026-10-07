namespace Encina.Security.ABAC.Evaluation;

/// <summary>
/// Selects the attribute values an <see cref="AttributeDesignator"/> refers to from a
/// <see cref="PolicyEvaluationContext"/>, by category, attribute identifier and data type.
/// </summary>
/// <remarks>
/// XACML 3.0 §7.3.5 — A designator returns the bag of the attribute named by its
/// <see cref="AttributeDesignator.AttributeId"/> in its <see cref="AttributeDesignator.Category"/>,
/// keeping only the values whose data type equals <see cref="AttributeDesignator.DataType"/>.
/// Values of other attributes of the same category are never returned (#1983). The caller
/// applies <see cref="AttributeDesignator.MustBePresent"/> to an empty result.
/// </remarks>
internal static class AttributeDesignatorResolver
{
    /// <summary>
    /// Returns the values of the attribute <paramref name="designator"/> refers to, or
    /// <see cref="AttributeBag.Empty"/> when the attribute is absent or has no value of the
    /// designator's data type.
    /// </summary>
    internal static AttributeBag Resolve(AttributeDesignator designator, PolicyEvaluationContext context)
    {
        var attributes = CategoryAttributes(designator.Category, context);

        // A missing category dictionary, a missing attribute and a null bag are all "absent".
        if (attributes is null || !attributes.TryGetValue(designator.AttributeId, out var bag) || bag is null)
        {
            return AttributeBag.Empty;
        }

        return FilterByDataType(bag, designator.DataType);
    }

    // crap-exempt: single-question switch — maps an attribute category to its dictionary in the context.
    private static IReadOnlyDictionary<string, AttributeBag>? CategoryAttributes(
        AttributeCategory category,
        PolicyEvaluationContext context) =>
        category switch
        {
            AttributeCategory.Subject => context.SubjectAttributes,
            AttributeCategory.Resource => context.ResourceAttributes,
            AttributeCategory.Environment => context.EnvironmentAttributes,
            AttributeCategory.Action => context.ActionAttributes,
            _ => null
        };

    private static AttributeBag FilterByDataType(AttributeBag bag, string dataType)
    {
        // The common case, every value has the designator's data type, returns the stored bag
        // without allocating; a filtered copy is built only when some value must be dropped.
        foreach (var value in bag.Values)
        {
            if (!HasDataType(value, dataType))
            {
                return AttributeBag.FromValues(bag.Values.Where(v => HasDataType(v, dataType)).ToArray());
            }
        }

        return bag;
    }

    private static bool HasDataType(AttributeValue value, string dataType) =>
        string.Equals(value.DataType, dataType, StringComparison.Ordinal);
}
