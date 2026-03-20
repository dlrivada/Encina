using LanguageExt;

namespace Encina.Compliance.NIS2.Abstractions;

/// <summary>
/// Enforces multi-factor authentication (MFA) requirements under NIS2 Article 21(2)(j).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 21(2)(j), entities must implement "the use of multi-factor authentication
/// or continuous authentication solutions, secured voice, video and text communications and
/// secured emergency communication systems within the entity, where appropriate."
/// </para>
/// <para>
/// The default implementation assumes MFA is handled externally by the application's
/// authentication infrastructure and always returns <c>true</c>. Applications should register
/// a custom <see cref="IMFAEnforcer"/> implementation that integrates with their identity
/// provider (e.g., Azure AD, Keycloak, Auth0) to perform actual MFA status checks.
/// </para>
/// <para>
/// The <c>NIS2CompliancePipelineBehavior</c> invokes this enforcer for requests decorated
/// with the <c>[RequireMFA]</c> attribute.
/// </para>
/// </remarks>
public interface IMFAEnforcer
{
    /// <summary>
    /// Checks whether MFA is enabled for the specified user.
    /// </summary>
    /// <param name="userId">Identifier of the user to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if MFA is enabled for the user; <c>false</c> if MFA is not enabled;
    /// or an <see cref="EncinaError"/> if the check could not be performed.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> IsMFAEnabledAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the current request context satisfies MFA requirements.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request being validated.</typeparam>
    /// <param name="request">The request that requires MFA.</param>
    /// <param name="context">The request context containing user identity and authentication information.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> if MFA requirements are satisfied; or an <see cref="EncinaError"/>
    /// with code <c>nis2.mfa_required</c> if MFA is not enabled for the current user.
    /// </returns>
    /// <remarks>
    /// This method is called by the <c>NIS2CompliancePipelineBehavior</c> for requests
    /// decorated with <c>[RequireMFA]</c>. Implementations should extract the user identity
    /// from <paramref name="context"/> and verify MFA status.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RequireMFAAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken = default);
}
