using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore.Authorization;

/// <summary>
/// Default implementation of <see cref="IResourceAuthorizer"/> that delegates
/// to ASP.NET Core's <see cref="IAuthorizationService"/>.
/// </summary>
/// <remarks>
/// The caller is the ambient request identity (<see cref="IRequestContextAccessor"/>), the same one
/// the <c>[Authorize]</c> gate evaluates; never <c>HttpContext.User</c>, which on a long-lived
/// connection is the connect-time principal (#1705).
/// </remarks>
internal sealed class ResourceAuthorizer : IResourceAuthorizer
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public ResourceAuthorizer(
        IAuthorizationService authorizationService,
        IRequestContextAccessor requestContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);

        _authorizationService = authorizationService;
        _requestContextAccessor = requestContextAccessor;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, bool>> AuthorizeAsync<TResource>(
        TResource resource,
        string policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);

        return AuthorizeInternalAsync(resource, policy);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, bool>> AuthorizeAsync(
        object resource,
        string policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);

        return AuthorizeInternalAsync(resource, policy);
    }

    private async Task<Either<EncinaError, bool>> AuthorizeInternalAsync(
        object resource,
        string policy)
    {
        // No readable context, or a non-conforming one, reads as anonymous and is denied.
        if (_requestContextAccessor.RequestContext?.Identity is not { IsAuthenticated: true } identity)
        {
            return Left<EncinaError, bool>(EncinaErrors.Create( // NOSONAR S6966
                EncinaErrorCodes.AuthorizationUnauthenticated,
                "Resource authorization requires an authenticated request identity.",
                details: new Dictionary<string, object?>
                {
                    ["resourceType"] = resource.GetType().FullName,
                    ["policy"] = policy
                }));
        }

        // An authenticated identity built without a principal (builders only) satisfies no policy.
        var user = identity.Principal ?? new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        var result = await _authorizationService
            .AuthorizeAsync(user, resource, policy)
            .ConfigureAwait(false);

        if (result.Succeeded)
        {
            return Right<EncinaError, bool>(true); // NOSONAR S6966
        }

        var failureReasons = result.Failure?.FailureReasons
            .Select(r => r.Message)
            .Where(m => !string.IsNullOrEmpty(m))
            .ToList();

        return Left<EncinaError, bool>(EncinaErrors.Create( // NOSONAR S6966
            EncinaErrorCodes.AuthorizationResourceDenied,
            $"Resource authorization denied. Policy '{policy}' was not satisfied for resource of type '{resource.GetType().Name}'.",
            details: new Dictionary<string, object?>
            {
                ["resourceType"] = resource.GetType().FullName,
                ["policy"] = policy,
                ["failureReasons"] = failureReasons
            }));
    }
}
