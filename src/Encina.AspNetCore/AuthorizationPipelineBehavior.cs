using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore;

/// <summary>
/// Pipeline behavior that enforces authorization using ASP.NET Core's authorization system.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior checks for <see cref="AuthorizeAttribute"/> on the request type and enforces
/// authorization using ASP.NET Core's <see cref="IAuthorizationService"/>.
/// </para>
/// <para>
/// Supports:
/// <list type="bullet">
/// <item><description><b>Role-based authorization</b>: [Authorize(Roles = "Admin")]</description></item>
/// <item><description><b>Policy-based authorization</b>: [Authorize(Policy = "RequireElevation")]</description></item>
/// <item><description><b>Multiple attributes</b>: All must pass (AND logic)</description></item>
/// <item><description><b>Allow anonymous</b>: [AllowAnonymous] bypasses all authorization</description></item>
/// <item><description><b>Resource-based authorization</b>: Request is passed as resource to policies</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Important</b>: Requires authenticated user via <see cref="HttpContext.User"/>.
/// Use after <c>app.UseAuthentication()</c> in the middleware pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Require authentication
/// [Authorize]
/// public record DeleteUserCommand(int UserId) : ICommand&lt;Unit&gt;;
///
/// // Require specific role
/// [Authorize(Roles = "Admin")]
/// public record BanUserCommand(int UserId) : ICommand&lt;Unit&gt;;
///
/// // Require custom policy
/// [Authorize(Policy = "RequireElevation")]
/// public record TransferMoneyCommand(decimal Amount) : ICommand&lt;Receipt&gt;;
///
/// // Multiple requirements (both must pass)
/// [Authorize(Roles = "Admin")]
/// [Authorize(Policy = "RequireApproval")]
/// public record DeleteAccountCommand(int AccountId) : ICommand&lt;Unit&gt;;
///
/// // Opt-out of authorization (public endpoint)
/// [AllowAnonymous]
/// public record GetPublicDataQuery : IQuery&lt;PublicData&gt;;
/// </code>
/// </example>
public sealed class AuthorizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageAuthorization = "authorization";

    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="authorizationService">The ASP.NET Core authorization service.</param>
    /// <param name="httpContextAccessor">Accessor to get the current HTTP context.</param>
    public AuthorizationPipelineBehavior(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check for AllowAnonymous first - bypasses all authorization
        var hasAllowAnonymous = typeof(TRequest)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .Length > 0;

        if (hasAllowAnonymous)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Get authorize attributes from request type
        var authorizeAttributes = typeof(TRequest)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .ToList();

        // If no authorization required, proceed
        if (authorizeAttributes.Count == 0)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Get HTTP context
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966: LanguageExt Left is a pure function
                code: "authorization.no_http_context",
                message: "Authorization requires HTTP context but none is available.",
                details: new Dictionary<string, object?>
                {
                    [MetadataKeyRequestType] = typeof(TRequest).FullName,
                    [MetadataKeyStage] = MetadataStageAuthorization
                }));
        }

        var user = httpContext.User;

        // Check if user is authenticated (if any [Authorize] attribute requires it)
        if (user?.Identity?.IsAuthenticated is not true)
        {
            return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966: LanguageExt Left is a pure function
                code: "authorization.unauthenticated",
                message: $"Request '{typeof(TRequest).Name}' requires authentication.",
                details: new Dictionary<string, object?>
                {
                    [MetadataKeyRequestType] = typeof(TRequest).FullName,
                    [MetadataKeyStage] = MetadataStageAuthorization,
                    ["requirement"] = "authenticated"
                }));
        }

        // Check each authorization requirement
        foreach (var authorizeAttribute in authorizeAttributes)
        {
            // Check policy-based authorization
            if (!string.IsNullOrWhiteSpace(authorizeAttribute.Policy))
            {
                var policyResult = await _authorizationService.AuthorizeAsync(
                    user,
                    resource: request, // Pass request as resource for resource-based authorization
                    policyName: authorizeAttribute.Policy)
                    .ConfigureAwait(false);

                if (!policyResult.Succeeded)
                {
                    return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966: LanguageExt Left is a pure function
                        code: "authorization.policy_failed",
                        message: $"User does not satisfy policy '{authorizeAttribute.Policy}' required by '{typeof(TRequest).Name}'.",
                        details: new Dictionary<string, object?>
                        {
                            [MetadataKeyRequestType] = typeof(TRequest).FullName,
                            [MetadataKeyStage] = MetadataStageAuthorization,
                            ["requirement"] = "policy",
                            ["policy"] = authorizeAttribute.Policy,
                            ["userId"] = context.UserId,
                            ["failureReasons"] = policyResult.Failure?.FailureReasons
                                .Select(r => r.Message)
                                .ToList()
                        }));
                }
            }

            // Check role-based authorization
            if (!string.IsNullOrWhiteSpace(authorizeAttribute.Roles))
            {
                var requiredRoles = authorizeAttribute.Roles
                    .Split(',')
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();

                var hasAnyRequiredRole = requiredRoles.Any(user.IsInRole);

                if (!hasAnyRequiredRole)
                {
                    return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966: LanguageExt Left is a pure function
                        code: "authorization.insufficient_roles",
                        message: $"User does not have any of the required roles ({string.Join(", ", requiredRoles)}) for '{typeof(TRequest).Name}'.",
                        details: new Dictionary<string, object?>
                        {
                            [MetadataKeyRequestType] = typeof(TRequest).FullName,
                            [MetadataKeyStage] = MetadataStageAuthorization,
                            ["requirement"] = "roles",
                            ["requiredRoles"] = requiredRoles,
                            ["userId"] = context.UserId
                        }));
                }
            }

            // Check authentication schemes (if specified)
            // Note: AuthenticationSchemes is typically handled by ASP.NET Core middleware
            // before the request reaches the Encina, so we don't need to check it here
        }

        // All authorization checks passed
        return await nextStep().ConfigureAwait(false);
    }
}
