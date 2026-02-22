using System.Collections.Concurrent;
using Encina.AspNetCore.Authorization;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore;

/// <summary>
/// Pipeline behavior that enforces authorization using ASP.NET Core's authorization system.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior checks for authorization attributes on the request type and enforces
/// authorization using ASP.NET Core's <see cref="IAuthorizationService"/>.
/// </para>
/// <para>
/// Supports:
/// <list type="bullet">
/// <item><description><b>Role-based authorization</b>: <c>[Authorize(Roles = "Admin")]</c></description></item>
/// <item><description><b>Policy-based authorization</b>: <c>[Authorize(Policy = "RequireElevation")]</c></description></item>
/// <item><description><b>Resource-based authorization</b>: <c>[ResourceAuthorize("PolicyName")]</c> — the request is passed as the resource</description></item>
/// <item><description><b>Multiple attributes</b>: All must pass (AND logic)</description></item>
/// <item><description><b>Allow anonymous</b>: <c>[AllowAnonymous]</c> bypasses all authorization</description></item>
/// <item><description><b>CQRS default policies</b>: Automatic policy application when <see cref="AuthorizationConfiguration.AutoApplyPolicies"/> is enabled</description></item>
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
/// // Resource-based authorization (request is the resource)
/// [ResourceAuthorize("CanEditOrder")]
/// public record UpdateOrderCommand(OrderId Id, string NewStatus) : ICommand&lt;Order&gt;;
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

    // Cache CQRS type checks and attribute lookups to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, bool> CommandTypeCache = new();
    private static readonly ConcurrentDictionary<Type, bool> AllowAnonymousCache = new();
    private static readonly ConcurrentDictionary<Type, List<AuthorizeAttribute>> AuthorizeAttributeCache = new();
    private static readonly ConcurrentDictionary<Type, ResourceAuthorizeAttribute?> ResourceAuthorizeCache = new();

    private static readonly Type CommandOpenGeneric = typeof(ICommand<>);

    // High-performance logging delegates
    private static readonly Action<ILogger, string, string?, string?, Exception?> LogAuthorizationSucceeded =
        LoggerMessage.Define<string, string?, string?>(
            LogLevel.Debug,
            new EventId(1, "AuthorizationSucceeded"),
            "Authorization succeeded for {RequestType}. Policy: {Policy}, UserId: {UserId}");

    private static readonly Action<ILogger, string, string?, string?, string, Exception?> LogAuthorizationDenied =
        LoggerMessage.Define<string, string?, string?, string>(
            LogLevel.Warning,
            new EventId(2, "AuthorizationDenied"),
            "Authorization denied for {RequestType}. Policy: {Policy}, UserId: {UserId}, Reason: {Reason}");

    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthorizationConfiguration _configuration;
    private readonly ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="authorizationService">The ASP.NET Core authorization service.</param>
    /// <param name="httpContextAccessor">Accessor to get the current HTTP context.</param>
    /// <param name="options">CQRS-aware authorization configuration.</param>
    /// <param name="logger">Logger for structured authorization diagnostics.</param>
    public AuthorizationPipelineBehavior(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuthorizationConfiguration> options,
        ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>> logger)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);

        // 1. Check for AllowAnonymous first - bypasses all authorization
        if (HasAllowAnonymous(requestType))
        {
            return await nextStep().ConfigureAwait(false);
        }

        // 2. Collect authorization metadata from attributes
        var authorizeAttributes = GetAuthorizeAttributes(requestType);
        var resourceAuthorizeAttribute = GetResourceAuthorizeAttribute(requestType);

        // 3. Determine if CQRS default policy should apply (when no explicit attributes)
        string? autoAppliedPolicy = null;
        if (authorizeAttributes.Count == 0
            && resourceAuthorizeAttribute is null
            && _configuration.AutoApplyPolicies)
        {
            autoAppliedPolicy = IsCommand(requestType)
                ? _configuration.DefaultCommandPolicy
                : _configuration.DefaultQueryPolicy;
        }

        // 4. If no authorization required at all, proceed
        if (authorizeAttributes.Count == 0
            && resourceAuthorizeAttribute is null
            && autoAppliedPolicy is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // 5. Get HTTP context
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            var reason = "Authorization requires HTTP context but none is available.";
            LogAuthorizationDenied(_logger, requestType.FullName!, null, null, reason, null);

            return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966
                code: EncinaErrorCodes.AuthorizationUnauthorized,
                message: reason,
                details: new Dictionary<string, object?>
                {
                    [MetadataKeyRequestType] = requestType.FullName,
                    [MetadataKeyStage] = MetadataStageAuthorization
                }));
        }

        var user = httpContext.User;
        var userId = context.UserId;

        // 6. Check if user is authenticated
        if (user?.Identity?.IsAuthenticated is not true)
        {
            var reason = $"Request '{requestType.Name}' requires authentication.";
            LogAuthorizationDenied(_logger, requestType.FullName!, null, userId, reason, null);

            return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966
                code: EncinaErrorCodes.AuthorizationUnauthorized,
                message: reason,
                details: new Dictionary<string, object?>
                {
                    [MetadataKeyRequestType] = requestType.FullName,
                    [MetadataKeyStage] = MetadataStageAuthorization,
                    ["requirement"] = "authenticated"
                }));
        }

        // 7. Process [Authorize] attributes
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
                    var reason = $"User does not satisfy policy '{authorizeAttribute.Policy}' required by '{requestType.Name}'.";
                    LogAuthorizationDenied(_logger, requestType.FullName!, authorizeAttribute.Policy, userId, reason, null);

                    return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966
                        code: EncinaErrorCodes.AuthorizationPolicyFailed,
                        message: reason,
                        details: new Dictionary<string, object?>
                        {
                            [MetadataKeyRequestType] = requestType.FullName,
                            [MetadataKeyStage] = MetadataStageAuthorization,
                            ["requirement"] = "policy",
                            ["policy"] = authorizeAttribute.Policy,
                            ["userId"] = userId,
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
                    var reason = $"User does not have any of the required roles ({string.Join(", ", requiredRoles)}) for '{requestType.Name}'.";
                    LogAuthorizationDenied(_logger, requestType.FullName!, null, userId, reason, null);

                    return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966
                        code: EncinaErrorCodes.AuthorizationForbidden,
                        message: reason,
                        details: new Dictionary<string, object?>
                        {
                            [MetadataKeyRequestType] = requestType.FullName,
                            [MetadataKeyStage] = MetadataStageAuthorization,
                            ["requirement"] = "roles",
                            ["requiredRoles"] = requiredRoles,
                            ["userId"] = userId
                        }));
                }
            }

            // Note: AuthenticationSchemes is typically handled by ASP.NET Core middleware
            // before the request reaches Encina, so we don't check it here
        }

        // 8. Process [ResourceAuthorize] attribute
        if (resourceAuthorizeAttribute is not null)
        {
            var policyResult = await _authorizationService.AuthorizeAsync(
                user,
                resource: request!,
                policyName: resourceAuthorizeAttribute.Policy)
                .ConfigureAwait(false);

            if (!policyResult.Succeeded)
            {
                var failureReasons = policyResult.Failure?.FailureReasons
                    .Select(r => r.Message)
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                var reason = $"Resource authorization denied. Policy '{resourceAuthorizeAttribute.Policy}' was not satisfied for request '{requestType.Name}'.";
                LogAuthorizationDenied(_logger, requestType.FullName!, resourceAuthorizeAttribute.Policy, userId, reason, null);

                return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966
                    code: EncinaErrorCodes.AuthorizationResourceDenied,
                    message: reason,
                    details: new Dictionary<string, object?>
                    {
                        [MetadataKeyRequestType] = requestType.FullName,
                        [MetadataKeyStage] = MetadataStageAuthorization,
                        ["requirement"] = "resource_authorization",
                        ["policy"] = resourceAuthorizeAttribute.Policy,
                        ["userId"] = userId,
                        ["failureReasons"] = failureReasons
                    }));
            }
        }

        // 9. Process CQRS auto-applied default policy
        if (autoAppliedPolicy is not null)
        {
            var policyResult = await _authorizationService.AuthorizeAsync(
                user,
                resource: request,
                policyName: autoAppliedPolicy)
                .ConfigureAwait(false);

            if (!policyResult.Succeeded)
            {
                var reason = $"User does not satisfy auto-applied default policy '{autoAppliedPolicy}' for '{requestType.Name}'.";
                LogAuthorizationDenied(_logger, requestType.FullName!, autoAppliedPolicy, userId, reason, null);

                return Left<EncinaError, TResponse>(EncinaErrors.Create( // NOSONAR S6966
                    code: EncinaErrorCodes.AuthorizationPolicyFailed,
                    message: reason,
                    details: new Dictionary<string, object?>
                    {
                        [MetadataKeyRequestType] = requestType.FullName,
                        [MetadataKeyStage] = MetadataStageAuthorization,
                        ["requirement"] = "auto_applied_policy",
                        ["policy"] = autoAppliedPolicy,
                        ["userId"] = userId,
                        ["isCommand"] = IsCommand(requestType),
                        ["failureReasons"] = policyResult.Failure?.FailureReasons
                            .Select(r => r.Message)
                            .ToList()
                    }));
            }
        }

        // 10. All authorization checks passed
        var effectivePolicy = resourceAuthorizeAttribute?.Policy
            ?? authorizeAttributes.FirstOrDefault()?.Policy
            ?? autoAppliedPolicy;
        LogAuthorizationSucceeded(_logger, requestType.FullName!, effectivePolicy, userId, null);

        return await nextStep().ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the specified type implements <see cref="ICommand{TResponse}"/>.
    /// If <c>false</c>, the type is treated as a query for CQRS default policy purposes.
    /// </summary>
    private static bool IsCommand(Type requestType)
    {
        return CommandTypeCache.GetOrAdd(requestType, static type =>
            type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == CommandOpenGeneric));
    }

    private static bool HasAllowAnonymous(Type requestType)
    {
        return AllowAnonymousCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0);
    }

    private static List<AuthorizeAttribute> GetAuthorizeAttributes(Type requestType)
    {
        return AuthorizeAttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .ToList());
    }

    private static ResourceAuthorizeAttribute? GetResourceAuthorizeAttribute(Type requestType)
    {
        return ResourceAuthorizeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttributes(typeof(ResourceAuthorizeAttribute), inherit: true)
                .Cast<ResourceAuthorizeAttribute>()
                .FirstOrDefault());
    }
}
