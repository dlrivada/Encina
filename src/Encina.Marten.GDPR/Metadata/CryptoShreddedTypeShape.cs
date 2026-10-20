using System.Collections.Immutable;
using System.Reflection;

namespace Encina.Marten.GDPR;

/// <summary>
/// The reflection view of one type: its <c>[CryptoShredded]</c> members, the valid fields built from them,
/// the problems found by the reflection rules and the checks deferred for an open generic type.
/// </summary>
/// <param name="Type">The classified type.</param>
/// <param name="Members">Every attributed member (valid or not), most-derived declaration only.</param>
/// <param name="Fields">The valid fields of a closed type; empty for an open generic type.</param>
/// <param name="Issues">The problems found by the reflection rules.</param>
/// <param name="DeferredChecks">Property names whose subject-id type check is deferred to the closed type.</param>
internal sealed record CryptoShreddedTypeShape(
    Type Type,
    ImmutableArray<CryptoShreddedMember> Members,
    ImmutableArray<CryptoShreddedField> Fields,
    ImmutableArray<CryptoShreddedPropertyIssue> Issues,
    ImmutableArray<string> DeferredChecks)
{
    /// <summary>Gets whether the type declares or inherits at least one <c>[CryptoShredded]</c> property.</summary>
    internal bool IsOwner => !Members.IsEmpty;

    /// <summary>Gets whether the type is an owner or carries a reflection problem.</summary>
    internal bool IsRelevant => IsOwner || !Issues.IsEmpty;
}

/// <summary>
/// One <c>[CryptoShredded]</c> member as the reflection rules saw it.
/// </summary>
/// <param name="Property">The attributed property (most-derived declaration).</param>
/// <param name="SubjectIdProperty">The resolved subject-id sibling, or <c>null</c> when not found.</param>
/// <param name="Problems">The reflection problems of this member.</param>
internal sealed record CryptoShreddedMember(
    PropertyInfo Property,
    PropertyInfo? SubjectIdProperty,
    CryptoShreddedPropertyProblems Problems);
