namespace Encina.Security.ABAC;

/// <summary>
/// The built-in subject attributes the Policy Enforcement Point adds for every authenticated caller.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ABACPipelineBehavior{TRequest, TResponse}"/> reads the caller from
/// <see cref="IRequestContext.Identity"/> once and adds these attributes after the ones returned by
/// <see cref="IAttributeProvider.GetSubjectAttributesAsync"/>, so a provider cannot replace them. They
/// let a policy target a user or a declared service identity with the default
/// <see cref="Providers.DefaultAttributeProvider"/>.
/// </para>
/// <para>
/// A policy reads them with a Subject attribute designator whose <c>AttributeId</c> is
/// <see cref="SubjectId"/> or <see cref="IdentityKind"/>; an EEL condition
/// (<see cref="RequireConditionAttribute"/>) reads them by name from the <c>user</c> dictionary.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A target match that only a declared service identity satisfies
/// var match = new Match
/// {
///     FunctionId = XACMLFunctionIds.StringEqual,
///     AttributeDesignator = ConditionBuilder.Attribute(AttributeCategory.Subject, ABACSubjectAttributes.IdentityKind, XACMLDataTypes.String),
///     AttributeValue = new AttributeValue { DataType = XACMLDataTypes.String, Value = "service" }
/// };
/// </code>
/// </example>
public static class ABACSubjectAttributes
{
    /// <summary>
    /// The caller's subject: the identity-provider subject of a user, or <c>service:&lt;name&gt;</c>
    /// for a declared service identity (<see cref="RequestIdentity.UserId"/>).
    /// </summary>
    public const string SubjectId = "subject-id";

    /// <summary>
    /// The kind of caller, lowercase: <c>user</c> or <c>service</c> (<see cref="RequestIdentity.Kind"/>).
    /// </summary>
    public const string IdentityKind = "identity-kind";

    /// <summary>
    /// Returns the provider's subject attributes plus the built-in ones of <paramref name="identity"/>;
    /// the built-in attributes win over a provider attribute with the same name.
    /// </summary>
    /// <param name="provided">The attributes returned by the attribute provider.</param>
    /// <param name="identity">The authenticated caller.</param>
    /// <returns>A new dictionary; <paramref name="provided"/> is not modified.</returns>
    internal static IReadOnlyDictionary<string, object> WithBuiltIns(
        IReadOnlyDictionary<string, object> provided,
        RequestIdentity identity)
    {
        var attributes = new Dictionary<string, object>(provided, StringComparer.Ordinal)
        {
            [SubjectId] = identity.UserId!,
            [IdentityKind] = KindName(identity.Kind)
        };

        return attributes;
    }

    /// <summary>
    /// The lowercase name of an identity kind, as the <see cref="IdentityKind"/> attribute and the
    /// policy administration point's audit metadata record it.
    /// </summary>
    /// <param name="kind">The identity kind.</param>
    /// <returns><c>user</c>, <c>service</c> or <c>anonymous</c>.</returns>
    // crap-exempt: single-question switch — the lowercase name of each identity kind.
    internal static string KindName(global::Encina.IdentityKind kind) => kind switch
    {
        global::Encina.IdentityKind.User => "user",
        global::Encina.IdentityKind.Service => "service",
        _ => "anonymous"
    };
}
