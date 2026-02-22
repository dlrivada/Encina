using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore.Authorization;

/// <summary>
/// Default implementation of <see cref="IResourceAuthorizer"/> that delegates
/// to ASP.NET Core's <see cref="IAuthorizationService"/>.
/// </summary>
internal sealed class ResourceAuthorizer : IResourceAuthorizer
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResourceAuthorizer(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
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
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return Left<EncinaError, bool>(EncinaErrors.Create( // NOSONAR S6966
                EncinaErrorCodes.AuthorizationUnauthorized,
                "Authorization requires HTTP context but none is available.",
                details: new Dictionary<string, object?>
                {
                    ["resourceType"] = resource.GetType().FullName,
                    ["policy"] = policy
                }));
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated is not true)
        {
            return Left<EncinaError, bool>(EncinaErrors.Unauthorized( // NOSONAR S6966
                new Dictionary<string, object?>
                {
                    ["resourceType"] = resource.GetType().FullName,
                    ["policy"] = policy
                }));
        }

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
