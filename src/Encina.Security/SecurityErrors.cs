namespace Encina.Security;

/// <summary>
/// Factory methods for security-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes belong to the <c>encina.authorization.*</c> family of <see cref="EncinaErrorCodes"/>:
/// unauthenticated and missing-context errors are 401, every other denial is 403.
/// All errors include structured metadata for observability.
/// </remarks>
public static class SecurityErrors
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageSecurity = "security";

    /// <summary>
    /// Creates an error for unauthenticated access.
    /// </summary>
    /// <param name="requestType">The request type that required authentication.</param>
    /// <returns>An error indicating the user is not authenticated.</returns>
    public static EncinaError Unauthenticated(Type requestType) =>
        EncinaErrors.Create(
            code: EncinaErrorCodes.AuthorizationUnauthenticated,
            message: $"Request '{requestType.Name}' requires authentication.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageSecurity,
                ["requirement"] = "authenticated"
            });

    /// <summary>
    /// Creates an error for insufficient roles.
    /// </summary>
    /// <param name="requestType">The request type that required the roles.</param>
    /// <param name="requiredRoles">The roles that were required.</param>
    /// <param name="userId">The user ID that was denied.</param>
    /// <param name="requireAll">Whether all roles were required (AND) or any (OR).</param>
    /// <returns>An error indicating insufficient roles.</returns>
    public static EncinaError InsufficientRoles(
        Type requestType,
        IEnumerable<string> requiredRoles,
        string? userId,
        bool requireAll = false) =>
        EncinaErrors.Create(
            code: EncinaErrorCodes.AuthorizationInsufficientRoles,
            message: $"User does not have the required roles ({string.Join(", ", requiredRoles)}) for '{requestType.Name}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageSecurity,
                ["requirement"] = requireAll ? "all_roles" : "any_role",
                ["requiredRoles"] = requiredRoles.ToList(),
                ["userId"] = userId
            });

    /// <summary>
    /// Creates an error for missing permissions.
    /// </summary>
    /// <param name="requestType">The request type that required the permissions.</param>
    /// <param name="requiredPermissions">The permissions that were required.</param>
    /// <param name="userId">The user ID that was denied.</param>
    /// <param name="requireAll">Whether all permissions were required (AND) or any (OR).</param>
    /// <returns>An error indicating missing permissions.</returns>
    public static EncinaError PermissionDenied(
        Type requestType,
        IEnumerable<string> requiredPermissions,
        string? userId,
        bool requireAll = false) =>
        EncinaErrors.Create(
            code: EncinaErrorCodes.AuthorizationPermissionDenied,
            message: $"User does not have the required permissions ({string.Join(", ", requiredPermissions)}) for '{requestType.Name}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageSecurity,
                ["requirement"] = requireAll ? "all_permissions" : "any_permission",
                ["requiredPermissions"] = requiredPermissions.ToList(),
                ["userId"] = userId
            });

    /// <summary>
    /// Creates an error for a missing or mismatched claim.
    /// </summary>
    /// <param name="requestType">The request type that required the claim.</param>
    /// <param name="claimType">The claim type that was required.</param>
    /// <param name="claimValue">The expected claim value, or <c>null</c> if only existence was checked.</param>
    /// <param name="userId">The user ID that was denied.</param>
    /// <returns>An error indicating a missing or mismatched claim.</returns>
    public static EncinaError ClaimMissing(
        Type requestType,
        string claimType,
        string? claimValue,
        string? userId) =>
        EncinaErrors.Create(
            code: EncinaErrorCodes.AuthorizationClaimMissing,
            message: claimValue is null
                ? $"User is missing required claim type '{claimType}' for '{requestType.Name}'."
                : $"User is missing required claim '{claimType}={claimValue}' for '{requestType.Name}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageSecurity,
                ["requirement"] = "claim",
                ["claimType"] = claimType,
                ["claimValue"] = claimValue,
                ["userId"] = userId
            });

    /// <summary>
    /// Creates an error when the user is not the resource owner.
    /// </summary>
    /// <param name="requestType">The request type that required ownership.</param>
    /// <param name="ownerProperty">The property name used for ownership verification.</param>
    /// <param name="userId">The user ID that was denied.</param>
    /// <returns>An error indicating the user is not the resource owner.</returns>
    public static EncinaError NotOwner(
        Type requestType,
        string ownerProperty,
        string? userId) =>
        EncinaErrors.Create(
            code: EncinaErrorCodes.AuthorizationNotOwner,
            message: $"User is not the owner of the resource for '{requestType.Name}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageSecurity,
                ["requirement"] = "ownership",
                ["ownerProperty"] = ownerProperty,
                ["userId"] = userId
            });

    /// <summary>
    /// Creates an error when the security context is not available.
    /// </summary>
    /// <param name="requestType">The request type that required security context.</param>
    /// <returns>An error indicating the security context is missing.</returns>
    /// <remarks>
    /// Carries <see cref="EncinaErrorCodes.AuthorizationUnauthenticated"/> (401): a missing security context
    /// is treated as an unauthenticated request until #1705 Phase 5 removes the second identity channel,
    /// where this error disappears.
    /// </remarks>
    public static EncinaError MissingContext(Type requestType) =>
        EncinaErrors.Create(
            code: EncinaErrorCodes.AuthorizationUnauthenticated,
            message: $"Security context is not available for '{requestType.Name}'. Ensure security middleware is configured.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageSecurity,
                ["requirement"] = "security_context"
            });
}
