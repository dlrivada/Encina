namespace Encina;

/// <summary>
/// The kind of caller a <see cref="RequestIdentity"/> represents.
/// </summary>
/// <remarks>
/// Gates read the kind instead of guessing it from the user id: only <see cref="User"/> and
/// <see cref="Service"/> identities are authenticated, and only a <see cref="User"/> is a data
/// subject that compliance gates may fall back to.
/// </remarks>
/// <example>
/// <code>
/// if (context.Identity is { Kind: IdentityKind.User } identity)
/// {
///     // a person authenticated by the identity provider
/// }
/// </code>
/// </example>
public enum IdentityKind
{
    /// <summary>No authenticated caller. Every gate that needs a caller denies this kind.</summary>
    Anonymous = 0,

    /// <summary>A person authenticated by an identity provider.</summary>
    User = 1,

    /// <summary>A declared service identity (background job, hosted service) opened through a logged scope.</summary>
    Service = 2
}
