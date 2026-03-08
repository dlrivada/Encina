namespace Encina.Security.ABAC;

/// <summary>
/// Defines common comparison operators used as syntactic sugar in the fluent policy DSL.
/// </summary>
/// <remarks>
/// <para>
/// These operators provide a C#-idiomatic way to express conditions in the policy builder.
/// Each operator maps to one or more XACML 3.0 standard functions internally. For example,
/// <see cref="Equals"/> maps to the appropriate <c>*-equal</c> function based on the data type.
/// </para>
/// <para>
/// Users interact with <see cref="ConditionOperator"/> values through the fluent DSL
/// (e.g., <c>.When("user.department", Equals, "Finance")</c>), and the builder translates
/// them into the underlying <c>Apply</c>/<c>Function</c> tree structure defined by XACML 3.0 §7.7.
/// </para>
/// </remarks>
public enum ConditionOperator
{
    /// <summary>
    /// Tests whether two values are equal.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>*-equal</c> functions (e.g., <c>string-equal</c>, <c>integer-equal</c>).</remarks>
    Equals,

    /// <summary>
    /// Tests whether two values are not equal.
    /// </summary>
    /// <remarks>Maps to a logical negation of the corresponding <c>*-equal</c> function.</remarks>
    NotEquals,

    /// <summary>
    /// Tests whether the first value is strictly greater than the second.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>*-greater-than</c> functions.</remarks>
    GreaterThan,

    /// <summary>
    /// Tests whether the first value is greater than or equal to the second.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>*-greater-than-or-equal</c> functions.</remarks>
    GreaterThanOrEqual,

    /// <summary>
    /// Tests whether the first value is strictly less than the second.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>*-less-than</c> functions.</remarks>
    LessThan,

    /// <summary>
    /// Tests whether the first value is less than or equal to the second.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>*-less-than-or-equal</c> functions.</remarks>
    LessThanOrEqual,

    /// <summary>
    /// Tests whether a string value contains the specified substring.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>string-contains</c> function.</remarks>
    Contains,

    /// <summary>
    /// Tests whether a string value does not contain the specified substring.
    /// </summary>
    /// <remarks>Maps to a logical negation of <c>string-contains</c>.</remarks>
    NotContains,

    /// <summary>
    /// Tests whether a string value starts with the specified prefix.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>string-starts-with</c> function.</remarks>
    StartsWith,

    /// <summary>
    /// Tests whether a string value ends with the specified suffix.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>string-ends-with</c> function.</remarks>
    EndsWith,

    /// <summary>
    /// Tests whether a value is a member of a specified bag (collection).
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>*-is-in</c> bag functions.</remarks>
    In,

    /// <summary>
    /// Tests whether a value is not a member of a specified bag (collection).
    /// </summary>
    /// <remarks>Maps to a logical negation of the corresponding <c>*-is-in</c> function.</remarks>
    NotIn,

    /// <summary>
    /// Tests whether the attribute exists (is present in the evaluation context).
    /// </summary>
    /// <remarks>Implemented via <c>AttributeDesignator.MustBePresent</c> semantics — returns true if the attribute bag is non-empty.</remarks>
    Exists,

    /// <summary>
    /// Tests whether the attribute does not exist (is absent from the evaluation context).
    /// </summary>
    /// <remarks>Returns true if the attribute bag is empty — the attribute could not be resolved.</remarks>
    DoesNotExist,

    /// <summary>
    /// Tests whether a string value matches a regular expression pattern.
    /// </summary>
    /// <remarks>Maps to XACML 3.0 <c>string-regexp-match</c> function.</remarks>
    RegexMatch
}
