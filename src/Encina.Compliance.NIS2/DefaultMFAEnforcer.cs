using Encina.Compliance.NIS2.Abstractions;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Default implementation of <see cref="IMFAEnforcer"/> that assumes MFA is handled externally.
/// </summary>
/// <remarks>
/// <para>
/// This pass-through implementation always returns <c>true</c> for MFA status checks and
/// <see cref="Unit"/> for MFA requirement validation, assuming that the application's
/// authentication infrastructure (e.g., Azure AD, Keycloak, Auth0) handles MFA independently.
/// </para>
/// <para>
/// Applications should register a custom <see cref="IMFAEnforcer"/> implementation before
/// calling <c>AddEncinaNIS2()</c> to integrate with their specific identity provider and
/// enforce actual MFA checks at the pipeline level.
/// </para>
/// </remarks>
internal sealed class DefaultMFAEnforcer : IMFAEnforcer
{
    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> IsMFAEnabledAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Right<EncinaError, bool>(true));

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RequireMFAAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
}
